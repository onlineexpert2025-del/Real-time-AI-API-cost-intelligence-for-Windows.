using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Windows.Threading;
using CostPulse.Models;

namespace CostPulse.Services
{
    public class ClipboardMonitorService
    {
        private readonly DataService _dataService;
        private readonly LoggingService _logger;
        private readonly DispatcherTimer _timer;
        private string _lastProcessedHash = string.Empty;

        public ClipboardMonitorService(DataService dataService, LoggingService logger)
        {
            _dataService = dataService ?? throw new ArgumentNullException(nameof(dataService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += OnTimerTick;
        }

        public void Start()
        {
            _timer.Start();
            _logger.Log("Clipboard Monitor started.");
        }

        public void Stop()
        {
            _timer.Stop();
            _logger.Log("Clipboard Monitor stopped.");
        }

        private void OnTimerTick(object? sender, EventArgs e)
        {
            try
            {
                if (!System.Windows.Clipboard.ContainsText()) return;

                string text = System.Windows.Clipboard.GetText();
                if (string.IsNullOrWhiteSpace(text)) return;

                // Trace log (verbose)
                // _logger.Log($"Clipboard Tick. Text length: {text.Length}"); 

                // Hash check to prevent duplicates
                string currentHash = ComputeHash(text);
                if (currentHash == _lastProcessedHash) return;

                // Quick detection logic
                // The user reported "Not detecting copied JSON"
                // It's possible the text doesn't explicitly contain "total_tokens" if it's "prompt_tokens" only?
                // Or maybe case sensitivity?
                // Let's widen the check and add a debug log when candidate found.
                
                // Allow "usage" object structure as well
                bool potentialJson = text.TrimStart().StartsWith("{");
                bool hasKeywords = text.Contains("total_tokens") || text.Contains("prompt_tokens") || text.Contains("\"usage\"");
                
                if (potentialJson && hasKeywords)
                {
                    _logger.Log($"Clipboard Candidate Found ({text.Length} chars). Hash: {currentHash}");
                    _lastProcessedHash = currentHash; 
                    TryProcessClipboardText(text);
                }
            }
            catch (Exception ex)
            {
                // Clipboard access can fail (e.g. valid fail if another app is using it), just log warning and continue
                // But don't spam log if it happens every second? Maybe safe to just ignore or log verbose.
                // _logger.LogError("Clipboard access failed", ex); 
            }
        }

        private void TryProcessClipboardText(string text)
        {
            try
            {
                // Parse JSON
                // Using JsonDocument to safe parse arbitrary JSON
                using (JsonDocument doc = JsonDocument.Parse(text))
                {
                    JsonElement root = doc.RootElement;
                    
                    // Look for token counts (handling various case or deeper nesting? 
                    // User said: "Extract: prompt_tokens, completion_tokens, total_tokens, model (if exists)"
                    // Usually this is flat or inside "usage". Let's try flat first, then "usage" property if not found?
                    // "If clipboard contains text... Parse JSON safely... Extract..."
                    // I will assume flat or common OpenAI format structure.
                    
                    // Helper to get int from element
                    int? promptTokens = GetIntProperty(root, "prompt_tokens");
                    int? completionTokens = GetIntProperty(root, "completion_tokens");
                    int? totalTokens = GetIntProperty(root, "total_tokens");
                    string? model = GetStringProperty(root, "model");

                    // If common stats are NOT found in root, check "usage" object (OpenAI style)
                    if (promptTokens == null && totalTokens == null && root.TryGetProperty("usage", out JsonElement usage))
                    {
                        promptTokens = GetIntProperty(usage, "prompt_tokens");
                        completionTokens = GetIntProperty(usage, "completion_tokens");
                        totalTokens = GetIntProperty(usage, "total_tokens");
                        
                        // Model usually at root in OpenAI
                        if (model == null) model = GetStringProperty(root, "model");
                    }

                    // Validation: Must have at least total_tokens or prompt_tokens
                    if (totalTokens.HasValue || promptTokens.HasValue)
                    {
                        // Calc missing
                        int p = promptTokens ?? 0;
                        int c = completionTokens ?? 0;
                        int t = totalTokens ?? (p + c);

                        if (t == 0) return; // Empty usage

                        var entry = new UsageEntry
                        {
                            Timestamp = DateTime.Now,
                            Provider = "Clipboard",
                            ModelName = !string.IsNullOrWhiteSpace(model) ? model : "Unknown",
                            InputTokens = p,
                            OutputTokens = c,
                            Label = "Auto-Import"
                        };

                        _dataService.AddEntry(entry);
                        _logger.Log($"Clipboard Auto-Import Success: {entry.ModelName} ({t} tokens)");
                    }
                    else
                    {
                        _logger.Log("Clipboard JSON parsed but missing token counts.");
                    }
                }
            }
            catch (JsonException jEx)
            {
                _logger.Log($"Clipboard Parse Failed: Not valid JSON. {jEx.Message}");
            }
            catch (Exception ex)
            {
                _logger.LogError("Error parsing clipboard JSON", ex);
            }
        }

        private int? GetIntProperty(JsonElement element, string propertyName)
        {
            if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty(propertyName, out JsonElement prop))
            {
                if (prop.ValueKind == JsonValueKind.Number && prop.TryGetInt32(out int val))
                    return val;
            }
            return null;
        }

        private string? GetStringProperty(JsonElement element, string propertyName)
        {
            if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty(propertyName, out JsonElement prop))
            {
                return prop.GetString();
            }
            return null;
        }

        private string ComputeHash(string input)
        {
            using (var md5 = MD5.Create())
            {
                var inputBytes = Encoding.UTF8.GetBytes(input);
                var hashBytes = md5.ComputeHash(inputBytes);
                return Convert.ToHexString(hashBytes);
            }
        }
    }
}
