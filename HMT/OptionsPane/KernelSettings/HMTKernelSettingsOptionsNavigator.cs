using Microsoft.VisualStudio.Shell;

namespace HMT.OptionsPane.KernelSettings
{
    public static class HMTKernelSettingsOptionsNavigator
    {
        public static void Open(AsyncPackage package)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            package.ShowOptionPage(typeof(HMTKernelSettingsOptionPage));
        }
    }
}
