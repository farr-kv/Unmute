using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Unmute.Core.Interops;

namespace Unmute.Core.Services.Implemenations
{
    public class ScreenCaptureService : IScreenCaptureService, IDisposable
    {
        private readonly byte[] buffer;

        public ScreenCaptureService()
        {
            ScreenCaptureInterop.InitializeCapture();

            ScreenCaptureInterop.GetCaptureSize(out int width, out int height);
            this.buffer = new byte[width * height * 4];
        }

        public Bitmap CaptureFrame()
        {
            var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            try
            {
                IntPtr ptr = handle.AddrOfPinnedObject();
                ScreenCaptureInterop.CaptureFrame(
                    ptr,
                    buffer.Length,
                    out var width,
                    out var height);

                var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
                var data = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                Marshal.Copy(this.buffer, 0, data.Scan0, this.buffer.Length);
                bitmap.UnlockBits(data);
                return bitmap;
            }
            finally
            {
                handle.Free();
            }
        }

        public void Dispose()
        {
            ScreenCaptureInterop.ShutdownCapture();
        }
    }
}
