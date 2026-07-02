using HMT.Kernel;
using HMT.Models.AiSchemas;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HMT.Services.AiIntegration
{
    /// <summary>
    /// Validates AI-generated metadata specs against the current D365 model store
    /// before execution, catching errors that LLM + grep would miss.
    /// </summary>
    public class MetadataValidator
    {
        private readonly AxHelper _axHelper;

        public MetadataValidator(AxHelper axHelper)
        {
            _axHelper = axHelper;
        }

        public ValidationResult ValidateSpec(AiMetadataSpec spec)
        {
            var result = new ValidationResult();

            if (spec == null)
            {
                result.AddError("Spec is null");
                return result;
            }

            if (spec.Objects == null || spec.Objects.Count == 0)
            {
                result.AddError("No objects defined in spec");
                return result;
            }

            foreach (var obj in spec.Objects)
            {
                ValidateObject(obj, result);
            }

            return result;
        }

        private void ValidateObject(MetadataObjectSpec obj, ValidationResult result)
        {
            switch (obj.ObjectType)
            {
                case MetadataObjectType.Table:
                    if (obj.Table == null) result.AddError("ObjectType is Table but table spec is null");
                    else ValidateTable(obj.Table, result);
                    break;
                case MetadataObjectType.Form:
                    if (obj.Form == null) result.AddError("ObjectType is Form but form spec is null");
                    else ValidateForm(obj.Form, result);
                    break;
                case MetadataObjectType.SecurityPrivilege:
                    if (obj.SecurityPrivilege == null) result.AddError("ObjectType is SecurityPrivilege but spec is null");
                    else ValidateSecurityPrivilege(obj.SecurityPrivilege, result);
                    break;
                case MetadataObjectType.MenuItem:
                    if (obj.MenuItem == null) result.AddError("ObjectType is MenuItem but spec is null");
                    else ValidateMenuItem(obj.MenuItem, result);
                    break;
                case MetadataObjectType.Edt:
                    if (obj.Edt == null) result.AddError("ObjectType is Edt but spec is null");
                    else ValidateEdt(obj.Edt, result);
                    break;
                case MetadataObjectType.Enum:
                    if (obj.Enum == null) result.AddError("ObjectType is Enum but spec is null");
                    else ValidateEnum(obj.Enum, result);
                    break;
                default:
                    result.AddError($"Unknown object type: {obj.ObjectType}");
                    break;
            }
        }

        private void ValidateTable(TableSpec table, ValidationResult result)
        {
            if (string.IsNullOrWhiteSpace(table.Name))
            {
                result.AddError("Table name is required");
                return;
            }

            if (_axHelper.MetadataProvider.Tables.Read(table.Name) != null)
            {
                result.AddWarning($"Table '{table.Name}' already exists and will be skipped");
            }

            foreach (var field in table.Fields)
            {
                if (string.IsNullOrWhiteSpace(field.Name))
                {
                    result.AddError($"Table '{table.Name}': field name is required");
                }

                if (!string.IsNullOrWhiteSpace(field.Edt) && _axHelper.MetadataProvider.Edts.Read(field.Edt) == null)
                {
                    result.AddWarning($"Table '{table.Name}', field '{field.Name}': EDT '{field.Edt}' does not exist in model store (may be created in same spec)");
                }

                if (field.Type == TableFieldType.Enum && !string.IsNullOrWhiteSpace(field.EnumType)
                    && _axHelper.MetadataProvider.Enums.Read(field.EnumType) == null)
                {
                    result.AddWarning($"Table '{table.Name}', field '{field.Name}': Enum '{field.EnumType}' does not exist in model store");
                }
            }

            foreach (var index in table.Indexes)
            {
                if (string.IsNullOrWhiteSpace(index.Name))
                {
                    result.AddError($"Table '{table.Name}': index name is required");
                }

                foreach (var fieldName in index.Fields)
                {
                    if (!table.Fields.Any(f => f.Name == fieldName))
                    {
                        result.AddError($"Table '{table.Name}', index '{index.Name}': field '{fieldName}' is not defined in the table");
                    }
                }
            }

            foreach (var relation in table.Relations)
            {
                if (!string.IsNullOrWhiteSpace(relation.RelatedTable)
                    && _axHelper.MetadataProvider.Tables.Read(relation.RelatedTable) == null)
                {
                    result.AddWarning($"Table '{table.Name}', relation '{relation.Name}': related table '{relation.RelatedTable}' does not exist");
                }
            }
        }

        private void ValidateForm(FormSpec form, ValidationResult result)
        {
            if (string.IsNullOrWhiteSpace(form.Name))
            {
                result.AddError("Form name is required");
                return;
            }

            if (_axHelper.MetadataProvider.Forms.Read(form.Name) != null)
            {
                result.AddError($"Form '{form.Name}' already exists");
            }

            if (string.IsNullOrWhiteSpace(form.DataSource))
            {
                result.AddError($"Form '{form.Name}': dataSource (table name) is required");
            }
            else if (_axHelper.MetadataProvider.Tables.Read(form.DataSource) == null)
            {
                result.AddWarning($"Form '{form.Name}': data source table '{form.DataSource}' does not exist (may be created in same spec)");
            }
        }

        private void ValidateSecurityPrivilege(SecurityPrivilegeSpec privilege, ValidationResult result)
        {
            if (string.IsNullOrWhiteSpace(privilege.Name))
            {
                result.AddError("Security privilege name is required");
                return;
            }

            if (_axHelper.MetadataProvider.SecurityPrivileges.Read(privilege.Name) != null)
            {
                result.AddError($"Security privilege '{privilege.Name}' already exists");
            }

            foreach (var ep in privilege.EntryPoints)
            {
                if (string.IsNullOrWhiteSpace(ep.ObjectName))
                {
                    result.AddError($"Privilege '{privilege.Name}': entry point object name is required");
                }
            }
        }

        private void ValidateMenuItem(MenuItemSpec menuItem, ValidationResult result)
        {
            if (string.IsNullOrWhiteSpace(menuItem.Name))
            {
                result.AddError("MenuItem name is required");
                return;
            }

            switch (menuItem.Type)
            {
                case MenuItemTypeSpec.Display:
                    if (_axHelper.MetadataProvider.MenuItemDisplays.Read(menuItem.Name) != null)
                        result.AddError($"MenuItemDisplay '{menuItem.Name}' already exists");
                    break;
                case MenuItemTypeSpec.Action:
                    if (_axHelper.MetadataProvider.MenuItemActions.Read(menuItem.Name) != null)
                        result.AddError($"MenuItemAction '{menuItem.Name}' already exists");
                    break;
                case MenuItemTypeSpec.Output:
                    if (_axHelper.MetadataProvider.MenuItemOutputs.Read(menuItem.Name) != null)
                        result.AddError($"MenuItemOutput '{menuItem.Name}' already exists");
                    break;
            }
        }

        private void ValidateEdt(EdtSpec edt, ValidationResult result)
        {
            if (string.IsNullOrWhiteSpace(edt.Name))
            {
                result.AddError("EDT name is required");
                return;
            }

            if (_axHelper.MetadataProvider.Edts.Read(edt.Name) != null)
            {
                result.AddWarning($"EDT '{edt.Name}' already exists and will be updated");
            }

            if (!string.IsNullOrWhiteSpace(edt.Extends) && _axHelper.MetadataProvider.Edts.Read(edt.Extends) == null)
            {
                result.AddWarning($"EDT '{edt.Name}': extends EDT '{edt.Extends}' does not exist");
            }

            if (edt.Type == EdtTypeSpec.Enum && !string.IsNullOrWhiteSpace(edt.EnumType)
                && _axHelper.MetadataProvider.Enums.Read(edt.EnumType) == null)
            {
                result.AddWarning($"EDT '{edt.Name}': enum type '{edt.EnumType}' does not exist");
            }
        }

        private void ValidateEnum(EnumSpec enumSpec, ValidationResult result)
        {
            if (string.IsNullOrWhiteSpace(enumSpec.Name))
            {
                result.AddError("Enum name is required");
                return;
            }

            if (enumSpec.Values == null || enumSpec.Values.Count == 0)
            {
                result.AddWarning($"Enum '{enumSpec.Name}' has no values defined");
            }

            // Check for duplicate value names
            var duplicateNames = enumSpec.Values?
                .GroupBy(v => v.Name)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicateNames != null && duplicateNames.Count > 0)
            {
                result.AddError($"Enum '{enumSpec.Name}': duplicate value names: {string.Join(", ", duplicateNames)}");
            }
        }
    }

    public class ValidationResult
    {
        public List<ValidationMessage> Messages { get; set; } = new List<ValidationMessage>();

        public bool HasErrors => Messages.Any(m => m.Severity == ValidationSeverity.Error);
        public bool HasWarnings => Messages.Any(m => m.Severity == ValidationSeverity.Warning);

        public void AddError(string message)
        {
            Messages.Add(new ValidationMessage { Severity = ValidationSeverity.Error, Message = message });
        }

        public void AddWarning(string message)
        {
            Messages.Add(new ValidationMessage { Severity = ValidationSeverity.Warning, Message = message });
        }

        public void AddInfo(string message)
        {
            Messages.Add(new ValidationMessage { Severity = ValidationSeverity.Info, Message = message });
        }

        public string GetSummary()
        {
            var sb = new StringBuilder();
            foreach (var msg in Messages)
            {
                sb.AppendLine($"[{msg.Severity}] {msg.Message}");
            }
            return sb.ToString();
        }
    }

    public class ValidationMessage
    {
        public ValidationSeverity Severity { get; set; }
        public string Message { get; set; }
    }

    public enum ValidationSeverity
    {
        Info,
        Warning,
        Error
    }
}
