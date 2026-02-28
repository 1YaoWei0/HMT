using System.ComponentModel;
using System.Windows;
using Microsoft.VisualStudio.Shell;

namespace HMT.OptionsPane.KernelSettings
{
    public class HMTKernelSettingsOptionPage : UIElementDialogPage
    {
        private HMTKernelSettingsOptionControl _control;

        [Category("Form")]
        [DisplayName("Form Prefix")]
        [Description("Prefix for Form extension class names.")]
        public string FormPrefix { get; set; } = string.Empty;

        [Category("Form")]
        [DisplayName("Form Suffix")]
        [Description("Suffix for Form extension class names.")]
        public string FormSuffix { get; set; } = "Extension";

        [Category("Table")]
        [DisplayName("Table Prefix")]
        [Description("Prefix for Table extension class names.")]
        public string TablePrefix { get; set; } = string.Empty;

        [Category("Table")]
        [DisplayName("Table Suffix")]
        [Description("Suffix for Table extension class names.")]
        public string TableSuffix { get; set; } = "Extension";

        [Category("Class")]
        [DisplayName("Class Prefix")]
        [Description("Prefix for Class extension class names.")]
        public string ClassPrefix { get; set; } = string.Empty;

        [Category("Class")]
        [DisplayName("Class Suffix")]
        [Description("Suffix for Class extension class names.")]
        public string ClassSuffix { get; set; } = "Extension";

        [Category("Entity")]
        [DisplayName("Entity Prefix")]
        [Description("Prefix for Entity extension class names.")]
        public string EntityPrefix { get; set; } = string.Empty;

        [Category("Entity")]
        [DisplayName("Entity Suffix")]
        [Description("Suffix for Entity extension class names.")]
        public string EntitySuffix { get; set; } = "Extension";

        protected override UIElement Child => _control ?? (_control = new HMTKernelSettingsOptionControl());

        protected override void OnActivate(CancelEventArgs e)
        {
            base.OnActivate(e);
            _control?.ViewModel.LoadFrom(this);
        }

        protected override void OnApply(PageApplyEventArgs e)
        {
            if (_control != null && !_control.ViewModel.ValidateAll())
            {
                e.ApplyBehavior = ApplyKind.CancelNoNavigate;
                return;
            }

            _control?.ViewModel.SaveTo(this);
            base.OnApply(e);
        }
    }
}
