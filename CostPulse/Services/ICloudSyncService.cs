using System;
using System.Threading.Tasks;

namespace CostPulse.Services
{
    public interface ICloudSyncService
    {
        bool IsSyncEnabled { get; set; }
        DateTime? LastSyncTime { get; }
        Task<bool> SyncAsync();
    }
}
