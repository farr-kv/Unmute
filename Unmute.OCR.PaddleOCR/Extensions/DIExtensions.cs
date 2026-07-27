using Unmute.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Unmute.OCR.PaddleOCR.Extensions
{
    public static class DIExtensions
    {
        public static IServiceCollection UsePaddleOCR(this IServiceCollection instance)
        {
            instance.AddSingleton<IOCREngine, PaddleOCREngine>();

            return instance;
        }
    }
}
