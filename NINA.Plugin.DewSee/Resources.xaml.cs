using System.ComponentModel.Composition;
using System.Windows;

namespace NINA.Plugin.DewSee {
    [Export(typeof(ResourceDictionary))]
    public partial class Resources : ResourceDictionary {
        public Resources() {
            InitializeComponent();
        }
    }
}
