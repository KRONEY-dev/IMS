using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Shared.Kernel.Extensions
{
    public static class DependencyInjectionExtensions
    {
        public static void ConfigureOption<TOption>(this IServiceCollection services, IConfiguration configuration)
            where TOption : class
        {
            services.Configure<TOption>(configuration.GetSection(typeof(TOption).Name));
        }
    }
}