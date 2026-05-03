using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Plugin;
using NINA.Plugin.Interfaces;
using NINA.Plugin.DewSee.Services;
using NINA.Equipment.Interfaces;

namespace NINA.Plugin.DewSee {

    [Export(typeof(IPluginManifest))]
    [Export]
    public class DewSeePlugin : PluginBase, IPluginManifest, INotifyPropertyChanged {

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void RaisePropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        private readonly IWeatherDataMediator _weatherMediator;
        private readonly ISwitchMediator _switchMediator;

        public DewControlService DewControlService { get; }
        public DewSeeSettings Settings { get; }

        [ImportingConstructor]
        public DewSeePlugin(IWeatherDataMediator weatherMediator, ISwitchMediator switchMediator) {
            _weatherMediator = weatherMediator;
            _switchMediator = switchMediator;

            Settings = DewSeeSettings.Load();
            DewControlService = new DewControlService(_weatherMediator, _switchMediator, Settings);

            OnBelowThreshold = Settings.OnBelowThreshold;
            OffAboveThreshold = Settings.OffAboveThreshold;
            PollIntervalMinutes = Settings.PollIntervalMinutes;
            DiscordWebhookUrl = Settings.DiscordWebhookUrl;

            SaveSettingsCommand = new RelayCommand(_ => ApplyAndSave());
        }

        public override async Task Teardown() {
            await DewControlService.StopAsync();
            await base.Teardown();
        }

        // Options UI commands
        public ICommand SaveSettingsCommand { get; }

        private void ApplyAndSave() {
            Settings.OnBelowThreshold = OnBelowThreshold;
            Settings.OffAboveThreshold = OffAboveThreshold;
            Settings.PollIntervalMinutes = PollIntervalMinutes;
            Settings.DiscordWebhookUrl = DiscordWebhookUrl;
            Settings.Save();
        }

        // Bindable settings properties for the Options UI
        private double _onBelowThreshold = 3.5;
        public double OnBelowThreshold {
            get => _onBelowThreshold;
            set { _onBelowThreshold = value; RaisePropertyChanged(); }
        }

        private double _offAboveThreshold = 5.0;
        public double OffAboveThreshold {
            get => _offAboveThreshold;
            set { _offAboveThreshold = value; RaisePropertyChanged(); }
        }

        private int _pollIntervalMinutes = 1;
        public int PollIntervalMinutes {
            get => _pollIntervalMinutes;
            set { _pollIntervalMinutes = value; RaisePropertyChanged(); }
        }

        private string _discordWebhookUrl = "";
        public string DiscordWebhookUrl {
            get => _discordWebhookUrl;
            set { _discordWebhookUrl = value; RaisePropertyChanged(); }
        }

    }
}
