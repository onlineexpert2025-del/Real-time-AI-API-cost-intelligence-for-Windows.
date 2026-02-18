using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CostPulse.Models;

namespace CostPulse.Services
{
    public class LogImportService
    {
        private readonly DataService _dataService;
        private readonly LoggingService _logger;
        private readonly List<FileSystemWatcher> _watchers = new List<FileSystemWatcher>();
        private readonly ConcurrentDictionary<string, long> _fileOffsets = new ConcurrentDictionary<string, long>();
        private Timer _scanTimer;

        private readonly string _offsetsFilePath;

        public LogImportService(DataService dataService, LoggingService logger)
        {
            _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            string appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CostPulse");
            _offsetsFilePath = Path.Combine(appData, "log_offsets.json");
        }

        public void Start()
        {
            LoadOffsets();
            UpdateWatchers();
            // Also run a periodic scan for existing files or missed events
            _scanTimer = new Timer(ScanCallback, null, 1000, 30000); // Start in 1s, repeat every 30s
            _logger.Log("Log Import Service started.");
        }

        public void Stop()
        {
            SaveOffsets();
            _scanTimer?.Dispose();
            foreach (var w in _watchers)
            {
                w.EnableRaisingEvents = false;
                w.Dispose();
            }
            _watchers.Clear();
            _logger.Log("Log Import Service stopped.");
        }

        private void LoadOffsets()
        {
            try
            {
                if (File.Exists(_offsetsFilePath))
                {
                    string json = File.ReadAllText(_offsetsFilePath);
                    var validOffsets = JsonSerializer.Deserialize<Dictionary<string, long>>(json);
                    if (validOffsets != null)
                    {
                        foreach (var kvp in validOffsets)
                        {
                            _fileOffsets[kvp.Key] = kvp.Value;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to load log offsets", ex);
            }
        }

        private void SaveOffsets()
        {
            try
            {
                var copy = new Dictionary<string, long>(_fileOffsets);
                string json = JsonSerializer.Serialize(copy, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_offsetsFilePath, json);
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to save log offsets", ex);
            }
        }

        public void UpdateWatchers()
        {
            // Stop existing
            foreach (var w in _watchers)
            {
                w.EnableRaisingEvents = false;
                w.Dispose();
            }
            _watchers.Clear();

            var paths = _dataService.Data?.Settings?.LogWatchPaths;
            if (paths == null) return;

            foreach (var path in paths)
            {
                if (Directory.Exists(path))
                {
                    try
                    {
                        var watcher = new FileSystemWatcher(path);
                        watcher.Filter = "*.*"; // We filter in logic
                        watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size; // Watch for writes
                        watcher.Changed += OnFileChanged;
                        watcher.Created += OnFileChanged;
                        watcher.EnableRaisingEvents = true;
                        _watchers.Add(watcher);
                        _logger.Log($"Watching directory: {path}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Failed to watch {path}", ex);
                    }
                }
            }
        }

        private void ScanCallback(object? state)
        {
            var paths = _dataService.Data?.Settings?.LogWatchPaths;
            if (paths == null) return;

            foreach (var path in paths)
            {
                if (Directory.Exists(path))
                {
                    try
                    {
                        var files = Directory.GetFiles(path, "*.*", SearchOption.TopDirectoryOnly);
                        foreach (var file in files)
                        {
                            if (ShouldProcessFile(file))
                            {
                                ProcessFile(file);
                            }
                        }
                    }
                    catch {}
                }
            }
            SaveOffsets();
        }

        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            if (ShouldProcessFile(e.FullPath))
            {
                // Debounce or just try processing?
                // File might be locked. ProcessFile handles rudimentary checking.
                ProcessFile(e.FullPath);
            }
        }

        private bool ShouldProcessFile(string path)
        {
            var ext = Path.GetExtension(path).ToLowerInvariant();
            return ext == ".log" || ext == ".txt" || ext == ".json" || ext == ".jsonl";
        }

        private void ProcessFile(string filePath)
        {
            try
            {
                // Check if file is readable
                using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var reader = new StreamReader(fs))
                {
                    long lastOffset = 0;
                    if (_fileOffsets.TryGetValue(filePath, out long offset))
                    {
                        lastOffset = offset;
                    }

                    // Reset if file shrank (replaced/rotated)
                    if (fs.Length < lastOffset)
                    {
                        _logger.Log($"File shrank or replaced: {Path.GetFileName(filePath)}. Resetting offset to 0.");
                        lastOffset = 0;
                    }

                    if (fs.Length == lastOffset) 
                    {
                        return; // Nothing new to read
                    }

                    fs.Seek(lastOffset, SeekOrigin.Begin);
                    _logger.Log($"Processing {Path.GetFileName(filePath)} from offset {lastOffset}...");

                    string line;
                    int linesRead = 0;
                    int entriesImported = 0;

                    while ((line = reader.ReadLine()) != null)
                    {
                        linesRead++;
                        if (ProcessLine(line, filePath))
                        {
                            entriesImported++;
                        }
                    }

                    // Update offset
                    _fileOffsets[filePath] = fs.Position;
                    if (linesRead > 0)
                    {
                        _logger.Log($"Finished {Path.GetFileName(filePath)}. Read {linesRead} lines, Imported {entriesImported} entries. New Offset: {fs.Position}");
                    }
                }
            }
            catch (IOException)
            {
                // File locked, commonly happens if writer is writing. Retry later.
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing file {filePath}", ex);
            }
        }

        private readonly LogParser _parser = new LogParser();

        private bool ProcessLine(string line, string sourceFile)
        {
            var entry = _parser.ParseLine(line, sourceFile);
            if (entry != null)
            {
                entry.Timestamp = DateTime.Now; // Default to now
                
                // Deduplication Check
                if (!_dataService.Data.Entries.Any(e => e.ContentHash == entry.ContentHash))
                {
                    _dataService.AddEntry(entry);
                    _logger.Log($"Imported log entry: {entry.ModelName}");
                    return true;
                }
                else 
                {
                    // _logger.Log("Skipped duplicate."); // Optional: Verbose logging
                }
            }
            return false;
        }


    }
}
