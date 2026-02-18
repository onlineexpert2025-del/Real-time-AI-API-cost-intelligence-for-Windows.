using System;

namespace CostPulse.Models
{
    public class UsageEntry
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string Provider { get; set; } = "Unknown";
        public string ModelName { get; set; } = "Unknown";
        public int InputTokens { get; set; }
        public int OutputTokens { get; set; }
        public decimal Cost { get; set; }
        public string Label { get; set; }
        public string ContentHash { get; set; } // For deduplication
        public string FormattedCost => Cost < 1.00m ? $"${Cost:F4}" : $"${Cost:F2}";
    }
}
