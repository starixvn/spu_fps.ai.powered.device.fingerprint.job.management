using System.ComponentModel;

namespace Device.FingerPrint.JobManagement.Models
{
    public interface IAppSettings
    {
        string Environment { get; }

        string SentryDsn { get; }

        string TelegramProxy { get; }

        string TelegramGroupID { get; }

        Scheduler Schedulers { get; }
    }

    public class AppSettings : IAppSettings
    {
        private readonly IConfiguration configuration;

        public string Environment { get; } = string.Empty;

        public string SentryDsn { get; } = string.Empty;

        public string TelegramProxy { get; } = string.Empty;

        public string TelegramGroupID { get; } = string.Empty;

        public Scheduler Schedulers { get; } = new Scheduler();

        public AppSettings(IConfiguration configuration)
        {
            this.configuration = configuration;

            Environment = GetValue<string>("Environment");
            SentryDsn = GetValue<string>("Sentry:Dsn");
            TelegramProxy = GetValue<string>("Telegram:Proxy");
            TelegramGroupID = GetValue<string>("Telegram:GroupID");
            Schedulers = GetSection<Scheduler>(nameof(Schedulers));
        }

        private T GetValue<T>(string key)
        {
            try
            {
                var value = configuration[key];

                if (typeof(T) == typeof(bool))
                {
                    if (string.Equals(value, "1", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(value, "true", StringComparison.OrdinalIgnoreCase))
                    {
                        return (T)(object)true;
                    }
                    if (string.Equals(value, "0", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(value, "false", StringComparison.OrdinalIgnoreCase))
                    {
                        return (T)(object)false;
                    }
                }

                var typeConverter = TypeDescriptor.GetConverter(typeof(T));
                return (T)typeConverter.ConvertFromString(value);
            }
            catch (Exception ex)
            {
                throw new InvalidCastException($"Key: {key}", ex);
            }
        }

        private T GetSection<T>(string key)
        {
            var section = configuration.GetSection(key);
            if (section == null)
            {
                return default;
            }
            return section.Get<T>();
        }
    }
}
