using AdonisUI.Controls;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using Unmute.Core.Models;
using Unmute.Core.Services;

namespace Unmute.App.WPF.UI.Windows.Settings
{
    public partial class SettingsWindow : AdonisWindow, INotifyPropertyChanged, IDisposable
    {        
        public ObservableCollection<Voice> AvailableVoices { get; } = new ();

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

        public SettingsWindow(ITtsService ttsService)
        {
            this.ttsService = ttsService;
            this.InitializeComponent();
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            foreach (var voice in this.ttsService.AvailableVoices)
            {
                this.AvailableVoices.Add(voice);
            }
        }

        public void Dispose()
        {
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

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        #endregion
    }
}