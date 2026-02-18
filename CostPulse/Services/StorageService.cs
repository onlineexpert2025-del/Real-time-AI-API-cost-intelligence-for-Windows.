using System;
using System.IO;
using System.Text.Json;
using CostPulse.Models;

namespace CostPulse.Services
{
    public class StorageService
    {
        private readonly string _appDataPath;
        private readonly string _filePath;
        private readonly LoggingService _logger;

        public StorageService(LoggingService logger)
        {
            _logger = logger;
            _appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CostPulse");
            _filePath = Path.Combine(_appDataPath, "costpulse.json");

            if (!Directory.Exists(_appDataPath))
            {
                Directory.CreateDirectory(_appDataPath);
                _logger.Log($"Created AppData folder: {_appDataPath}");
            }
        }

        public CostPulseData Load()
        {
            if (!File.Exists(_filePath))
            {
                _logger.Log("No existing data file found. Creating new.");
                return new CostPulseData();
            }

            try
            {
                string json = File.ReadAllText(_filePath);
                var data = JsonSerializer.Deserialize<CostPulseData>(json);
                if (data == null) throw new Exception("Deserialized data is null");
                _logger.Log("Data loaded successfully.");
                return data;
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to load data. Backing up and starting fresh.", ex);
                BackupCorruptFile();
                return new CostPulseData(); // Return fresh start
            }
        }

        public void Save(CostPulseData data)
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(data, options);
                File.WriteAllText(_filePath, json);
                _logger.Log("Data saved successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to save data.", ex);
            }
        }

        private void BackupCorruptFile()
        {
            try
            {
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string backupPath = Path.Combine(_appDataPath, $"costpulse_corrupt_{timestamp}.json");
                if (File.Exists(_filePath))
                {
                    File.Copy(_filePath, backupPath);
                    _logger.Log($"Corrupt file backed up to {backupPath}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to backup corrupt file.", ex);
            }
        }
    }
}
