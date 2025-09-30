using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using ONEbook.UM.AspNetCore;
using ONEbook.UM.Configurations;
using ONEbook.UM.Helpers;
using ONEbook.UM.Services;
using ONEbook.UM.Transport;
using Quartz;
using SilkierQuartz;

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
        options.CronExpressionOptions = new CronExpressionDescriptor.Options()
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
    }
);

builder.Services.AddHealthChecks();
builder.Services.AddOptions();

builder.Services.AddSingleton(new UMClientConfiguration().Initialize(builder.Configuration));
builder.Services.AddSingleton<IUMCacheService, UMCacheService>();
builder.Services.AddSingleton<IUMRestClient, UMRestClient>();
builder.Services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
builder.Services.AddSingleton<IHttpContextHelper, HttpContextHelper>();
builder.Services.AddSingleton<IMaintenanceService, MaintenanceService>();

builder.Services.AddControllers();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.UseHealthChecks("/healthz", new HealthCheckOptions()
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
