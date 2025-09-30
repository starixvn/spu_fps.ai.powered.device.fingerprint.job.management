namespace Device.FingerPrint.JobManagement.Models
{
    using System.Collections.Generic;

    public class Scheduler
    {
        public IEnumerable<GroupJob>? GroupJobs { get; set; }
    }
}
