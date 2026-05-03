using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using NINA.Core.Model;
using NINA.Sequencer.SequenceItem;

namespace NINA.Plugin.DewSee.Sequencer {

    [Export(typeof(ISequenceItem))]
    [ExportMetadata("Name", "Stop Dew Control")]
    [ExportMetadata("Description", "Stops the DewSee automatic dew heater control")]
    [ExportMetadata("Icon", "DewSee_Icon")]
    [ExportMetadata("Category", "DewSee")]
    public class StopDewControlInstruction : SequenceItem {

        private readonly DewSeePlugin _plugin;

        [ImportingConstructor]
        public StopDewControlInstruction(DewSeePlugin plugin) {
            _plugin = plugin;
            Name = "Stop Dew Control";
        }

        private StopDewControlInstruction(StopDewControlInstruction cloneMe) : this(cloneMe._plugin) { }

        public override async Task Execute(IProgress<ApplicationStatus> progress, CancellationToken token) {
            await _plugin.DewControlService.StopAsync();
        }

        public override object Clone() => new StopDewControlInstruction(this);
    }
}
