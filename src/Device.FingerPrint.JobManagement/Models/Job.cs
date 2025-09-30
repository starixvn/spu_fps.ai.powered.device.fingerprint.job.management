namespace Device.FingerPrint.JobManagement.Models
{
    using System.Collections.Generic;
    public class Job
    {
        public string JobName { get; set; } = string.Empty;

        public string JobType { get; set; } = string.Empty;

        public string APIUrl { get; set; } = string.Empty;

        public string MethodName { get; set; } = string.Empty;

        public string Controller { get; set; } = string.Empty;

        public bool Active { get; set; }

        public IEnumerable<Trigger>? Triggers { get; set; }        
        
    }
}
