using AdonisUI.Controls;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Unmute.App.WPF.Extensions;
using Unmute.Core.Models;
using Unmute.Core.Services;

namespace Unmute.App.WPF.UI.Windows.Settings
{
    public partial class SettingsWindow : AdonisWindow, INotifyPropertyChanged, IDisposable
    {
        private const int HOTKEY_READ_SCREEN = 9000;
        private const int HOTKEY_AUTO_READ = 9001;

        public ObservableCollection<Process> RunningProcesses { get; } = new ();
        public ObservableCollection<Voice> AvailableVoices { get; } = new ();

        public Process? SelectedProcess { 
            get => field;
            set
            {
                field = value;
                this.OnPropertyChanged();
            }
        }
        public Voice SelectedVoice 
        {
            get => this.ttsService.Voice;
            set
            {
                this.ttsService.Voice = value;
                this.OnPropertyChanged();
            }
        }
        public ImageSource? PreviewImage { 
            get => field;
            set { 
                field = value;
                this.OnPropertyChanged();
            }
        }

        private readonly IApplicationMonitor appMonitor;
        private readonly ITtsService ttsService;

        public SettingsWindow(IApplicationMonitor appMonitor, ITtsService ttsService)
        {
            this.appMonitor = appMonitor;
            this.ttsService = ttsService;
            this.InitializeComponent();
        }

        private void OnLoadRunningProcesses(object sender, EventArgs e)
        {
            this.RunningProcesses.Clear();

            var windowedProcesses = Process.GetProcesses().Where(p => p.MainWindowHandle != IntPtr.Zero);
            foreach (var process in windowedProcesses)
            {
                this.RunningProcesses.Add(process);
            }
        }

        private async void OnSelectedProcessChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (this.SelectedProcess is null)
            {
                this.PreviewImage = null;
                return;
            }

            var bytes = await this.appMonitor.GetProcessScreenshotAsync(this.SelectedProcess);
            this.PreviewImage = this.ToImageSource(bytes);
        }

        private void OnPreviewChanged(object sender, EventArgs e)
        {
            // TODO resizable rectangle
            // TODO draggable rectangle
            // TODO translate Top,Left,Bottom,Right bounds to a perc of total height/width to account for resizing of either this or the source application
            this.CropControl.Width = this.PreviewControl?.ActualWidth ?? 0;
            this.CropControl.Height = this.PreviewControl?.ActualHeight ?? 0;
            this.SelectionRect.Width = this.PreviewControl?.ActualWidth ?? 0;
            this.SelectionRect.Height = this.PreviewControl?.ActualHeight ?? 0;
            Canvas.SetTop(this.SelectionRect, 0);
            Canvas.SetLeft(this.SelectionRect, 0);
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            foreach (var voice in this.ttsService.AvailableVoices)
            {
                this.AvailableVoices.Add(voice);
            }

            // TODO make this key rebindable
            this.RegisterHotkey(HOTKEY_READ_SCREEN,
                System.Windows.Input.ModifierKeys.Shift,
                System.Windows.Input.Key.R,
                async () =>
                {
                    if (this.SelectedProcess is null)
                        return;

                    if (this.IsProcessRunning())
                    {
                        var text = await this.appMonitor.MonitorProcessAsync(this.SelectedProcess);
                        await this.ttsService.NarrateAsync(text);
                    }
                });
        }

        public void Dispose()
        {
            this.DeregisterHotkey(HOTKEY_READ_SCREEN);
            this.DeregisterHotkey(HOTKEY_AUTO_READ);
        }

        private void Titlebar_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void ButtonPreviewVoice_Click(object sender, RoutedEventArgs e)
        {
            this.ttsService.NarrateAsync($"Hello World! My name is {this.SelectedVoice.Name}");
        }

        private bool IsProcessRunning()
        {
            if (this.SelectedProcess is null)
                return false;

            bool processExited;
            try
            {
                this.SelectedProcess.Refresh();
                processExited = this.SelectedProcess.HasExited;
            }
            catch (InvalidOperationException)
            {
                processExited = true;
            }

            // TODO display error message underneath the dropdown box
            return this.SelectedProcess != null;
        }

        public ImageSource? ToImageSource(byte[]? imageData)
        {
            if (imageData == null || imageData.Length == 0)
                return null;

            using var stream = new MemoryStream(imageData);

            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.StreamSource = stream;
            bitmap.EndInit();
            bitmap.Freeze();

            return bitmap;
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        #endregion
    }
}