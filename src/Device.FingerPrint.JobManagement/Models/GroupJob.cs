namespace Device.FingerPrint.JobManagement.Models
{
    using System.Collections.Generic;

    public class GroupJob
    {
        public string Name { get; set; } = string.Empty;

        public string HealthCheck { get; set; } = string.Empty;

        public IEnumerable<Job>? Jobs { get; set; }        
    }
}
