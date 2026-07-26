using Unmute.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Unmute.OCR.Extensions
{
    public static class DIExtensions
    {
        public static IServiceCollection UseOCR(this IServiceCollection instance)
        {
            instance.AddSingleton<IOCREngine, OCREngine>();

            return instance;
        }
    }
}
