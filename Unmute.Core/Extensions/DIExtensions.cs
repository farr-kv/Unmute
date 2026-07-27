using Microsoft.Extensions.DependencyInjection;
using Unmute.Core.Services;
using Unmute.Core.Services.Implementations;

namespace Unmute.Core.Extensions
{
    public static class DIExtensions
    {
        public static IServiceCollection AddUnmuteCore(this IServiceCollection instance)
        {
            instance.AddSingleton<IApplicationMonitor, ApplicationMonitor>();

            return instance;
        }
    }
}
