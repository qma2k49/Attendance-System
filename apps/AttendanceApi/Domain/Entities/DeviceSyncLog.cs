using AttendanceApi.Domain.Enums;

namespace AttendanceApi.Domain.Entities
{
    public class DeviceSyncLog
    {
        public int Id { get; set; }
        public int DeviceId { get; set; }
        public string SyncType { get; set; } = "AUTO_SCHEDULED";
        public int RecordsPulled { get; set; } = 0;
        public int RecordsInserted { get; set; } = 0;
        public SyncStatus Status { get; set; } = SyncStatus.Success;
        public string? ErrorMessage { get; set; }
        public DateTime SyncedAt { get; set; } = DateTime.UtcNow;
        

        public virtual AttendanceDevice? AttendanceDevice { get; set; }
    }
}