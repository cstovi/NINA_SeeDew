using System;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Input;
using NINA.Core.Utility;
using NINA.Equipment.Interfaces.ViewModel;
using NINA.Profile.Interfaces;
using NINA.WPF.Base.ViewModel;
using NINA.Plugin.SeeDew.Services;

namespace NINA.Plugin.SeeDew.ViewModels {

    [Export(typeof(IDockableVM))]
    public class DewStatusViewModel : DockableVM {
        private readonly DewControlService _service;

        [ImportingConstructor]
        public DewStatusViewModel(IProfileService profileService, SeeDewPlugin plugin) : base(profileService) {
            _service = plugin.DewControlService;

            Title = "Dew Control";

            LogEntries = new ObservableCollection<string>();

            ClearLogCommand = new RelayCommand(_ => LogEntries.Clear());

            _service.CycleCompleted += OnCycleCompleted;
            _service.LogEntryAdded += OnLogEntry;
            _service.StatusChanged += OnStatusChanged;

            // Reflect initial state
            ServiceStatus = _service.Status;
        }

        public ICommand ClearLogCommand { get; }

        public ObservableCollection<string> LogEntries { get; }

        private double _temperature;
        public double Temperature {
            get => _temperature;
            set { _temperature = value; RaisePropertyChanged(); }
        }

        private double _dewPoint;
        public double DewPoint {
            get => _dewPoint;
            set { _dewPoint = value; RaisePropertyChanged(); }
        }

        private double _margin;
        public double Margin {
            get => _margin;
            set { _margin = value; RaisePropertyChanged(); }
        }

        private bool _heaterOn;
        public bool HeaterOn {
            get => _heaterOn;
            set { _heaterOn = value; RaisePropertyChanged(); RaisePropertyChanged(nameof(HeaterLabel)); }
        }

        public string HeaterLabel => _heaterOn ? "ON" : "OFF";

        private DewServiceStatus _serviceStatus;
        public DewServiceStatus ServiceStatus {
            get => _serviceStatus;
            set { _serviceStatus = value; RaisePropertyChanged(); RaisePropertyChanged(nameof(StatusLabel)); }
        }

        public string StatusLabel => _serviceStatus switch {
            DewServiceStatus.Running => "Running",
            DewServiceStatus.Error => "Error",
            _ => "Stopped"
        };

        private string? _lastError;
        public string? LastError {
            get => _lastError;
            set { _lastError = value; RaisePropertyChanged(); RaisePropertyChanged(nameof(HasError)); }
        }

        public bool HasError => !string.IsNullOrEmpty(_lastError);

        private void OnCycleCompleted(object sender, DewStateEventArgs e) {
            Application.Current.Dispatcher.Invoke(() => {
                Temperature = e.Temperature;
                DewPoint = e.DewPoint;
                Margin = e.Margin;
                HeaterOn = e.HeaterOn;
                LastError = _service.LastError;
            });
        }

        private void OnLogEntry(object sender, string entry) {
            Application.Current.Dispatcher.Invoke(() => {
                LogEntries.Insert(0, entry);
                while (LogEntries.Count > 100)
                    LogEntries.RemoveAt(LogEntries.Count - 1);
            });
        }

        private void OnStatusChanged(object sender, DewServiceStatus status) {
            Application.Current.Dispatcher.Invoke(() => {
                ServiceStatus = status;
                LastError = _service.LastError;
            });
        }
    }
}
