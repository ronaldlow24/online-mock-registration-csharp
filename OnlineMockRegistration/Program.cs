using Amazon.Runtime;
using Amazon.SQS;
using Microsoft.AspNetCore.RateLimiting;
using OnlineMockRegistration;
using OnlineMockRegistration.Cron;
using Quartz;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

//insert environment variable to class
ApplicationConfiguration.StaticCurrent = builder.Configuration.GetSection("ApplicationConfiguration").Get<ApplicationConfiguration>();

//aws
var awsOption = builder.Configuration.GetAWSOptions();
awsOption.Region = Amazon.RegionEndpoint.GetBySystemName(ApplicationConfiguration.StaticCurrent.AWSRegion);
awsOption.Credentials = new BasicAWSCredentials(ApplicationConfiguration.StaticCurrent.AWSKey, ApplicationConfiguration.StaticCurrent.AWSSecret);
builder.Services.AddDefaultAWSOptions(awsOption);
builder.Services.AddAWSService<IAmazonSQS>();

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddQuartz(q =>
{
    q.UseMicrosoftDependencyInjectionJobFactory();
    // Just use the name of your job that you created in the Jobs folder.
    var jobKey = new JobKey("TriggerCronJob");
    q.AddJob<TriggerCronJob>(opts => opts.WithIdentity(jobKey));

    q.AddTrigger(opts => opts
        .ForJob(jobKey)
        .WithIdentity("TriggerCronJob-trigger")
        .WithCronSchedule("0/5 * * * * ? *")
    );
});

//rate limit
builder.Services.AddRateLimiter(_ => _
    .AddFixedWindowLimiter(policyName: "fixed", options =>
    {
        options.PermitLimit = 4;
        options.Window = TimeSpan.FromSeconds(12);
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        options.QueueLimit = 2;
    })
    .OnRejected = (context, _) => {

        // inject Retry-After header (too much line wrapping, I know)
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter,
            out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter =
                ((int)retryAfter.TotalSeconds).ToString();
        }
        // return a different status code
        context.HttpContext.Response.StatusCode =
            StatusCodes.Status429TooManyRequests;
        return new();
    }
);

// Quartz.Extensions.Hosting hosting
builder.Services.AddQuartzHostedService(options =>
{
    options.WaitForJobsToComplete = true;
});


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseRateLimiter();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}").RequireRateLimiting("fixed");

app.Run();
