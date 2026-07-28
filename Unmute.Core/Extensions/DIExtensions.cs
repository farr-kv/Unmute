using Microsoft.Extensions.DependencyInjection;

namespace Unmute.Core.Extensions
{
    public static class DIExtensions
    {
        public static IServiceCollection AddUnmuteCore(this IServiceCollection instance)
        {
            return instance;
        }
    }
}
