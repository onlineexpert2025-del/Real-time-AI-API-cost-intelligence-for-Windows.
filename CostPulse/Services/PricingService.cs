using System.Collections.Generic;
using System.Linq;
using CostPulse.Models;

namespace CostPulse.Services
{
    public class PricingService
    {
        private List<PricingModel> _priceTable;

        public PricingService(List<PricingModel> initialPrices)
        {
            _priceTable = initialPrices ?? new List<PricingModel>();
        }

        public void UpdatePriceTable(List<PricingModel> newTable)
        {
            _priceTable = newTable;
        }

        public decimal CalculateCost(int inputTokens, int outputTokens, PricingModel model)
        {
            if (model == null) return 0m;

            decimal inputCost = (inputTokens / 1_000_000m) * model.InputPricePerMillion;
            decimal outputCost = (outputTokens / 1_000_000m) * model.OutputPricePerMillion;

            return inputCost + outputCost;
        }

        public PricingModel GetPricingForModel(string modelName)
        {
            if (string.IsNullOrWhiteSpace(modelName)) return null;
            
            string normalized = modelName.Trim();
            
            // 1. Exact Match (Case-Insensitive)
            var match = _priceTable.FirstOrDefault(p => p.ModelName.Equals(normalized, System.StringComparison.OrdinalIgnoreCase));
            if (match != null) return match;

            // 2. Fuzzy Mapping (OpenAI Only checks)
            // If starts with "gpt-4o" -> try to match "gpt-4o" pricing exactly
            // If starts with "gpt-4" -> try to match "gpt-4" pricing exactly
            // Note: This relies on "gpt-4o" and "gpt-4" existing in the table (which they do by default).
            
            string lowerName = normalized.ToLowerInvariant();
            
            if (lowerName.StartsWith("gpt-4o"))
            {
                 return _priceTable.FirstOrDefault(p => p.ModelName.Equals("gpt-4o", System.StringComparison.OrdinalIgnoreCase));
            }
            if (lowerName.StartsWith("gpt-4"))
            {
                 return _priceTable.FirstOrDefault(p => p.ModelName.Equals("gpt-4", System.StringComparison.OrdinalIgnoreCase));
            }

            return null;
        }
    }
}
