using Unmute.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Unmute.TTS.PocketTTS.Extensions
{
    public static class DIExtensions
    {
        public static IServiceCollection UsePocketTTS(this IServiceCollection instance)
        {
            instance.AddSingleton<ITtsService, PocketTtsService>();

            return instance;
        }
    }
}
