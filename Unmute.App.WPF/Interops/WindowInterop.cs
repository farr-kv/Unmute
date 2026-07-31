
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Interop;

namespace Unmute.App.WPF.Interops
{
    internal static class WindowInterop
    {
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_NOACTIVATE = 0x08000000;
        private const uint WDA_EXCLUDEFROMCAPTURE = 0x00000011;

        /// <summary>
        /// A top-level window created with this style does not become the foreground window when the user clicks it. The system does not bring this window to the foreground when the user minimizes or closes the foreground window.
        /// The window should not be activated through programmatic access or via keyboard navigation by accessible technology, such as Narrator.
        /// To activate the window, use the SetActiveWindow or SetForegroundWindow function.
        /// The window does not appear on the taskbar by default. To force the window to appear on the taskbar, use the WS_EX_APPWINDOW style.
        /// </summary>
        public static void SetNoActivate(this Window instance)
        {
            var hwnd = new WindowInteropHelper(instance).Handle;
            var exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, exStyle | WS_EX_NOACTIVATE);
        }

        /// <summary>
        /// The window is displayed only on a monitor. Everywhere else, the window does not appear at all.
        /// One use for this affinity is for windows that show video recording controls, so that the controls are not included in the capture.
        /// </summary>
        /// <param name="instance"></param>
        public static void SetExcludeFromCapture(this Window instance)
        {
            var hwnd = new WindowInteropHelper(instance).Handle;
            SetWindowDisplayAffinity(hwnd, WDA_EXCLUDEFROMCAPTURE);
        }

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        private static extern bool SetWindowDisplayAffinity(IntPtr hWnd, uint affinity);
    }
}
