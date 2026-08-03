using AdonisUI.Controls;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using Unmute.Core.Models;
using Unmute.Core.Services;
using Unmute.OCR;
using Unmute.OCR.Services;

namespace Unmute.App.WPF.UI.Windows.Settings
{
    public partial class SettingsWindow : AdonisWindow, INotifyPropertyChanged, IDisposable
    {        
        public ObservableCollection<Voice> AvailableVoices { get; }
        public ObservableCollection<OcrEngineType> AvailableOcrEngines { get; }

        public Voice SelectedVoice 
        {
            get => this.ttsService.Voice;
            set
            {
                this.ttsService.Voice = value;
                this.OnPropertyChanged();
            }
        }

        public OcrEngineType SelectedOcrEngine
        {
            get => this.ocrService.SelectedEngineType;
            set
            {
                this.ocrService.InitializeAsync(value).Wait();
                this.OnPropertyChanged();
            }
        }

        private readonly ITtsService ttsService;
        private readonly IOcrService ocrService;

        public SettingsWindow(ITtsService ttsService, IOcrService ocrService)
        {
            this.ttsService = ttsService;
            this.ocrService = ocrService;
            this.InitializeComponent();

            this.AvailableVoices = new ObservableCollection<Voice>(this.ttsService.AvailableVoices);
            this.AvailableOcrEngines = new ObservableCollection<OcrEngineType>(Enum.GetValues<OcrEngineType>().Where(x => x is not OcrEngineType.None));

            this.OnPropertyChanged(nameof(AvailableVoices));
            this.OnPropertyChanged(nameof(AvailableOcrEngines));
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