using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using System.Windows.Input;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces.Mediator;
using NINA.Plugin;
using NINA.Plugin.Interfaces;
using NINA.Plugin.DewSee.Services;

namespace NINA.Plugin.DewSee {

    [Export(typeof(IPluginManifest))]
    [Export]
    public class DewSeePlugin : PluginBase, IPluginManifest {
        private readonly IWeatherDataMediator _weatherMediator;

        public DewControlService DewControlService { get; }
        public DewSeeSettings Settings { get; }

        [ImportingConstructor]
        public DewSeePlugin(IWeatherDataMediator weatherMediator) {
            _weatherMediator = weatherMediator;

            Settings = DewSeeSettings.Load();
            var alpaca = new SeestarAlpacaClient(() => Settings.AlpacaBaseUrl);
            DewControlService = new DewControlService(_weatherMediator, alpaca, Settings);

            if (Settings.AutoStart)
                Task.Run(() => DewControlService.StartAsync());

            // Bindable properties for the options UI
            AlpacaHost = Settings.AlpacaHost;
            AlpacaPort = Settings.AlpacaPort;
            OnBelowThreshold = Settings.OnBelowThreshold;
            OffAboveThreshold = Settings.OffAboveThreshold;
            PollIntervalMinutes = Settings.PollIntervalMinutes;
            DiscordWebhookUrl = Settings.DiscordWebhookUrl;
            AutoStart = Settings.AutoStart;
            ManageConnection = Settings.ManageConnection;

            SaveSettingsCommand = new RelayCommand(_ => ApplyAndSave());
        }

        public override async Task Teardown() {
            await DewControlService.StopAsync();
            await base.Teardown();
        }

        // Options UI commands
        public ICommand SaveSettingsCommand { get; }

        private void ApplyAndSave() {
            Settings.AlpacaHost = AlpacaHost;
            Settings.AlpacaPort = AlpacaPort;
            Settings.OnBelowThreshold = OnBelowThreshold;
            Settings.OffAboveThreshold = OffAboveThreshold;
            Settings.PollIntervalMinutes = PollIntervalMinutes;
            Settings.DiscordWebhookUrl = DiscordWebhookUrl;
            Settings.AutoStart = AutoStart;
            Settings.ManageConnection = ManageConnection;
            Settings.Save();
        }

        // Bindable settings properties for the Options UI
        private string _alpacaHost = "localhost";
        public string AlpacaHost {
            get => _alpacaHost;
            set { _alpacaHost = value; RaisePropertyChanged(); }
        }

        private int _alpacaPort = 5555;
        public int AlpacaPort {
            get => _alpacaPort;
            set { _alpacaPort = value; RaisePropertyChanged(); }
        }

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

        private bool _autoStart = true;
        public bool AutoStart {
            get => _autoStart;
            set { _autoStart = value; RaisePropertyChanged(); }
        }

        private bool _manageConnection = false;
        public bool ManageConnection {
            get => _manageConnection;
            set { _manageConnection = value; RaisePropertyChanged(); }
        }
    }
}
