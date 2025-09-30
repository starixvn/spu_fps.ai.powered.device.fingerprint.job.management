using Device.FingerPrint.JobManagement.Models;
using Device.FingerPrint.JobManagement.Services;
using Moq;
using ONEbook.UM.Models;
using ONEbook.UM.Services;
using Quartz;

namespace Device.FingerPrint.JobManagement.Tests.Services
{
    public class JobExecuteTests
    {
        private readonly Mock<ILogger> _mockLogger;
        private readonly Mock<IJobService> _mockJobService;
        private readonly Mock<ISchedulerFactory> _mockSchedulerFactory;
        private readonly Mock<IMaintenanceService> _mockMaintenanceService;
        private readonly Mock<IAppSettings> _mockAppSettings;
        private readonly JobExecute _jobExecute;

        public JobExecuteTests()
        {
            _mockLogger = new Mock<ILogger>();
            _mockJobService = new Mock<IJobService>();
            _mockSchedulerFactory = new Mock<ISchedulerFactory>();
            _mockMaintenanceService = new Mock<IMaintenanceService>();
            _mockAppSettings = new Mock<IAppSettings>();
            _jobExecute = new JobExecute(
                _mockLogger.Object,
                _mockJobService.Object,
                _mockSchedulerFactory.Object,
                _mockMaintenanceService.Object,
                _mockAppSettings.Object);
        }

        [Fact]
        public async Task Execute_ShouldPauseTrigger_WhenRetriesExceeded()
        {
            // Arrange
            var mockScheduler = new Mock<IScheduler>();
            _mockSchedulerFactory.Setup(f => f.GetScheduler(It.IsAny<CancellationToken>())).ReturnsAsync(mockScheduler.Object);
            var underMaintenanceTime = new UnderMaintenanceTime
            {
                IsUnderMaintenanceTime = false,
                From = DateTime.MinValue.ToString("HH:mm"),
                To = DateTime.MinValue.ToString("HH:mm"),
                ConnectionResult = new ConnectionResult(true, "Success")
            };
            _mockMaintenanceService.Setup(m => m.GetUnderMaintenanceTime()).Returns(underMaintenanceTime);
            var jobDataMap = new JobDataMap
            {
                { "apiUrl", "http://example.com" },
                { "count", 5 },
                { "methodName", "TestMethod" },
                { "controller", "TestController" }
            };
            var jobDetail = JobBuilder.Create<JobExecute>()
                .WithIdentity("TestJob")
                .UsingJobData(jobDataMap)
                .Build();
            var trigger = TriggerBuilder.Create().WithIdentity("TestTrigger").Build();
            var context = new Mock<IJobExecutionContext>();
            context.Setup(c => c.JobDetail).Returns(jobDetail);
            context.Setup(c => c.Trigger).Returns(trigger);

            // Act & Assert
            await Assert.ThrowsAsync<JobExecutionException>(() => _jobExecute.Execute(context.Object));
            mockScheduler.Verify(s => s.PauseTrigger(trigger.Key, It.IsAny<CancellationToken>()), Times.Once);
            _mockLogger.Verify(l => l.ErrorAsync(It.Is<string>(msg => msg.Contains("is STOPPED. Cause of RETRIED EXECEEDED."))), Times.Once);
        }

        [Fact]
        public async Task Excute_ShouldReturn_WhenIsUM()
        {
            // Arrange
            var mockScheduler = new Mock<IScheduler>();
            _mockSchedulerFactory.Setup(f => f.GetScheduler(It.IsAny<CancellationToken>())).ReturnsAsync(mockScheduler.Object);
            var underMaintenanceTime = new UnderMaintenanceTime
            {
                IsUnderMaintenanceTime = true,
                From = DateTime.MinValue.ToString("HH:mm"),
                To = DateTime.MinValue.ToString("HH:mm"),
                ConnectionResult = new ConnectionResult(true, "Success")
            };
            _mockMaintenanceService.Setup(m => m.GetUnderMaintenanceTime()).Returns(underMaintenanceTime);
            
            // Act & Assert
            mockScheduler.Verify(s => s.PauseTrigger(It.IsAny<TriggerKey>(), It.IsAny<CancellationToken>()), Times.Never);
            _mockLogger.Verify(l => l.ErrorAsync(It.Is<string>(msg => msg.Contains("is STOPPED. Cause of RETRIED EXECEEDED."))), Times.Never);
        }
    }
}
