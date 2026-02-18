using System;
using System.Linq;
using CostPulse.Models;

namespace CostPulse.Services
{
    public class DataService
    {
        private readonly StorageService _storage;
        private readonly PricingService _pricingService;
        private readonly TotalsService _totalsService;
        private readonly ExportService _exportService;
        private readonly LoggingService _logger;
        public readonly LicenseService LicenseService;

        public CostPulseData Data { get; private set; }

        public event Action DataChanged;

        public DataService(StorageService storage, PricingService pricingService, TotalsService totalsService, ExportService exportService, LoggingService logger, LicenseService licenseService)
        {
            _storage = storage ?? throw new ArgumentNullException(nameof(storage));
            _pricingService = pricingService ?? throw new ArgumentNullException(nameof(pricingService));
            _totalsService = totalsService ?? throw new ArgumentNullException(nameof(totalsService));
            _exportService = exportService ?? throw new ArgumentNullException(nameof(exportService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            LicenseService = licenseService ?? throw new ArgumentNullException(nameof(licenseService));
            
            LoadData();
        }

        private void LoadData()
        {
            Data = _storage.Load();
            
            // Auto-Migration: Ensure default pricing models exist
            var defaults = new AppSettings().PricingModels; // Get fresh defaults
            bool changed = false;

            if (Data.Settings == null) Data.Settings = new AppSettings();
            if (Data.Settings.PricingModels == null) Data.Settings.PricingModels = new System.Collections.Generic.List<PricingModel>();

            foreach (var def in defaults)
            {
                // Check if model exists (case-insensitive)
                var exists = Data.Settings.PricingModels.Any(p => p.ModelName.Equals(def.ModelName, StringComparison.OrdinalIgnoreCase));
                if (!exists)
                {
                    Data.Settings.PricingModels.Add(def);
                    _logger.Log($"Pricing model auto-migrated: {def.ModelName}");
                    changed = true;
                }
            }

            if (changed)
            {
                SaveData();
            }

            // Sync pricing settings to service
            if (Data.Settings?.PricingModels != null)
            {
                _pricingService.UpdatePriceTable(Data.Settings.PricingModels);
            }

            // Validate License
            if (Data.Settings != null)
            {
                LicenseService.DeveloperMode = Data.Settings.DeveloperMode;
                LicenseService.ValidateLicense(Data.Settings.LicenseKey);
                _logger.Log($"License Status: {(LicenseService.IsPro ? "PRO" : "FREE")}");
            }
        }

        public void SaveData()
        {
            _storage.Save(Data);
            DataChanged?.Invoke();
        }

        public void AddEntry(UsageEntry entry)
        {
            // Calculate cost using PricingService
            string normalizedModel = entry.ModelName?.Trim() ?? "Unknown";
            var pricing = _pricingService.GetPricingForModel(normalizedModel);
            
            // Debug Logging for Pricing Lookup
            _logger.Log($"Pricing Lookup: Input='{entry.ModelName}', Normalized='{normalizedModel}'");
            if (pricing != null)
            {
                _logger.Log($"Matched Pricing: Model='{pricing.ModelName}', Input=${pricing.InputPricePerMillion:F4}/M, Output=${pricing.OutputPricePerMillion:F4}/M");
            }
            else
            {
                _logger.Log("Matched Pricing: None (Using $0.00)");
            }

            entry.Cost = _pricingService.CalculateCost(entry.InputTokens, entry.OutputTokens, pricing);
            _logger.Log($"Calculated Cost: ${entry.Cost:F6}");
            
            Data.Entries.Add(entry);
            SaveData();
            _logger.Log($"Added entry: {entry.Provider} / {entry.ModelName} - ${entry.Cost:F4}");
        }

        public void DeleteEntry(UsageEntry entry)
        {
            if (Data.Entries.Remove(entry))
            {
                SaveData();
                _logger.Log("Deleted entry.");
            }
        }

        public void ClearHistory()
        {
            Data.Entries.Clear();
            SaveData();
            _logger.Log("Cleared all history.");
        }

        public void UpdateSettings(AppSettings newSettings)
        {
            Data.Settings = newSettings;
            if (Data.Settings.PricingModels != null)
            {
                _pricingService.UpdatePriceTable(Data.Settings.PricingModels);
            }
            
            // Re-validate in case user entered a new key
            LicenseService.DeveloperMode = Data.Settings.DeveloperMode;
            LicenseService.ValidateLicense(Data.Settings.LicenseKey);
            _logger.Log($"Settings updated. License Status: {(LicenseService.IsPro ? "PRO" : "FREE")}");

            SaveData();
        }
        
        public void ExportData(string filePath)
        {
             _exportService.ExportToCsv(Data.Entries, filePath);
        }

        // Aggregation delegation
        public decimal GetSessionTotal() => _totalsService.GetSessionTotal(Data.Entries);
        public decimal GetTodayTotal() => _totalsService.GetTodayTotal(Data.Entries);
        public decimal GetMonthTotal() => _totalsService.GetMonthTotal(Data.Entries);
        public int GetTokensToday() => _totalsService.GetTokensToday(Data.Entries);
        public int GetTokensMonth() => _totalsService.GetTokensMonth(Data.Entries);
        public System.Collections.Generic.List<decimal> GetLast7DaysTotals() => _totalsService.GetDailyTotals(Data.Entries, 7);
    }
}
