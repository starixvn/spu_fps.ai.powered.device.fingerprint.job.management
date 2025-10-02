using Quartz;

namespace Device.FingerPrint.JobManagement.Models
{
    public class Trigger
    {
        public string TriggerType { get; set; } = string.Empty;

        public string TriggerName { get; set; } = string.Empty;

        public string CronExpression { get; set; } = string.Empty;

        public int IntervalInSecond { get; set; }

        public string TimeZone { get; set; } = string.Empty;

        public bool IgnoreMonitoring { get; set; }

        public int Interval { get; set; }

        public IntervalUnit IntervalUnit { get; set; }
    }

}
