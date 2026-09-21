using AccountsService.Application.Options;
using AccountsService.Application.Repositories.Interfaces;
using AccountsService.Application.Services.Interfaces;
using AccountsService.Infrastructure.Database;
using AccountsService.Infrastructure.Database.Repositories;
using AccountsService.Infrastructure.Jobs;
using AccountsService.Infrastructure.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Kernel.AspNetCore.Requests;
using Shared.Kernel.AspNetCore.Requests.Middleware;
using Shared.Kernel.Database;
using Shared.Kernel.Extensions;
using Shared.Kernel.Quartz;
using Shared.Kernel.Redis;
using Shared.Kernel.Requests;

namespace AccountsService.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection InjectInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            ConfigureOptions(services, configuration);
            AddServices(services, configuration);
            AddQuartzJobs(services, configuration);

            return services;
        }

        public static void UseExceptionHandling(this IApplicationBuilder app)
        {
            app.UseMiddleware<ExceptionHandlingMiddleware>();
        }

        public static void UseRequestContext(this IApplicationBuilder app)
        {
            app.UseMiddleware<RequestContextMiddleware>();
        }

        private static void AddServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<AccountsDbContext>(options =>
                options.UseNpgsql(configuration.GetConnectionString("Accounts")),
                optionsLifetime: ServiceLifetime.Singleton);

            services.AddDbContextFactory<AccountsDbContext>();

            services.AddScoped<IUnitOfWork, UnitOfWorkScoped>();
            services.AddScoped<IUserRepository, UserRepositoryScoped>();
            services.AddScoped<IRefreshTokenRepository, RefreshTokenRepositoryScoped>();

            services.AddSingleton<ITokenSignerService, TokenSignerSingletonService>();
            services.AddSingleton<IPasswordHasherService, PasswordHasherSingletonService>();

            services.AddRedisAccessTokenBlacklist(configuration);
            services.AddRedisDistributedLock(configuration);

            services.AddScoped<IRequestContext, RequestContextScoped>();
        }

        private static void AddQuartzJobs(IServiceCollection services, IConfiguration configuration)
        {
            services.AddQuartzWithCronJobs(quartz =>
            {
                quartz.AddCronJob<RefreshTokenCleanupJob, RefreshTokenCleanupJobSettings>(configuration);
            });
        }

        private static void ConfigureOptions(IServiceCollection services, IConfiguration configuration)
        {
            services.ConfigureOption<RefreshTokenCleanupJobSettings>(configuration);
        }
    }
}