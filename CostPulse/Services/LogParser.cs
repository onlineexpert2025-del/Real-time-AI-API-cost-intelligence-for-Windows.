using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CostPulse.Models;

namespace CostPulse.Services
{
    public class LogParser
    {
        public UsageEntry? ParseLine(string line, string sourceFile)
        {
            if (string.IsNullOrWhiteSpace(line)) return null;

            // Heuristic usage detection
            if (!line.Contains("tokens", StringComparison.OrdinalIgnoreCase)) return null;

            // Extract JSON
            // Line might be "INFO: { ... }"
            // Find first '{' and last '}'
            int start = line.IndexOf('{');
            int end = line.LastIndexOf('}');
            if (start >= 0 && end > start)
            {
                string json = line.Substring(start, end - start + 1);
                try
                {
                    using (var doc = JsonDocument.Parse(json))
                    {
                        var root = doc.RootElement;
                        var entry = ExtractUsage(root, json);
                        if (entry != null)
                        {
                            entry.Label = $"Log: {Path.GetFileName(sourceFile)}";
                            return entry;
                        }
                    }
                }
                catch { }
            }
            return null;
        }

        private UsageEntry? ExtractUsage(JsonElement root, string rawJson)
        {
            int? promptTokens = GetIntProperty(root, "prompt_tokens");
            int? outputTokens = GetIntProperty(root, "completion_tokens") ?? GetIntProperty(root, "output_tokens");
            int? totalTokens = GetIntProperty(root, "total_tokens");
            string? model = GetStringProperty(root, "model");

            // Check nested 'usage'
            if (root.TryGetProperty("usage", out JsonElement usage))
            {
                if (promptTokens == null) promptTokens = GetIntProperty(usage, "prompt_tokens");
                if (outputTokens == null) outputTokens = GetIntProperty(usage, "completion_tokens") ?? GetIntProperty(usage, "output_tokens");
                if (totalTokens == null) totalTokens = GetIntProperty(usage, "total_tokens");
                if (model == null) model = GetStringProperty(root, "model");
            }

            // Check Anthropic style
            if (root.TryGetProperty("usage", out JsonElement anthropicUsage)) 
            {
                 if (promptTokens == null) promptTokens = GetIntProperty(anthropicUsage, "input_tokens");
                 if (outputTokens == null) outputTokens = GetIntProperty(anthropicUsage, "output_tokens");
            }
            
            if (model == null) model = GetStringProperty(root, "model");

            if (promptTokens.HasValue || totalTokens.HasValue || outputTokens.HasValue)
            {
                 int p = promptTokens ?? 0;
                 int o = outputTokens ?? 0;
                 
                 var entry = new UsageEntry
                 {
                     ModelName = model ?? "Unknown",
                     InputTokens = p,
                     OutputTokens = o,
                     ContentHash = ComputeHash(rawJson)
                 };
                 return entry;
            }
            return null;
        }

        private int? GetIntProperty(JsonElement element, string propName)
        {
             if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty(propName, out JsonElement p))
             {
                 if (p.ValueKind == JsonValueKind.Number && p.TryGetInt32(out int v)) return v;
             }
             return null;
        }

        private string? GetStringProperty(JsonElement element, string propName)
        {
             if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty(propName, out JsonElement p))
             {
                 return p.GetString();
             }
             return null;
        }

        public string ComputeHash(string input)
        {
            using (var md5 = MD5.Create())
            {
                var bytes = Encoding.UTF8.GetBytes(input);
                var hash = md5.ComputeHash(bytes);
                return Convert.ToHexString(hash);
            }
        }
    }
}
