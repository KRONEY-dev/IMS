using AccountsService.Application.Exceptions;
using AccountsService.Application.Mappings;
using AccountsService.Application.Options;
using AccountsService.Application.Services;
using AccountsService.Application.Services.Interfaces;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Kernel.Exceptions;
using Shared.Kernel.Extensions;
using Shared.Kernel.Mapping;
using System.Reflection;

namespace AccountsService.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection InjectApplication(this IServiceCollection services, IConfiguration configuration)
        {
            AddServices(services);
            AddMapping(services, configuration);
            ConfigureOptions(services, configuration);

            return services;
        }

        private static void AddServices(IServiceCollection services)
        {
            services.AddSingleton<IExceptionMapperService, AccountsExceptionMapperSingletonService>();

            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

            services.AddScoped<IAuthService, AuthScopedService>();
            services.AddScoped<IUserService, UserScopedService>();
        }

        private static void AddMapping(IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<IMapperWrapper, AutoMapperObjectMapper>();
            services.AddAutoMapper(cfg =>
            {
                cfg.LicenseKey = configuration["AutoMapper:LicenseKey"];
            });
        }

        private static void ConfigureOptions(IServiceCollection services, IConfiguration configuration)
        {
            services.ConfigureOption<JwtSettings>(configuration);
        }
    }
}