using System;
using System.Collections.Generic;
using HMT.Kernel;
using HMT.OptionsPane;
using HMT.OptionsPane.KernelSettings;
using Microsoft.VisualStudio.Shell;

namespace HMT.Services.Settings
{
    public enum ExtensionTemplateDefaultScheme
    {
        Default,
        DefaultWithUnderscore
    }

    public class HMTKernelSettingsManager
    {
        private const string MainElementTemplateName = "$MainObject$";
        private const string SubElementTemplateName = "$SubObject$";

        private AxModelSettings AxModelSettings;
        private readonly HMTKernelSettingsStorage _kernelSettingsStorage = new HMTKernelSettingsStorage();

        public AxModelSettings GetAxModelSettings()
        {
            return AxModelSettings;
        }

        public void LoadSettings(AsyncPackage package = null)
        {
            if (package != null)
            {
                AxModelSettings = BuildFromOptionPage(package);
                if (HasAnyConfiguredRule(AxModelSettings))
                {
                    return;
                }
            }

            AxModelSettings = _kernelSettingsStorage.LoadSettings(package);
            InitMissingSettings();
        }

        private static bool HasAnyConfiguredRule(AxModelSettings settings)
        {
            if (settings?.ExtensionNameTemplateList == null)
            {
                return false;
            }

            foreach (var item in settings.ExtensionNameTemplateList)
            {
                if (!string.IsNullOrWhiteSpace(item.Value.ExtensionTemplate))
                {
                    return true;
                }
            }

            return false;
        }

        private AxModelSettings BuildFromOptionPage(AsyncPackage package)
        {
            var page = (HMTKernelSettingsOptionPage)package.GetDialogPage(typeof(HMTKernelSettingsOptionPage));
            var options = HMTOptionsUtils.getPrefix(package);

            var settings = new AxModelSettings
            {
                ModelPrefix = options,
                ExtensionNameTemplateList = new Dictionary<ExtensionClassType, ExtensionNameTemplate>()
            };

            settings.ExtensionNameTemplateList[ExtensionClassType.Form] = CreateRule(ExtensionClassType.Form, page.FormPrefix, page.FormSuffix);
            settings.ExtensionNameTemplateList[ExtensionClassType.Table] = CreateRule(ExtensionClassType.Table, page.TablePrefix, page.TableSuffix);
            settings.ExtensionNameTemplateList[ExtensionClassType.Class] = CreateRule(ExtensionClassType.Class, page.ClassPrefix, page.ClassSuffix);
            settings.ExtensionNameTemplateList[ExtensionClassType.DataEntityView] = CreateRule(ExtensionClassType.DataEntityView, page.EntityPrefix, page.EntitySuffix);

            return settings;
        }

        private static ExtensionNameTemplate CreateRule(ExtensionClassType type, string prefix, string suffix)
        {
            var normalizedPrefix = Normalize(prefix);
            var normalizedSuffix = Normalize(suffix);
            return new ExtensionNameTemplate
            {
                ExtensionType = type,
                ExtensionTemplate = BuildTemplate(normalizedPrefix, normalizedSuffix),
                EventHandlerTemplate = BuildTemplate(normalizedPrefix, normalizedSuffix + "EventHandler")
            };
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
        }

        private static string BuildTemplate(string prefix, string suffix)
        {
            return $"{prefix} + {MainElementTemplateName} + {suffix}";
        }

        protected void InitMissingSettings()
        {
            if (string.IsNullOrEmpty(AxModelSettings.ModelPrefix))
            {
                AxModelSettings.ModelPrefix = "TST";
            }

            if (AxModelSettings.ExtensionNameTemplateList == null)
            {
                AxModelSettings.ExtensionNameTemplateList = new Dictionary<ExtensionClassType, ExtensionNameTemplate>();
            }

            EnsureDefaultRule(ExtensionClassType.Form);
            EnsureDefaultRule(ExtensionClassType.Table);
            EnsureDefaultRule(ExtensionClassType.Class);
            EnsureDefaultRule(ExtensionClassType.DataEntityView);
        }

        private void EnsureDefaultRule(ExtensionClassType classType)
        {
            if (AxModelSettings.ExtensionNameTemplateList.ContainsKey(classType))
            {
                return;
            }

            AxModelSettings.ExtensionNameTemplateList[classType] = new ExtensionNameTemplate
            {
                ExtensionType = classType,
                ExtensionTemplate = BuildTemplate(string.Empty, "Extension"),
                EventHandlerTemplate = BuildTemplate(string.Empty, "EventHandler")
            };
        }

        public string GetClassName(Kernel.ExtensionClassType elementType, ExtensionClassModeType classType,
            string prefixValue, string mainElementValue,
            string subElementValue)
        {
            if (AxModelSettings == null)
            {
                LoadSettings();
            }

            var mappedType = MapElementType(elementType);
            var nameTemplate = AxModelSettings.ExtensionNameTemplateList[mappedType];

            var templateString = classType == ExtensionClassModeType.EventHandler
                ? nameTemplate.EventHandlerTemplate
                : nameTemplate.ExtensionTemplate;

            var resultName = templateString;
            resultName = resultName.Replace("$Prefix$", prefixValue ?? string.Empty);
            resultName = resultName.Replace(MainElementTemplateName, mainElementValue ?? string.Empty);
            resultName = resultName.Replace(SubElementTemplateName, subElementValue ?? string.Empty);

            return AxHelper.RemoveSpecialCharacters(resultName).Replace(" ", "");
        }

        private static ExtensionClassType MapElementType(ExtensionClassType elementType)
        {
            switch (elementType)
            {
                case ExtensionClassType.Form:
                case ExtensionClassType.FormDataField:
                case ExtensionClassType.FormDataSource:
                case ExtensionClassType.FormControl:
                    return ExtensionClassType.Form;
                case ExtensionClassType.Table:
                    return ExtensionClassType.Table;
                case ExtensionClassType.DataEntityView:
                    return ExtensionClassType.DataEntityView;
                default:
                    return ExtensionClassType.Class;
            }
        }

        public string GetDescription()
        {
            return "Configure naming rules in Tools > Options > HMT > Kernel Settings.";
        }

        public void LoadDefaultSettings(ExtensionTemplateDefaultScheme extensionTemplateDefaultScheme, AsyncPackage package = null)
        {
            LoadSettings(package);
        }

        public string GetSettingsFilename()
        {
            return _kernelSettingsStorage.GetFilePath();
        }

        public void InitFormControlData(out string typeStringControl, out string templateStringControl)
        {
            typeStringControl = "Legacy WinForms settings are deprecated. Use Tools > Options > HMT > Kernel Settings.";
            templateStringControl = string.Empty;
        }

        public void LoadSettingsFromFormControlData(string templateStringControl, AsyncPackage package = null)
        {
            LoadSettings(package);
        }

        public bool SaveToFile()
        {
            return false;
        }
    }
}
