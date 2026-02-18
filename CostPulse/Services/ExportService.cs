using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CostPulse.Models;

namespace CostPulse.Services
{
    public class ExportService
    {
        private readonly LoggingService _logger;

        public ExportService(LoggingService logger)
        {
            _logger = logger;
        }

        public void ExportToCsv(IEnumerable<UsageEntry> entries, string filePath)
        {
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("Timestamp,Provider,Model,InputTokens,OutputTokens,Cost,Label");

                foreach (var entry in entries)
                {
                    sb.AppendLine($"{entry.Timestamp:yyyy-MM-dd HH:mm:ss},{entry.Provider},{entry.ModelName},{entry.InputTokens},{entry.OutputTokens},{entry.Cost},{entry.Label}");
                }

                File.WriteAllText(filePath, sb.ToString());
                _logger.Log($"Exported CSV to {filePath}");
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to export CSV", ex);
                throw; // Re-throw to show UI error if needed
            }
        }
    }
}
