using System;

namespace CostPulse.Services
{
    public class ServiceContainer
    {
        public LoggingService Logger { get; private set; }
        public DataService Data { get; private set; }
        public ICloudSyncService CloudSync { get; private set; }
        public ClipboardMonitorService ClipboardMonitor { get; private set; }
        public LogImportService LogImporter { get; private set; }

        public ServiceContainer()
        {
            Logger = new LoggingService();
            InitializeServices();
        }

        private void InitializeServices()
        {
            try
            {
                var storage = new StorageService(Logger);
                var pricing = new PricingService(null); // Load default internally or via DataService
                var totals = new TotalsService();
                var export = new ExportService(Logger);
                var license = new LicenseService();

                Data = new DataService(storage, pricing, totals, export, Logger, license);
                
                ClipboardMonitor = new ClipboardMonitorService(Data, Logger);
                LogImporter = new LogImportService(Data, Logger);
                CloudSync = new LocalMockCloudService(Data, Logger);
            }
            catch (Exception ex)
            {
                Logger.LogError("Failed to initialize services", ex);
                throw;
            }
        }

        public void StartServices()
        {
            Logger.Log("Starting background services...");
            ClipboardMonitor.Start();
            LogImporter.Start();
        }

        public void StopServices()
        {
            Logger.Log("Stopping background services...");
            ClipboardMonitor?.Stop();
            LogImporter?.Stop();
        }
    }
}
