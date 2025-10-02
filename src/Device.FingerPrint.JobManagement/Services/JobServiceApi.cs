namespace Device.FingerPrint.JobManagement.Services
{
    using Refit;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface IJobServiceApi
    {
        [Post("/{controller}/{methodName}")]
        Task<bool> Execute(string jobName, string methodName, string controller, [Body(BodySerializationMethod.UrlEncoded)] Dictionary<string, object> data);
    }
}
