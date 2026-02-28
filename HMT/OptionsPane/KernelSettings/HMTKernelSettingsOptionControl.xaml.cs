using System.Windows.Controls;

namespace HMT.OptionsPane.KernelSettings
{
    public partial class HMTKernelSettingsOptionControl : UserControl
    {
        public HMTKernelSettingsOptionControl()
        {
            InitializeComponent();
            ViewModel = new HMTKernelSettingsViewModel();
            DataContext = ViewModel;
        }

        public HMTKernelSettingsViewModel ViewModel { get; }
    }
}
