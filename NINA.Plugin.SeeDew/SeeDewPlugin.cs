using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Plugin;
using NINA.Plugin.Interfaces;
using NINA.Plugin.SeeDew.Services;

namespace NINA.Plugin.SeeDew {

    [Export(typeof(IPluginManifest))]
    [Export]
    public class SeeDewPlugin : PluginBase, IPluginManifest, INotifyPropertyChanged {

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void RaisePropertyChanged([CallerMemberName] string? propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private readonly IWeatherDataMediator _weatherMediator;
        private readonly ISwitchMediator _switchMediator;

        private bool _isInitializing;
        private bool _isSyncing;

        public DewControlService DewControlService { get; }
        public SeeDewSettings Settings { get; }

        [ImportingConstructor]
        public SeeDewPlugin(IWeatherDataMediator weatherMediator, ISwitchMediator switchMediator) {
            _weatherMediator = weatherMediator;
            _switchMediator = switchMediator;

            Settings = SeeDewSettings.Load();
            DewControlService = new DewControlService(_weatherMediator, _switchMediator, Settings);

            _isInitializing = true;
            OnBelowThreshold = Settings.OnBelowThreshold;
            OffAboveThreshold = Settings.OffAboveThreshold;
            PollIntervalMinutes = Settings.PollIntervalMinutes;
            DiscordWebhookUrl = Settings.DiscordWebhookUrl;
            _isInitializing = false;
        }

        public override async Task Teardown() {
            await DewControlService.StopAsync();
            await base.Teardown();
        }

        private void SyncAndSaveSettings() {
            if (_isInitializing || _isSyncing) return;
            _isSyncing = true;
            try {
                Settings.OnBelowThreshold = _onBelowThreshold;
                Settings.OffAboveThreshold = _offAboveThreshold;
                Settings.PollIntervalMinutes = _pollIntervalMinutes;
                Settings.DiscordWebhookUrl = _discordWebhookUrl;
                Settings.Save();
            } finally {
                _isSyncing = false;
            }
        }

        /// <summary>
        /// Refreshes runtime fields from persisted settings so sequencer runs pick up option changes
        /// made since plugin construction.
        /// </summary>
        public void RefreshRuntimeSettingsFromDisk() {
            try {
                var latest = SeeDewSettings.Load();

                Settings.OnBelowThreshold = latest.OnBelowThreshold;
                Settings.OffAboveThreshold = latest.OffAboveThreshold;
                Settings.PollIntervalMinutes = latest.PollIntervalMinutes;
                Settings.DiscordWebhookUrl = latest.DiscordWebhookUrl;

                _isSyncing = true;
                try {
                    _onBelowThreshold = latest.OnBelowThreshold;
                    _offAboveThreshold = latest.OffAboveThreshold;
                    _pollIntervalMinutes = latest.PollIntervalMinutes;
                    _discordWebhookUrl = latest.DiscordWebhookUrl;
                    RaisePropertyChanged(nameof(OnBelowThreshold));
                    RaisePropertyChanged(nameof(OffAboveThreshold));
                    RaisePropertyChanged(nameof(PollIntervalMinutes));
                    RaisePropertyChanged(nameof(DiscordWebhookUrl));
                } finally {
                    _isSyncing = false;
                }
            } catch { }
        }

        // Bindable settings properties for the Options UI
        private double _onBelowThreshold = 3.5;
        public double OnBelowThreshold {
            get => _onBelowThreshold;
            set { _onBelowThreshold = value; RaisePropertyChanged(); SyncAndSaveSettings(); }
        }

        private double _offAboveThreshold = 5.0;
        public double OffAboveThreshold {
            get => _offAboveThreshold;
            set { _offAboveThreshold = value; RaisePropertyChanged(); SyncAndSaveSettings(); }
        }

        private int _pollIntervalMinutes = 1;
        public int PollIntervalMinutes {
            get => _pollIntervalMinutes;
            set { _pollIntervalMinutes = value; RaisePropertyChanged(); SyncAndSaveSettings(); }
        }

        private string _discordWebhookUrl = "";
        public string DiscordWebhookUrl {
            get => _discordWebhookUrl;
            set { _discordWebhookUrl = value; RaisePropertyChanged(); SyncAndSaveSettings(); }
        }

    }
}
