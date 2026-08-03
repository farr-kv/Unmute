using System.Drawing;
using Tesseract;
using Unmute.Core.Models;

namespace Unmute.OCR.Services.Implementations
{
    internal sealed class TesseractOcrEngine : IOcrEngine
    {
        private readonly TesseractEngine engine;
        
        public TesseractOcrEngine()
        {
            engine = new TesseractEngine(".\\", "eng", EngineMode.Default);
        }

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }

        public Task<IEnumerable<OCRResult>> ReadTextAsync(Bitmap bitmap)
        {
            var results = new List<OCRResult>();

            using var ms = new MemoryStream();
            bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);

            using var img = Pix.LoadFromMemory(ms.ToArray());
            using var page = engine.Process(img);
            using var iter = page.GetIterator();

            do
            {
                var text = iter.GetText(PageIteratorLevel.TextLine);
                if (string.IsNullOrWhiteSpace(text))
                    continue;

                if (iter.TryGetBoundingBox(PageIteratorLevel.TextLine, out Rect rect))
                {
                    results.Add(new OCRResult
                    {
                        Text = text.Trim().Replace("\r", "").Replace("\n", " "),
                        Bounds = new Bounds
                        {
                            Left = rect.X1,
                            Right = rect.X2,
                            Top = rect.Y1,
                            Bottom = rect.Y2
                        }
                    });
                }
            }
            while (iter.Next(PageIteratorLevel.TextLine));
            return Task.FromResult(Paragrapher.MergeParagraphs(results));
        }

        public void Dispose()
        {
            engine.Dispose();
        }
    }
}