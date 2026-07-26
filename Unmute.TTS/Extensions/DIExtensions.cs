using Unmute.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Unmute.TTS.Extensions
{
    public static class DIExtensions
    {
        public static IServiceCollection UseTTS(this IServiceCollection instance)
        {
            instance.AddSingleton<ITtsService, TtsService>();

            return instance;
        }
    }
}
