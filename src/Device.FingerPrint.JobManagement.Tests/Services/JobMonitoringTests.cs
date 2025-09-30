using Device.FingerPrint.JobManagement.Models;
using Device.FingerPrint.JobManagement.Services;
using Moq;
using ONEbook.UM.Models;
using ONEbook.UM.Services;
using Quartz;
using Quartz.Impl.Matchers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Device.FingerPrint.JobManagement.Tests.Services
{
    public class JobMonitoringTests
    {
        private readonly Mock<ILogger> _mockLogger;
        private readonly Mock<IAppSettings> _mockAppSettings;
        private readonly Mock<ISchedulerFactory> _mockSchedulerFactory;
        private readonly Mock<IMaintenanceService> _mockMaintenanceService;

        private readonly JobMonitoring _jobMonitoring;

        public JobMonitoringTests()
        {
            _mockLogger = new Mock<ILogger>();
            _mockAppSettings = new Mock<IAppSettings>();
            _mockSchedulerFactory = new Mock<ISchedulerFactory>();
            _mockMaintenanceService = new Mock<IMaintenanceService>();
            _jobMonitoring = new JobMonitoring(
                _mockLogger.Object,
                _mockAppSettings.Object,
                _mockSchedulerFactory.Object,
                _mockMaintenanceService.Object);
        }

        [Fact]
        public async Task Execute_ShouldReturn_WhenUnderMaintenance()
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
            var context = new Mock<IJobExecutionContext>();
            // Act
            await _jobMonitoring.Execute(context.Object);
            // Assert
            _mockSchedulerFactory.Verify(f => f.GetScheduler(It.IsAny<CancellationToken>()), Times.Once);
            _mockMaintenanceService.Verify(m => m.GetUnderMaintenanceTime(), Times.Once);
            mockScheduler.Verify(s => s.GetTriggerKeys(It.IsAny<GroupMatcher<TriggerKey>>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Execute_ShouldLogError_WhenExceptionThrown()
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
            mockScheduler.Setup(s => s.GetTriggerKeys(It.IsAny<GroupMatcher<TriggerKey>>(), It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("Test exception"));
            var context = new Mock<IJobExecutionContext>();
            // Act
            await _jobMonitoring.Execute(context.Object);
            // Assert
            _mockSchedulerFactory.Verify(f => f.GetScheduler(It.IsAny<CancellationToken>()), Times.Once);
            _mockMaintenanceService.Verify(m => m.GetUnderMaintenanceTime(), Times.Once);
            mockScheduler.Verify(s => s.GetTriggerKeys(It.IsAny<GroupMatcher<TriggerKey>>(), It.IsAny<CancellationToken>()), Times.Once);
            _mockLogger.Verify(l => l.ErrorAsync(It.Is<string>(msg => msg.Contains("Job Monitoring Execute error")), It.IsAny<Exception>()), Times.Once);
        }
    }
}
