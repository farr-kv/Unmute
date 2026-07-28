using AdonisUI.Controls;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Unmute.App.WPF.Extensions;
using Unmute.App.WPF.UI.Windows.Snipping;
using Unmute.Core.Interops;
using Unmute.Core.Models;
using Unmute.Core.Services;

namespace Unmute.App.WPF.UI.Windows.Settings
{
    public partial class SettingsWindow : AdonisWindow, INotifyPropertyChanged, IDisposable
    {
        private const int HOTKEY_READ_SCREENSHOT = 9000;
        private const int HOTKEY_READ_APP = 9001;

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

        private readonly ITtsService ttsService;
        private readonly IOCREngine ocrEngine;

        public SettingsWindow(ITtsService ttsService, IOCREngine ocrEngine)
        {
            this.ttsService = ttsService;
            this.ocrEngine = ocrEngine;
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

        private void OnSelectedProcessChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (this.SelectedProcess is null)
            {
                this.ImageCrop.PreviewImage = null;
                return;
            }

            var bytes = ScreenCaptureInterop.Capture(this.SelectedProcess);
            this.ImageCrop.PreviewImage = this.ToImageSource(bytes);
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            foreach (var voice in this.ttsService.AvailableVoices)
            {
                this.AvailableVoices.Add(voice);
            }

            this.RegisterHotkey(HOTKEY_READ_SCREENSHOT,
                System.Windows.Input.ModifierKeys.Control | System.Windows.Input.ModifierKeys.Shift,
                System.Windows.Input.Key.T,
                async () =>
                {
                    var bmp = await SnippingWindow.TakeScreenshotAsync();
                    if (bmp is null)
                        return;

                    using var ms = new MemoryStream();
                    bmp.Save(ms, ImageFormat.Png);

                    var ocrResults = await this.ocrEngine.ReadTextAsync(ms.ToArray());
                    if (ocrResults is null)
                        return;

                    var text = string.Join(Environment.NewLine, ocrResults.Where(x => x.Confidence > 0.75).Select(x => x.Text));
                    await this.ttsService.NarrateAsync(text);
                });

            this.RegisterHotkey(HOTKEY_READ_APP,
                System.Windows.Input.ModifierKeys.Control | System.Windows.Input.ModifierKeys.Shift,
                System.Windows.Input.Key.R,
                async () =>
                {
                    if (!this.IsProcessRunning())
                        return;

                    var bytes = ScreenCaptureInterop.Capture(this.SelectedProcess!);
                    if (bytes is null)
                        return;

                    var cropped = this.ApplyCropToImage(bytes);
                    var ocrResults = await this.ocrEngine.ReadTextAsync(cropped);
                    if (ocrResults is null)
                        return;

                    var text = string.Join(Environment.NewLine, ocrResults.Where(x => x.Confidence > 0.75).Select(x => x.Text));
                    await this.ttsService.NarrateAsync(text);
                });
        }

        public void Dispose()
        {
            this.DeregisterHotkey(HOTKEY_READ_SCREENSHOT);
            this.DeregisterHotkey(HOTKEY_READ_APP);
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

        private byte[] ApplyCropToImage(byte[] bytes)
        {
            var cropAreaPercentages = this.ImageCrop.CropArea;

            using var ms = new MemoryStream(bytes);
            using var bmp = new Bitmap(ms);
            var cropArea = new Rectangle
            {
                X = (int)(bmp.Width * cropAreaPercentages.Left),
                Y = (int)(bmp.Height * cropAreaPercentages.Top),
                Width = (int)(bmp.Width * cropAreaPercentages.Width),
                Height = (int)(bmp.Height * cropAreaPercentages.Height),
            };

            using var cropped = bmp.Clone(cropArea, bmp.PixelFormat);
            using var outputStream = new MemoryStream();
            cropped.Save(outputStream, ImageFormat.Png);

            return outputStream.ToArray();
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