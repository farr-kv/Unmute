using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Unmute.Core.Interops
{
    internal static class ScreenCaptureInterop
    {
        [DllImport("Unmute.ScreenCapture.dll", SetLastError = true)]
        private static extern bool CaptureWindowToBuffer(IntPtr hwnd, out IntPtr buffer, out uint size);

        [DllImport("Unmute.ScreenCapture.dll")]
        private static extern void FreeCaptureBuffer(IntPtr buffer);

        public static byte[]? Capture(Process proc)
        {
            if (!CaptureWindowToBuffer(proc.MainWindowHandle, out IntPtr buffer, out uint size))
            {
                return null;
            }                

            try
            {
                var result = new byte[size];
                Marshal.Copy(buffer, result, 0, (int)size);
                return result;
            }
            finally
            {
                FreeCaptureBuffer(buffer);
            }
        }
    }
}