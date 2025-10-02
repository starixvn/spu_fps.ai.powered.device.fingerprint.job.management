using Device.FingerPrint.JobManagement.Services;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Device.FingerPrint.JobManagement.Tests.Services
{
    public class JobServiceTests
    {
        [Fact]
        public async Task Execute_ShouldReturnFalse_WhenApiUrlIsInvalid()
        {
            // Arrange
            var mockJobService = new Mock<IJobService>();
            var jobService = mockJobService.Object;
            var jobName = "TestJob";
            var apiUrl = new Uri("http://invalid-url");
            var methodName = "TestMethod";
            var controller = "TestController";
            var data = new Dictionary<string, object>();
            mockJobService.Setup(js => js.Execute(jobName, apiUrl, methodName, controller, data)).ReturnsAsync(false);
            // Act
            var result = await jobService.Execute(jobName, apiUrl, methodName, controller, data);
            // Assert
            Assert.False(result);
            mockJobService.Verify(js => js.Execute(jobName, apiUrl, methodName, controller, data), Times.Once);
        }

        [Fact]
        public async Task Execute_ShouldReturnTrue_WhenApiUrlIsValid()
        {
            // Arrange
            var mockJobService = new Mock<IJobService>();
            var jobService = mockJobService.Object;
            var jobName = "TestJob";
            var apiUrl = new Uri("http://valid-url");
            var methodName = "TestMethod";
            var controller = "TestController";
            var data = new Dictionary<string, object>();
            mockJobService.Setup(js => js.Execute(jobName, apiUrl, methodName, controller, data)).ReturnsAsync(true);
            // Act
            var result = await jobService.Execute(jobName, apiUrl, methodName, controller, data);
            // Assert
            Assert.True(result);
            mockJobService.Verify(js => js.Execute(jobName, apiUrl, methodName, controller, data), Times.Once);
        }
    }
}
