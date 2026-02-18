using System.Collections.Generic;

namespace CostPulse.Models
{
    public class CostPulseData
    {
        public AppSettings Settings { get; set; } = new AppSettings();
        public List<UsageEntry> Entries { get; set; } = new List<UsageEntry>();
    }
}
