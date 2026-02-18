using Xunit;
using CostPulse.Services;
using CostPulse.Models;

namespace CostPulse.Tests
{
    public class LogParserTests
    {
        private readonly LogParser _parser = new LogParser();

        [Fact]
        public void ParseLine_ShouldReturnNull_ForNonUsageLines()
        {
            var result = _parser.ParseLine("Just a log line", "test.log");
            Assert.Null(result);
        }

        [Fact]
        public void ParseLine_ShouldExtractUsage_FromOpenAIFormat()
        {
            string line = "INFO: { \"model\": \"gpt-4\", \"usage\": { \"prompt_tokens\": 10, \"completion_tokens\": 20, \"total_tokens\": 30 } }";
            var result = _parser.ParseLine(line, "test.log");
            
            Assert.NotNull(result);
            Assert.Equal("gpt-4", result.ModelName);
            Assert.Equal(10, result.InputTokens);
            Assert.Equal(20, result.OutputTokens);
            Assert.Contains("test.log", result.Label);
        }
        
        [Fact]
        public void ParseLine_ShouldExtractUsage_FromAnthropicFormat()
        {
             string line = "INFO: { \"model\": \"claude-3-opus\", \"usage\": { \"input_tokens\": 15, \"output_tokens\": 25 } }";
             var result = _parser.ParseLine(line, "test.log");
             
             Assert.NotNull(result);
             Assert.Equal("claude-3-opus", result.ModelName);
             Assert.Equal(15, result.InputTokens);
             Assert.Equal(25, result.OutputTokens);
        }

        [Fact]
        public void ComputeHash_ShouldBeDeterministic()
        {
            string input = "test data";
            string hashT1 = _parser.ComputeHash(input);
            string hashT2 = _parser.ComputeHash(input);
            
            Assert.Equal(hashT1, hashT2);
            Assert.NotEqual(hashT1, _parser.ComputeHash("different data"));
        }
    }
}
