namespace Device.FingerPrint.JobManagement.Services
{
    using Device.FingerPrint.JobManagement.Models;
    using Microsoft.Extensions.Hosting;
    using Quartz;
    using Quartz.Impl.Matchers;
    using Quartz.Spi;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    public class QuartzHostedService : IHostedService
    {
        private readonly ISchedulerFactory _schedulerFactory;
        private readonly IJobFactory _jobFactory;
        private readonly IScheduler _scheduler;

        public QuartzHostedService(ISchedulerFactory schedulerFactory,
            IJobFactory jobFactory,
            IAppSettings _appSettings)
        {
            _schedulerFactory = schedulerFactory;
            _jobFactory = jobFactory;
            _scheduler = _schedulerFactory.GetScheduler().Result;
            JobSchedulers = _appSettings.Schedulers;
        }

        public Scheduler JobSchedulers { get; set; }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (JobSchedulers.GroupJobs != null && JobSchedulers.GroupJobs.Any())
            {
                _scheduler.JobFactory = _jobFactory;

                var groupJobs = JobSchedulers.GroupJobs;

                foreach (var groupJob in groupJobs)
                {
                    groupJob.Jobs.ToList().ForEach(job =>
                    {
                        var jobDetail = BuildJob(job, groupJob.Name);

                        job.Triggers.ToList().ForEach(trigger =>
                        {
                            var triggerDetail = BuildTrigger(trigger, groupJob.Name);

                            _scheduler.ScheduleJob(jobDetail, triggerDetail, cancellationToken);

                            if (!job.Active)
                            {
                                _scheduler.PauseTrigger(new TriggerKey(trigger.TriggerName, groupJob.Name));
                            }
                        });
                    });

                    if (IsPauseTrigger(groupJob.Jobs))
                    {
                        await _scheduler.PauseTriggers(GroupMatcher<TriggerKey>.GroupEquals(groupJob.Name));
                    }
                }

                await _scheduler.Start(cancellationToken);
            }
        }

        private IJobDetail BuildJob(Job job, string groupName)
        {
            return JobBuilder
                .Create(Type.GetType(job.JobType))
                .WithIdentity(job.JobName, groupName)
                .WithDescription(job.JobName)
                .UsingJobData("apiUrl", job.APIUrl)
                .UsingJobData("methodName", job.MethodName)
                .UsingJobData("controller", job.Controller)
                .Build();
        }

        private ITrigger BuildTrigger(Trigger trigger, string groupName)
        {
            var triggerBuilder = TriggerBuilder
                .Create()
                .WithIdentity(trigger.TriggerName, groupName)
                .WithDescription(trigger.TriggerName)
                .UsingJobData("ignoreMonitoring", trigger.IgnoreMonitoring)
                .StartNow();

            if (trigger.TriggerType != "Cron") //simple
            {
                if (trigger.TriggerType == "Calendar") //calendar
                {

                    triggerBuilder.WithCalendarIntervalSchedule(action =>
                    {
                        action.InTimeZone(TimeZoneInfo.FindSystemTimeZoneById(trigger.TimeZone));

                        action.WithInterval(trigger.Interval, trigger.IntervalUnit);                       

                    });
                }
                else
                {
                    triggerBuilder.WithSimpleSchedule(x => x.WithIntervalInSeconds(trigger.IntervalInSecond).RepeatForever());
                }
            }
            else // cron
            {
                if (!string.IsNullOrWhiteSpace(trigger.TimeZone))
                {
                    triggerBuilder.WithCronSchedule(trigger.CronExpression, x => x.InTimeZone(TimeZoneInfo.FindSystemTimeZoneById(trigger.TimeZone)));
                }
                else
                {
                    triggerBuilder.WithCronSchedule(trigger.CronExpression);
                }
            }

            return triggerBuilder.Build();
        }

        private bool IsPauseTrigger(IEnumerable<Job> jobs)
        {
            return !jobs.Any(x => x.Active);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            var scheduler = await _schedulerFactory.GetScheduler(cancellationToken);

            await scheduler.Shutdown(cancellationToken);
        }
    }
}
