using System.Collections.Generic;

namespace CostPulse.Models
{
    public class AppSettings
    {
        public decimal DailyBudget { get; set; } = 10.0m;
        public string Currency { get; set; } = "$";
        public bool AlwaysOnTop { get; set; } = true;
        public bool RunOnStartup { get; set; } = false;
        public bool DeveloperMode { get; set; } = true; // Portfolio Mode
        public string LicenseKey { get; set; }
        public List<PricingModel> PricingModels { get; set; } = new List<PricingModel>
        {
            new PricingModel { ModelName = "gpt-4", InputPricePerMillion = 30.00m, OutputPricePerMillion = 60.00m },
            new PricingModel { ModelName = "gpt-4o", InputPricePerMillion = 5.00m, OutputPricePerMillion = 15.00m },
            new PricingModel { ModelName = "gpt-4o-mini", InputPricePerMillion = 0.15m, OutputPricePerMillion = 0.60m },
            new PricingModel { ModelName = "claude-3-haiku", InputPricePerMillion = 0.25m, OutputPricePerMillion = 1.25m },
            new PricingModel { ModelName = "gemini-1.5-pro", InputPricePerMillion = 3.50m, OutputPricePerMillion = 10.50m }
        };

        public List<string> LogWatchPaths { get; set; } = new List<string>();
    }
}
