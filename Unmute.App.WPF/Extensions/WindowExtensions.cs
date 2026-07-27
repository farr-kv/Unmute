using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using Unmute.App.WPF.Interops;

namespace Unmute.App.WPF.Extensions
{
    internal static class WindowExtensions
    {
        public static void RunOnUiThread(this Window window, Action action)
        {
            if (window.Dispatcher.CheckAccess())
            {
                action();
            }
            else
            {
                window.Dispatcher.Invoke(action);
            }
        }

        public static void RegisterHotkey(this Window instance, int id, ModifierKeys modifiers, Key key, Action onPressed)
        {
            var helper = new WindowInteropHelper(instance);
            HotkeyInterop.UnregisterHotKey(helper.Handle, id);

            var source = HwndSource.FromHwnd(helper.Handle);
            source.AddHook((hwnd, msg, wParam, lParam, ref handled) =>
            {
                if (msg == HotkeyInterop.WM_HOTKEY && wParam.ToInt32() == id)
                {
                    handled = true;
                    onPressed();
                }

                return IntPtr.Zero;
            });

            var vk = KeyInterop.VirtualKeyFromKey(key);
            if (!HotkeyInterop.RegisterHotKey(helper.Handle, id, (uint)modifiers, (uint)vk))
                throw new InvalidOperationException("Could not register hotkey.");
        }

        public static void DeregisterHotkey(this Window instance, int id)
        {
            var helper = new WindowInteropHelper(instance);
            HotkeyInterop.UnregisterHotKey(helper.Handle, id);
        }
    }
}
