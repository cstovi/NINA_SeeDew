using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NINA.Equipment.Interfaces.Mediator;

namespace NINA.Plugin.DewSee.Services {

    public enum DewServiceStatus { Stopped, Running, Error }

    public class DewStateEventArgs : EventArgs {
        public double Temperature { get; }
        public double DewPoint { get; }
        public double Margin { get; }
        public bool HeaterOn { get; }
        public bool StateChanged { get; }

        public DewStateEventArgs(double temp, double dew, double margin, bool heaterOn, bool stateChanged) {
            Temperature = temp;
            DewPoint = dew;
            Margin = margin;
            HeaterOn = heaterOn;
            StateChanged = stateChanged;
        }
    }

    public class DewControlService {
        private readonly IWeatherDataMediator _weatherMediator;
        private readonly SeestarAlpacaClient _alpaca;
        private readonly DewSeeSettings _settings;

        private CancellationTokenSource? _cts;
        private Task? _pollingTask;
        private bool _heaterOn;

        public event EventHandler<DewStateEventArgs>? CycleCompleted;
        public event EventHandler<string>? LogEntryAdded;
        public event EventHandler<DewServiceStatus>? StatusChanged;

        public DewServiceStatus Status { get; private set; } = DewServiceStatus.Stopped;
        public string? LastError { get; private set; }

        public DewControlService(IWeatherDataMediator weatherMediator, SeestarAlpacaClient alpaca, DewSeeSettings settings) {
            _weatherMediator = weatherMediator;
            _alpaca = alpaca;
            _settings = settings;
        }

        public bool IsRunning => Status == DewServiceStatus.Running;

        public async Task StartAsync() {
            if (IsRunning) return;

            LastError = null;
            Log("Dew control starting");

            try {
                if (_settings.ManageConnection)
                    await _alpaca.ConnectAsync();

                _heaterOn = await _alpaca.GetSwitchStateAsync();
                _cts = new CancellationTokenSource();
                _pollingTask = RunLoopAsync(_cts.Token);

                SetStatus(DewServiceStatus.Running);
                Log($"Running — on<{_settings.OnBelowThreshold:F1}°C, off>{_settings.OffAboveThreshold:F1}°C, every {_settings.PollIntervalMinutes}m");
                await NotifyDiscordAsync($"🔭 DewSee started — checking every {_settings.PollIntervalMinutes}m (on <{_settings.OnBelowThreshold:F1}°C margin, off >{_settings.OffAboveThreshold:F1}°C margin)");
            } catch (Exception ex) {
                LastError = ex.Message;
                SetStatus(DewServiceStatus.Error);
                Log($"Start error: {ex.Message}");
            }
        }

        public async Task StopAsync() {
            if (!IsRunning) return;

            _cts?.Cancel();
            if (_pollingTask != null) {
                try { await _pollingTask; } catch (OperationCanceledException) { }
            }

            try {
                if (_heaterOn) {
                    await _alpaca.SetSwitchStateAsync(false);
                    _heaterOn = false;
                    Log("Heater OFF (shutdown)");
                }
                if (_settings.ManageConnection)
                    await _alpaca.DisconnectAsync();
            } catch (Exception ex) {
                Log($"Shutdown cleanup error: {ex.Message}");
            }

            SetStatus(DewServiceStatus.Stopped);
            Log("Dew control stopped");
            await NotifyDiscordAsync("🛑 DewSee stopped");
        }

        private async Task RunLoopAsync(CancellationToken token) {
            while (!token.IsCancellationRequested) {
                try {
                    await ExecuteCycleAsync(token);
                } catch (OperationCanceledException) {
                    break;
                } catch (Exception ex) {
                    LastError = ex.Message;
                    Log($"Error: {ex.Message}");
                    await NotifyDiscordAsync($"⚠️ DewSee error: {ex.Message}");
                    // Don't stop on transient errors — keep trying
                }

                try {
                    await Task.Delay(TimeSpan.FromMinutes(_settings.PollIntervalMinutes), token);
                } catch (OperationCanceledException) {
                    break;
                }
            }
        }

        private async Task ExecuteCycleAsync(CancellationToken token) {
            var weather = _weatherMediator.GetInfo();
            if (!weather.Connected) {
                Log("Weather device not connected — skipping cycle");
                return;
            }

            double temp = weather.Temperature;
            double dew = weather.DewPoint;
            double margin = temp - dew;

            bool currentHeater = await _alpaca.GetSwitchStateAsync();
            bool newHeater = currentHeater;

            if (!currentHeater && margin < _settings.OnBelowThreshold)
                newHeater = true;
            else if (currentHeater && margin > _settings.OffAboveThreshold)
                newHeater = false;

            bool stateChanged = newHeater != currentHeater;

            if (stateChanged) {
                await _alpaca.SetSwitchStateAsync(newHeater);
                _heaterOn = newHeater;
                string action = newHeater ? "ON " : "OFF";
                string emoji = newHeater ? "🌡️" : "✅";
                Log($"Heater {action} — Temp:{temp:F1}°C  Dew:{dew:F1}°C  Margin:{margin:F1}°C");
                await NotifyDiscordAsync($"{emoji} DewSee heater {action.Trim()} — Temp:{temp:F1}°C  Dew:{dew:F1}°C  Margin:{margin:F1}°C");
            } else {
                Log($"No change  — Temp:{temp:F1}°C  Dew:{dew:F1}°C  Margin:{margin:F1}°C  Heater:{ (currentHeater ? "ON" : "OFF")}");
            }

            CycleCompleted?.Invoke(this, new DewStateEventArgs(temp, dew, margin, newHeater, stateChanged));
        }

        private void Log(string message) {
            var entry = $"[{DateTime.Now:HH:mm:ss}] {message}";
            LogEntryAdded?.Invoke(this, entry);
        }

        private async Task NotifyDiscordAsync(string message) {
            if (string.IsNullOrWhiteSpace(_settings.DiscordWebhookUrl)) return;
            try {
                using var http = new HttpClient();
                await http.PostAsync(_settings.DiscordWebhookUrl,
                    new System.Net.Http.StringContent(
                        $"{{\"content\":\"{message.Replace("\"", "\\\"")}\"}}",
                        System.Text.Encoding.UTF8, "application/json"));
            } catch (Exception ex) {
                Log($"Discord notification failed: {ex.Message}");
            }
        }

        private void SetStatus(DewServiceStatus status) {
            Status = status;
            StatusChanged?.Invoke(this, status);
        }
    }
}
