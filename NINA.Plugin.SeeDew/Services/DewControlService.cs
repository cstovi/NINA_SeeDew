using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces.Mediator;

namespace NINA.Plugin.SeeDew.Services {

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
        private readonly ISwitchMediator _switchMediator;
        private readonly SeeDewSettings _settings;

        private CancellationTokenSource? _cts;
        private Task? _pollingTask;
        private bool _heaterOn;
        private string _logFilePath = "";

        private bool? _switchConnected = null;   // null = not yet checked this session
        private bool _weatherAvailable = false;

        public event EventHandler<DewStateEventArgs>? CycleCompleted;
        public event EventHandler<string>? LogEntryAdded;
        public event EventHandler<DewServiceStatus>? StatusChanged;

        public DewServiceStatus Status { get; private set; } = DewServiceStatus.Stopped;
        public string? LastError { get; private set; }
        public bool IsRunning => Status == DewServiceStatus.Running;

        public DewControlService(IWeatherDataMediator weatherMediator, ISwitchMediator switchMediator, SeeDewSettings settings) {
            _weatherMediator = weatherMediator;
            _switchMediator = switchMediator;
            _settings = settings;
        }

        public async Task StartAsync() {
            if (IsRunning) return;

            LastError = null;
            _switchConnected = null;
            _weatherAvailable = false;

            _logFilePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "NINA", "SeeDew", $"seedew_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.log");

            Log("Dew control starting");

            // Check switch on startup
            string switchStatusLine;
            var switchInfo = _switchMediator.GetInfo();
            if (!switchInfo.Connected) {
                _switchConnected = false;
                switchStatusLine = "⚠️ Seestar switch not connected in NINA — connect it in the Switch panel";
                Log("Seestar switch not connected in NINA");
            } else {
                var sw = switchInfo.WritableSwitches?.FirstOrDefault(s => s.Id == 0);
                _heaterOn = sw != null && sw.Value > 0.5;
                _switchConnected = true;
                switchStatusLine = $"✅ Seestar switch connected — heater currently {(_heaterOn ? "ON" : "OFF")}";
                Log($"Seestar switch connected — heater currently {(_heaterOn ? "ON" : "OFF")}");
            }

            // Check weather on startup
            var weather = _weatherMediator.GetInfo();
            _weatherAvailable = weather.Connected;
            if (!_weatherAvailable)
                Log("Weather source not connected in NINA");

            _cts = new CancellationTokenSource();
            _pollingTask = RunLoopAsync(_cts.Token);
            SetStatus(DewServiceStatus.Running);

            Log($"Running — on < {_settings.OnBelowThreshold:F1}°C, off > {_settings.OffAboveThreshold:F1}°C, every {_settings.PollIntervalMinutes}m");

            var lines = new List<string> {
                $"🔭 SeeDew started — every {_settings.PollIntervalMinutes}m | heater on <{_settings.OnBelowThreshold:F1}°C margin, off >{_settings.OffAboveThreshold:F1}°C margin",
                switchStatusLine
            };
            if (!_weatherAvailable) lines.Add("⚠️ No weather source connected in NINA — enable a weather plugin (e.g. SkyAlert, OpenWeatherMap, or an ASCOM weather station)");
            await NotifyDiscordAsync(string.Join("\n", lines));
        }

        public async Task StopAsync() {
            if (!IsRunning) return;

            _cts?.Cancel();
            if (_pollingTask != null) {
                try { await _pollingTask; } catch (OperationCanceledException) { }
            }

            try {
                if (_heaterOn && _switchConnected == true) {
                    await _switchMediator.SetSwitchValue((short)0, 0.0, null, CancellationToken.None);
                    _heaterOn = false;
                    Log("Heater OFF (shutdown)");
                }
            } catch (Exception ex) {
                Log($"Shutdown cleanup error: {ex.Message}");
            }

            SetStatus(DewServiceStatus.Stopped);
            Log("Dew control stopped");
            await NotifyDiscordAsync("🛑 SeeDew: Seestar dew control stopped (stop requested)");
        }

        private async Task RunLoopAsync(CancellationToken token) {
            while (!token.IsCancellationRequested) {
                try {
                    await ExecuteCycleAsync(token);
                } catch (OperationCanceledException) {
                    break;
                } catch (Exception ex) {
                    LastError = ex.Message;
                    Log($"Unexpected error: {ex.Message}");
                }

                try {
                    await Task.Delay(TimeSpan.FromMinutes(_settings.PollIntervalMinutes), token);
                } catch (OperationCanceledException) {
                    break;
                }
            }
        }

        private async Task ExecuteCycleAsync(CancellationToken token) {
            // --- Weather dependency check ---
            var weather = _weatherMediator.GetInfo();
            bool weatherNow = weather.Connected;

            if (weatherNow != _weatherAvailable) {
                _weatherAvailable = weatherNow;
                if (weatherNow) {
                    Log("Weather source connected");
                    await NotifyDiscordAsync("✅ SeeDew: Weather source connected — temperature readings resumed");
                } else {
                    Log("Weather source disconnected");
                    await NotifyDiscordAsync("⚠️ SeeDew: Weather source offline — cannot read temperature/dew point. Enable a weather plugin in NINA (e.g. SkyAlert, OpenWeatherMap, or an ASCOM weather station).");
                }
            }

            // --- Switch / Seestar dependency check ---
            var switchInfo = _switchMediator.GetInfo();
            bool switchNow = switchInfo.Connected;
            bool currentHeater = _heaterOn;

            if (switchNow) {
                var sw = switchInfo.WritableSwitches?.FirstOrDefault(s => s.Id == 0);
                currentHeater = sw != null && sw.Value > 0.5;
            }

            if (_switchConnected == null || switchNow != _switchConnected.Value) {
                if (!switchNow) {
                    await NotifyDiscordAsync("⚠️ SeeDew: Seestar switch not connected in NINA — connect it in the Switch panel");
                } else if (_switchConnected == false) {
                    Log("Seestar switch reconnected");
                    await NotifyDiscordAsync("✅ SeeDew: Seestar switch reconnected");
                }
                _switchConnected = switchNow;
            }

            if (!weatherNow) {
                Log("Skipping heater check — weather offline");
                return;
            }
            if (!switchNow) {
                Log("Skipping heater check — switch not connected");
                return;
            }

            // --- Normal heater control cycle ---
            double temp   = weather.Temperature;
            double dew    = weather.DewPoint;
            double margin = temp - dew;

            bool newHeater = currentHeater;
            if (!currentHeater && margin < _settings.OnBelowThreshold)
                newHeater = true;
            else if (currentHeater && margin > _settings.OffAboveThreshold)
                newHeater = false;

            bool stateChanged = newHeater != currentHeater;

            if (stateChanged) {
                await _switchMediator.SetSwitchValue((short)0, newHeater ? 1.0 : 0.0, null, token);
                _heaterOn = newHeater;
                string action = newHeater ? "turned ON" : "turned OFF";
                string emoji  = newHeater ? "🌡️" : "✅";
                Log($"Heater {action} — Temp:{temp:F1}°C  Dew:{dew:F1}°C  Margin:{margin:F1}°C");
                await NotifyDiscordAsync($"{emoji} SeeDew: Seestar dew heater {action} — Temp:{temp:F1}°C  Dew:{dew:F1}°C  Margin:{margin:F1}°C");
            }

            CycleCompleted?.Invoke(this, new DewStateEventArgs(temp, dew, margin, newHeater, stateChanged));
        }

        private void Log(string message) {
            var entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
            LogEntryAdded?.Invoke(this, entry);
            WriteToLog(entry);
        }

        private void WriteToLog(string line) {
            if (string.IsNullOrEmpty(_logFilePath)) return;
            try {
                Directory.CreateDirectory(Path.GetDirectoryName(_logFilePath)!);
                File.AppendAllText(_logFilePath, line + Environment.NewLine);
            } catch (Exception ex) {
                Logger.Warning($"[SeeDew] Log write failed: {ex.Message}");
            }
        }

        private async Task NotifyDiscordAsync(string message) {
            if (string.IsNullOrWhiteSpace(_settings.DiscordWebhookUrl)) return;
            WriteToLog($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [Discord] {message}");
            try {
                using var http = new HttpClient();
                await http.PostAsync(_settings.DiscordWebhookUrl,
                    new System.Net.Http.StringContent(
                        $"{{\"content\":{Newtonsoft.Json.JsonConvert.ToString(message)}}}",
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
