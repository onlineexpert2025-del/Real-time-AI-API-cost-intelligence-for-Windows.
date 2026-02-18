using Xunit;
using CostPulse.Services;
using CostPulse.Models;
using System.Collections.Generic;

namespace CostPulse.Tests
{
    public class PricingServiceTests
    {
        private readonly List<PricingModel> _defaultModels = new List<PricingModel>
        {
            new PricingModel { ModelName = "gpt-4", InputPricePerMillion = 30.0m, OutputPricePerMillion = 60.0m },
            new PricingModel { ModelName = "gpt-4o", InputPricePerMillion = 5.0m, OutputPricePerMillion = 15.0m }
        };

        [Fact]
        public void CalculateCost_ShouldReturnCorrectCost()
        {
            var service = new PricingService(_defaultModels);
            var model = _defaultModels[0]; // gpt-4
            
            // 1M input ($30) + 1M output ($60) = $90
            var cost = service.CalculateCost(1_000_000, 1_000_000, model);
            
            Assert.Equal(90.0m, cost);
        }

        [Fact]
        public void GetPricingForModel_ShouldFindExactMatch()
        {
            var service = new PricingService(_defaultModels);
            var model = service.GetPricingForModel("gpt-4");
            
            Assert.NotNull(model);
            Assert.Equal("gpt-4", model.ModelName);
        }

        [Fact]
        public void GetPricingForModel_ShouldHandleCaseInsensitivity()
        {
            var service = new PricingService(_defaultModels);
            var model = service.GetPricingForModel("GPT-4");
            
            Assert.NotNull(model);
            Assert.Equal("gpt-4", model.ModelName);
        }

        [Fact]
        public void GetPricingForModel_ShouldHandleFuzzyMatching()
        {
            var service = new PricingService(_defaultModels);
            
            // "gpt-4-0613" should map to "gpt-4"
            var model = service.GetPricingForModel("gpt-4-0613");
            Assert.NotNull(model);
            Assert.Equal("gpt-4", model.ModelName);

            // "gpt-4o-2024-05-13" should map to "gpt-4o"
            var modelO = service.GetPricingForModel("gpt-4o-2024-05-13");
            Assert.NotNull(modelO);
            Assert.Equal("gpt-4o", modelO.ModelName);
        }
    }
}
