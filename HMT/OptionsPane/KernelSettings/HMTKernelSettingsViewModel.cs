using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace HMT.OptionsPane.KernelSettings
{
    public class HMTKernelSettingsViewModel : INotifyPropertyChanged, INotifyDataErrorInfo
    {
        private readonly Dictionary<string, List<string>> _errors = new Dictionary<string, List<string>>();

        private string _formPrefix = string.Empty;
        private string _formSuffix = "Extension";
        private string _tablePrefix = string.Empty;
        private string _tableSuffix = "Extension";
        private string _classPrefix = string.Empty;
        private string _classSuffix = "Extension";
        private string _entityPrefix = string.Empty;
        private string _entitySuffix = "Extension";

        public event PropertyChangedEventHandler PropertyChanged;
        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

        public string FormPrefix { get => _formPrefix; set => SetField(ref _formPrefix, value); }
        public string FormSuffix { get => _formSuffix; set => SetField(ref _formSuffix, value); }

        public string TablePrefix { get => _tablePrefix; set => SetField(ref _tablePrefix, value); }
        public string TableSuffix { get => _tableSuffix; set => SetField(ref _tableSuffix, value); }

        public string ClassPrefix { get => _classPrefix; set => SetField(ref _classPrefix, value); }
        public string ClassSuffix { get => _classSuffix; set => SetField(ref _classSuffix, value); }

        public string EntityPrefix { get => _entityPrefix; set => SetField(ref _entityPrefix, value); }
        public string EntitySuffix { get => _entitySuffix; set => SetField(ref _entitySuffix, value); }

        public bool HasErrors => _errors.Count > 0;

        public IEnumerable GetErrors(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName) || !_errors.ContainsKey(propertyName))
            {
                return null;
            }

            return _errors[propertyName];
        }

        public void LoadFrom(HMTKernelSettingsOptionPage page)
        {
            FormPrefix = page.FormPrefix;
            FormSuffix = page.FormSuffix;
            TablePrefix = page.TablePrefix;
            TableSuffix = page.TableSuffix;
            ClassPrefix = page.ClassPrefix;
            ClassSuffix = page.ClassSuffix;
            EntityPrefix = page.EntityPrefix;
            EntitySuffix = page.EntitySuffix;

            ValidateAll();
        }

        public void SaveTo(HMTKernelSettingsOptionPage page)
        {
            page.FormPrefix = FormPrefix?.Trim() ?? string.Empty;
            page.FormSuffix = FormSuffix?.Trim() ?? string.Empty;
            page.TablePrefix = TablePrefix?.Trim() ?? string.Empty;
            page.TableSuffix = TableSuffix?.Trim() ?? string.Empty;
            page.ClassPrefix = ClassPrefix?.Trim() ?? string.Empty;
            page.ClassSuffix = ClassSuffix?.Trim() ?? string.Empty;
            page.EntityPrefix = EntityPrefix?.Trim() ?? string.Empty;
            page.EntitySuffix = EntitySuffix?.Trim() ?? string.Empty;
        }

        public bool ValidateAll()
        {
            ValidateRequiredSuffix(nameof(FormSuffix), FormSuffix, "Form");
            ValidateRequiredSuffix(nameof(TableSuffix), TableSuffix, "Table");
            ValidateRequiredSuffix(nameof(ClassSuffix), ClassSuffix, "Class");
            ValidateRequiredSuffix(nameof(EntitySuffix), EntitySuffix, "Entity");
            return !HasErrors;
        }

        private void ValidateRequiredSuffix(string propertyName, string value, string objectType)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                SetErrors(propertyName, new List<string> { $"{objectType} suffix is required." });
            }
            else
            {
                ClearErrors(propertyName);
            }
        }

        private void SetErrors(string propertyName, List<string> errors)
        {
            _errors[propertyName] = errors;
            ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
            OnPropertyChanged(nameof(HasErrors));
        }

        private void ClearErrors(string propertyName)
        {
            if (_errors.Remove(propertyName))
            {
                ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
                OnPropertyChanged(nameof(HasErrors));
            }
        }

        private void SetField(ref string field, string value, [CallerMemberName] string propertyName = null)
        {
            if (field == value)
            {
                return;
            }

            field = value;
            OnPropertyChanged(propertyName);

            switch (propertyName)
            {
                case nameof(FormSuffix):
                    ValidateRequiredSuffix(propertyName, value, "Form");
                    break;
                case nameof(TableSuffix):
                    ValidateRequiredSuffix(propertyName, value, "Table");
                    break;
                case nameof(ClassSuffix):
                    ValidateRequiredSuffix(propertyName, value, "Class");
                    break;
                case nameof(EntitySuffix):
                    ValidateRequiredSuffix(propertyName, value, "Entity");
                    break;
            }
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
