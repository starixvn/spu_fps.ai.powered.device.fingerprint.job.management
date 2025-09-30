namespace Device.FingerPrint.JobManagement.Services
{
    using Quartz;
    using Sentry;
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Web;

    public static class JobHelper
    {
        public static string GetUrl(string apiUrl)
        {
            if (!string.IsNullOrWhiteSpace(apiUrl))
            {
                var uri = new Uri(apiUrl);

                return string.IsNullOrWhiteSpace(uri.Query) ? apiUrl : uri.AbsoluteUri.Replace(uri.Query, string.Empty);
            }

            return string.Empty;
        }

        public static Dictionary<string, object> GetQueryData(string apiUrl)
        {
            if (!string.IsNullOrWhiteSpace(apiUrl))
            {
                var uri = new Uri(apiUrl);

                if (string.IsNullOrWhiteSpace(uri.Query)) return new Dictionary<string, object>();

                var parameters = HttpUtility.ParseQueryString(uri.Query);
                var result = new Dictionary<string, object>();

                foreach (var param in parameters.AllKeys)
                {
                    result.Add(param, parameters[param]);
                }

                return result;
            }

            return new Dictionary<string, object>();
        }

        public static JobExecutionException JobExecuteFailed(string errorMessage, ISentryClient sentryClient)
        {
            var jobExecutionException = new JobExecutionException(errorMessage);

            sentryClient.CaptureMessage(errorMessage, SentryLevel.Error);

            Thread.Sleep(60000);

            jobExecutionException.RefireImmediately = true;

            return jobExecutionException;
        }
    }
}
