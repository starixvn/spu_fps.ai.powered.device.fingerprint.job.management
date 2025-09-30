namespace Device.FingerPrint.JobManagement.Services
{
    using Device.FingerPrint.JobManagement.Models;
    using Fanex.NAP.SPU.Notification.Services;
    using ONEbook.UM.Services;
    using Quartz;
    using Quartz.Impl.Matchers;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    [DisallowConcurrentExecution, PersistJobDataAfterExecution]
    public class JobMonitoring : IJob
    {
        private readonly ILogger _logger;
        private readonly IScheduler _scheduler;
        private readonly IMaintenanceService _maintenanceService;
        private readonly IAppSettings _appSettings;

        public JobMonitoring(ILogger logger, IAppSettings appSettings, ISchedulerFactory schedulerFactory, IMaintenanceService maintenanceService)
        {
            _logger = logger;
            _scheduler = schedulerFactory.GetScheduler().Result;
            _maintenanceService = maintenanceService;
            _appSettings = appSettings;
        }

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                var isUM = _maintenanceService.GetUnderMaintenanceTime().IsUnderMaintenanceTime;

                if (!isUM)
                {

                    var allJobs = _scheduler.GetJobKeys(GroupMatcher<JobKey>.AnyGroup());

                    var allTriggerKeys = _scheduler.GetTriggerKeys(GroupMatcher<TriggerKey>.AnyGroup());
                    var result = new List<string>();

                    foreach (var triggerKey in allTriggerKeys.Result)
                    {

                        var triggerdetails = await _scheduler.GetTrigger(triggerKey);
                        var state = await _scheduler.GetTriggerState(triggerdetails.Key);

                        if (state == TriggerState.Paused)
                        {
                            var ignoreMonitoring = triggerdetails.JobDataMap.GetBoolean("ignoreMonitoring");
                            if (!ignoreMonitoring)
                                result.Add(triggerdetails.JobKey.Name);
                        }
                    }

                    if (result.Any())
                    {
                        var jobName = string.Join("", result.OrderBy(x => x.ToString()).Select(x => "\n - " + x));

                        Notification("Jobs Monitoring", "The JOBS are PAUSED.", "The List of jobs. \n" + jobName);
                    }
                }
            }
            catch (Exception ex)
            {
                await _logger.ErrorAsync("Job Monitoring Execute error", ex);
            }
        }

        private void Notification(string jobName, string message, string moreInfo)
        {
            var notification = new NotificationService();

            notification
                      .Config("0A40aOc7CtpzjbBSJu8KNWQRoXyjRvf9", _appSettings.TelegramGroupID, "https://bot.nexdev.net/skynex/api/messages/forwardwithtoken", _appSettings.TelegramProxy)
                      .Message(new MessageModel(jobName, DateTime.UtcNow.AddHours(7), _appSettings.Environment, message, moreInfo))
                      .SendWithDelay(300);
        }
    }
}
