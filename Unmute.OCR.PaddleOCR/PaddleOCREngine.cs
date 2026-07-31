using OpenCvSharp;
using OpenCvSharp.Extensions;
using Sdcb.PaddleOCR;
using Sdcb.PaddleOCR.Models.Local;
using System.Drawing;
using Unmute.Core.Models;
using Unmute.Core.Services;

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
                // TODO detect from cpu/gpu architecture
                config.OneDnnEnabled = true;
                config.CpuMathThreadCount = 8;
                config.UseGpu = true;
            })
            {
                AllowRotateDetection = false,
                Enable180Classification = false,
            };
            return Task.CompletedTask;
        }

        public Task<IEnumerable<OCRResult>> ReadTextAsync(Bitmap bitmap)
        {
            using Mat image = BitmapConverter.ToMat(bitmap);
            if (image.Channels() == 4)
                Cv2.CvtColor(image, image, ColorConversionCodes.BGRA2BGR);

            var results = ocr.Run(image).Regions
                .Where(r => r.Score > 0.5f)
                .Select(r =>
            {
                var rect = r.Rect.BoundingRect();
                return new InternalOCRResult
                {
                    Text = r.Text,
                    Bounds = new Bounds
                    {
                        Left = rect.Left,
                        Right = rect.Right,
                        Top = rect.Top,
                        Bottom = rect.Bottom
                    },
                    FontSize = rect.Bottom - rect.Top
                };
            });

            return Task.FromResult(this.MergeParagraphs(results)
                                       .Cast<OCRResult>());
        }

        public void Dispose()
        {
            ocr.Dispose();
        }

        private IEnumerable<InternalOCRResult> MergeParagraphs(IEnumerable<InternalOCRResult> regions)
        {
            var results = new List<InternalOCRResult>();
            foreach (var next in regions.OrderBy(x => x.Bounds.Top))
            {
                var current = results.LastOrDefault();
                if (current is not null &&
                    this.AreSameFontSize(current, next) && 
                    this.AreLeftAligned(current, next) && 
                    this.AreBelow(current, next))
                {
                    current.Text += Environment.NewLine + next.Text;
                    current.Bounds.Left = Math.Min(current.Bounds.Left, next.Bounds.Left);
                    current.Bounds.Top = Math.Min(current.Bounds.Top, next.Bounds.Top);
                    current.Bounds.Right = Math.Max(current.Bounds.Right, next.Bounds.Right);
                    current.Bounds.Bottom = Math.Max(current.Bounds.Bottom, next.Bounds.Bottom);
                }
                else
                {
                    Console.WriteLine($"{current?.Text} | {next.Text}");

                    results.Add(next);
                }
            }
            return results;
        }

        private bool AreSameFontSize(InternalOCRResult current, InternalOCRResult next)
        {
            // Lines shouldn't be more than 10% larger than the next
            var threshold = Math.Max(current.FontSize, next.FontSize) * 0.2f;
            return Math.Abs(current.FontSize - next.FontSize) < threshold;
        }

        private bool AreLeftAligned(InternalOCRResult current, InternalOCRResult next)
        {
            // Lines should follow the same indentation
            var threshold = Math.Max(current.FontSize, next.FontSize) * 0.5f;
            return Math.Abs(current.Bounds.Left - next.Bounds.Left) < threshold;
        }

        private bool AreBelow(InternalOCRResult current, InternalOCRResult next)
        {
            // There should not be a character's height of space between lines
            var threshold = Math.Max(current.FontSize, next.FontSize) * 0.6f;
            return Math.Abs(current.Bounds.Bottom - next.Bounds.Top) < threshold;
        }

        private class InternalOCRResult : OCRResult
        {
            public int FontSize { get; set; }
        }
    }
}
