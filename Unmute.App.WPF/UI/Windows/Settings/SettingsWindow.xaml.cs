using AdonisUI.Controls;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using Unmute.App.WPF.Extensions;
using Unmute.Core.Models;
using Unmute.Core.Services;

namespace Unmute.App.WPF.UI.Windows.Settings
{
    public partial class SettingsWindow : AdonisWindow, INotifyPropertyChanged, IDisposable
    {
        private const int HOTKEY_READ_SCREEN = 9000;
        private const int HOTKEY_AUTO_READ = 9001;

        public event PropertyChangedEventHandler? PropertyChanged;
        public ObservableCollection<Process> RunningProcesses { get; } = new ();
        public ObservableCollection<Voice> AvailableVoices { get; } = new ();

        public Process? SelectedProcess { get; set; }
        public Voice SelectedVoice 
        {
            get => this.ttsService.Voice;
            set => this.ttsService.Voice = value;
        }

        private readonly IApplicationMonitor appMonitor;
        private readonly ITtsService ttsService;

        public SettingsWindow(IApplicationMonitor appMonitor, ITtsService ttsService)
        {
            this.appMonitor = appMonitor;
            this.ttsService = ttsService;
            this.InitializeComponent();
        }

        public void OnLoadRunningProcesses(object sender, EventArgs e)
        {
            this.RunningProcesses.Clear();

            var windowedProcesses = Process.GetProcesses().Where(p => p.MainWindowHandle != IntPtr.Zero);
            foreach (var process in windowedProcesses)
            {
                this.RunningProcesses.Add(process);
            }
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

        private void ButtonPreview_Click(object sender, RoutedEventArgs e)
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
    }
}