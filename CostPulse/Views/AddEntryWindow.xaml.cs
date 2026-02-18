using System;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using CostPulse.Models;

namespace CostPulse.Views
{
    public partial class AddEntryWindow : Window
    {
        public AddEntryWindow()
        {
            InitializeComponent();
            LoadModels();
        }

        private void LoadModels()
        {
            if (App.DataService?.Data?.Settings?.PricingModels != null)
            {
                foreach (var model in App.DataService.Data.Settings.PricingModels)
                {
                    CmbModel.Items.Add(model.ModelName);
                }
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            TxtError.Text = "";

            if (!int.TryParse(TxtInput.Text, out int input) || input < 0)
            {
                TxtError.Text = "Invalid Input Tokens";
                return;
            }

            if (!int.TryParse(TxtOutput.Text, out int output) || output < 0)
            {
                TxtError.Text = "Invalid Output Tokens";
                return;
            }
            
            string provider = CmbProvider.Text;
            string modelName = CmbModel.Text;

            if (string.IsNullOrWhiteSpace(modelName))
            {
                TxtError.Text = "Model is required";
                return;
            }

            // Create Entry
            var entry = new UsageEntry
            {
                Id = Guid.NewGuid(),
                Timestamp = DateTime.Now,
                Provider = string.IsNullOrWhiteSpace(provider) ? "Unknown" : provider,
                ModelName = modelName,
                InputTokens = input,
                OutputTokens = output,
                Label = TxtLabel.Text
            };

            try
            {
                App.DataService.AddEntry(entry);
                this.Close();
            }
            catch (Exception ex)
            {
                TxtError.Text = $"Error saving: {ex.Message}";
            }
        }

        private void TxtJson_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Simple "Quick Add" parsing
            // Expects something like {"model": "gpt-4", "usage": {"prompt_tokens": 10, "completion_tokens": 20}} 
            // OR flat: {"model": "x", "input_tokens": 10, "output_tokens": 20}
            
            string json = TxtJson.Text.Trim();
            if (string.IsNullOrEmpty(json)) return;
            if (!json.StartsWith("{")) return;

            try
            {
                using (JsonDocument doc = JsonDocument.Parse(json))
                {
                    var root = doc.RootElement;
                    
                    // Try to extract Model
                    if (root.TryGetProperty("model", out JsonElement modelEl))
                    {
                        CmbModel.Text = modelEl.ToString();
                        
                        // Heuristic for provider based on model name
                        string m = modelEl.ToString().ToLower();
                        if (m.Contains("gpt")) CmbProvider.Text = "OpenAI";
                        else if (m.Contains("claude")) CmbProvider.Text = "Anthropic";
                        else if (m.Contains("gemini")) CmbProvider.Text = "Google";
                    }

                    // Try to extract Usage
                    int input = 0;
                    int output = 0;

                    // OpenAI format: "usage": { "prompt_tokens": 5, "completion_tokens": 7 }
                    if (root.TryGetProperty("usage", out JsonElement usageEl))
                    {
                        if (usageEl.TryGetProperty("prompt_tokens", out JsonElement pt)) input = pt.GetInt32();
                        if (usageEl.TryGetProperty("completion_tokens", out JsonElement ct)) output = ct.GetInt32();
                        if (usageEl.TryGetProperty("input_tokens", out JsonElement it)) input = it.GetInt32(); // Anthropic style
                        if (usageEl.TryGetProperty("output_tokens", out JsonElement ot)) output = ot.GetInt32();
                    }
                    else
                    {
                        // Flat format
                        if (root.TryGetProperty("input_tokens", out JsonElement it)) input = it.GetInt32();
                        if (root.TryGetProperty("output_tokens", out JsonElement ot)) output = ot.GetInt32();
                        if (root.TryGetProperty("prompt_tokens", out JsonElement pt)) input = pt.GetInt32();
                        if (root.TryGetProperty("completion_tokens", out JsonElement ct)) output = ct.GetInt32();
                    }

                    TxtInput.Text = input.ToString();
                    TxtOutput.Text = output.ToString();
                }
            }
            catch 
            {
                // Ignore parsing errors while typing
            }
        }
    }
}
