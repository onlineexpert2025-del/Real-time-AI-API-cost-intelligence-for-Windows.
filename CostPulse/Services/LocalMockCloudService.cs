using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace CostPulse.Services
{
    public class LocalMockCloudService : ICloudSyncService
    {
        private readonly DataService _dataService;
        private readonly LoggingService _logger;
        private readonly string _mockCloudPath;

        public bool IsSyncEnabled { get; set; } = false;
        public DateTime? LastSyncTime { get; private set; }

        public LocalMockCloudService(DataService dataService, LoggingService logger)
        {
            _dataService = dataService;
            _logger = logger;
            _mockCloudPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CostPulse", "cloud_sync_mock.json");
        }

        public async Task<bool> SyncAsync()
        {
            if (!IsSyncEnabled) return false;

            try
            {
                _logger.Log("Starting Cloud Sync (Mock)...");
                await Task.Delay(1500); // Simulate network latency

                // Mock Logic: Serialize current data to "Cloud" file
                var data = _dataService.Data;
                string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(_mockCloudPath, json);

                LastSyncTime = DateTime.Now;
                _logger.Log("Cloud Sync (Mock) completed successfully.");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError("Cloud Sync failed", ex);
                return false;
            }
        }
    }
}
