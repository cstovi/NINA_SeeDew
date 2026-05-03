using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using NINA.Core.Model;
using NINA.Sequencer.SequenceItem;

namespace NINA.Plugin.DewSee.Sequencer {

    [Export(typeof(ISequenceItem))]
    [ExportMetadata("Name", "Start Dew Control")]
    [ExportMetadata("Description", "Starts the DewSee automatic dew heater control")]
    [ExportMetadata("Icon", "DewSee_Icon")]
    [ExportMetadata("Category", "DewSee")]
    public class StartDewControlInstruction : SequenceItem {

        private readonly DewSeePlugin _plugin;

        [ImportingConstructor]
        public StartDewControlInstruction(DewSeePlugin plugin) {
            _plugin = plugin;
            Name = "Start Dew Control";
        }

        private StartDewControlInstruction(StartDewControlInstruction cloneMe) : this(cloneMe._plugin) { }

        public override async Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            await _plugin.DewControlService.StartAsync();
        }

        public override object Clone() => new StartDewControlInstruction(this);
    }
}
