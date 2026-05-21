using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using NINA.Core.Model;
using NINA.Sequencer.SequenceItem;

namespace NINA.Plugin.SeeDew.Sequencer {

    [Export(typeof(ISequenceItem))]
    [ExportMetadata("Name", "SeeDew Start Dew Control")]
    [ExportMetadata("Description", "Starts the SeeDew automatic dew heater control")]
    [ExportMetadata("Icon", "SeeDew_Icon")]
    [ExportMetadata("Category", "SeeDew")]
    public class StartDewControlInstruction : SequenceItem {

        private readonly SeeDewPlugin _plugin;

        [ImportingConstructor]
        public StartDewControlInstruction(SeeDewPlugin plugin) {
            _plugin = plugin;
            Name = "SeeDew Start Dew Control";
            if (System.Windows.Application.Current?.Resources["SeeDew_Icon"] is System.Windows.Media.GeometryGroup icon)
                Icon = icon;
        }

        private StartDewControlInstruction(StartDewControlInstruction cloneMe) : this(cloneMe._plugin) { }

        public override async Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            _plugin.RefreshRuntimeSettingsFromDisk();
            await _plugin.DewControlService.StartAsync();
        }

        public override object Clone() => new StartDewControlInstruction(this);
    }
}
