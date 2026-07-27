using Unmute.Core.Models;
using Unmute.Core.Services;
using OpenCvSharp;
using Sdcb.PaddleOCR;
using Sdcb.PaddleOCR.Models.Local;

namespace Unmute.OCR.PaddleOCR
{
    internal sealed class PaddleOCREngine : IOCREngine, IDisposable
    {
        private PaddleOcrAll ocr;

        public PaddleOCREngine()
        {
        }

        public Task InitializeAsync()
        {
            ocr = new PaddleOcrAll(LocalFullModels.EnglishV5, config =>
            {
                config.OneDnnEnabled = true;
            })
            {
                AllowRotateDetection = false,
                Enable180Classification = false,
            };
            return Task.CompletedTask;
        }

        public async Task<IEnumerable<OCRResult>> ReadTextAsync(byte[] imageBytes)
        {
            using Mat image = Cv2.ImDecode(imageBytes, ImreadModes.Color);
            var result = ocr.Run(image);
            return result.Regions.Select(r => new OCRResult(r.Text, r.Score));
        }

        public void Dispose()
        {
            ocr.Dispose();
        }
    }
}
