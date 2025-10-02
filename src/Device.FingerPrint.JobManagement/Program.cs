using Device.FingerPrint.JobManagement.Models;
using Device.FingerPrint.JobManagement.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using ONEbook.UM.AspNetCore;
using ONEbook.UM.Configurations;
using ONEbook.UM.Helpers;
using ONEbook.UM.Services;
using ONEbook.UM.Transport;
using Quartz;
using SilkierQuartz;
using Refit;
using Fanex.Logging;
using Fanex.Logging.Sentry;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                     .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
                     .AddEnvironmentVariables();

builder.Services.Configure<QuartzOptions>(options =>
{
    options.Scheduling.IgnoreDuplicates = true; // default: false
    options.Scheduling.OverWriteExistingData = true; // default: true
});

builder.Services.AddSilkierQuartz(
    options => 
    {
        options.VirtualPathRoot = "/jobs";
        options.UseLocalTime = true;
        options.DefaultDateFormat = "yyyy-MM-dd";
        options.DefaultTimeFormat = "HH:mm:ss.fff";
        options.CronExpressionOptions = new CronExpressionDescriptor.Options
        {
            DayOfWeekStartIndexZero = false //Quartz uses 1-7 as the range
        };
        options.ProductName = "Device FingerPrint";
    },
    authenticationOptions =>
    {
        authenticationOptions.AuthScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        authenticationOptions.SilkierQuartzClaim = "Device";
        authenticationOptions.SilkierQuartzClaimValue = "FingerPrint";
        authenticationOptions.UserName = "admin";
        authenticationOptions.Password = "admin";
        authenticationOptions.AccessRequirement = SilkierQuartzAuthenticationOptions.SimpleAccessRequirement.AllowOnlyUsersWithClaim;
    },
    options =>
    {
        options.Add("quartz.plugin.recentHistory.storeType", "Quartz.Plugins.RecentHistory.Impl.InProcExecutionHistoryStore, Quartz.Plugins.RecentHistory");
        options.Add("quartz.plugin.recentHistory.type", "Quartz.Plugins.RecentHistory.ExecutionHistoryPlugin, Quartz.Plugins.RecentHistory");

    }
);

builder.Host.ConfigureSilkierQuartzHost();

builder.Services.AddHealthChecks();
builder.Services.AddOptions();

builder.Services.AddSingleton<ILogger, Logger>();
builder.Services.AddSingleton<IAppSettings, AppSettings>();
builder.Services.AddSingleton(new UMClientConfiguration().Initialize(builder.Configuration));
builder.Services.AddSingleton<IUMCacheService, UMCacheService>();
builder.Services.AddSingleton<IUMRestClient, UMRestClient>();
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.AddSingleton<IHttpContextHelper, HttpContextHelper>();
builder.Services.AddSingleton<IMaintenanceService, MaintenanceService>();

builder.Services.AddRefitClient<IJobServiceApi>();
builder.Services.AddSingleton<IJobService, JobService>();
builder.Services.AddSingleton<QuartzHostedService>();
builder.Services.AddHostedService<QuartzHostedService>();
builder.Services.AddSingleton<JobExecute>();
builder.Services.AddSingleton<JobMonitoring>();

builder.Services.AddControllers();

var app = builder.Build();

var appSettings = app.Services.GetRequiredService<IAppSettings>();

LogManager.SetDefaultLogCategory("FPS-Job-Management")
           .Use(
           [
                    new SentryLogging(new SentryEngineOptions
                    {
                        Dsn = appSettings.SentryDsn
                    })
           ]);

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseHealthChecks("/healthz", new HealthCheckOptions
{
    Predicate = _ => true
});

app.UseSilkierQuartz();

app.MapGet("/", context =>
{
    context.Response.Redirect("/jobs");
    return Task.CompletedTask;
});

app.Run();
