using System.Runtime.InteropServices;

namespace Unmute.Core.Interops
{
    public static class ScreenCaptureInterop
    {
        [DllImport("Unmute.ScreenCapture.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool InitializeCapture();

        [DllImport("Unmute.ScreenCapture.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ShutdownCapture();

        [DllImport("Unmute.ScreenCapture.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool GetCaptureSize(
            out int width,
            out int height);

        [DllImport("Unmute.ScreenCapture.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern bool CaptureFrame(
            IntPtr destination,
            int destinationSize,
            out int width,
            out int height);
    }
}