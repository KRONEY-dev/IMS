using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Shared.Kernel.Scheduling;

namespace Shared.Kernel.Quartz
{
    public static class QuartzExtensions
    {
        public static IServiceCollection AddQuartzWithCronJobs(this IServiceCollection services, Action<IQuartzBuilder>? configure)
        {
            services.AddQuartz(quartz =>
            {
                configure?.Invoke(quartz);
            });

            services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);

            return services;
        }

        public static void AddCronJob<TJob, TSettings>(this IQuartzBuilder quartz, IConfiguration configuration)
            where TJob : IJob
            where TSettings : class, ICronJobSettings
        {
            var jobKey = new JobKey(typeof(TJob).Name);
            var cronExpression =
                configuration[$"{typeof(TSettings).Name}:{nameof(ICronJobSettings.CronExpression)}"]!;

            quartz.AddJob<TJob>(job => job.WithIdentity(jobKey));
            quartz.AddTrigger<TJob>(trigger => trigger
                .ForJob(jobKey)
                .WithCronSchedule(cronExpression));
        }
    }
}