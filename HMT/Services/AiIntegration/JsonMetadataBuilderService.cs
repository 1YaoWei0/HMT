using HMT.Kernel;
using HMT.Models.AiSchemas;
using Microsoft.Dynamics.AX.Metadata.Core.MetaModel;
using Microsoft.Dynamics.AX.Metadata.MetaModel;
using Microsoft.VisualStudio.Shell;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace HMT.Services.AiIntegration
{
    /// <summary>
    /// Core service that converts AI-generated JSON specs into D365 metadata objects
    /// using the VSIX SDK APIs (AxTable, AxForm, etc.).
    /// 
    /// This bridges the gap between AI (fast but imprecise) and SDK (type-safe and correct).
    /// </summary>
    public class JsonMetadataBuilderService
    {
        private readonly AxHelper _axHelper;
        private readonly StringBuilder _logBuilder = new StringBuilder();

        public JsonMetadataBuilderService()
        {
            _axHelper = new AxHelper();
        }

        public JsonMetadataBuilderService(AxHelper axHelper)
        {
            _axHelper = axHelper;
        }

        /// <summary>
        /// Load and parse an AI-generated JSON spec file.
        /// </summary>
        public AiMetadataSpec LoadSpec(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Spec file not found: {filePath}");
            }

            string json = File.ReadAllText(filePath, Encoding.UTF8);
            return JsonConvert.DeserializeObject<AiMetadataSpec>(json);
        }

        /// <summary>
        /// Parse JSON string directly into spec.
        /// </summary>
        public AiMetadataSpec ParseSpec(string json)
        {
            return JsonConvert.DeserializeObject<AiMetadataSpec>(json);
        }

        /// <summary>
        /// Execute all objects in the spec, creating them via the D365 SDK.
        /// Returns a build result with logs and any errors.
        /// </summary>
        public BuildResult ExecuteSpec(AiMetadataSpec spec)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var result = new BuildResult();
            _logBuilder.Clear();

            // Validate first
            var validator = new MetadataValidator(_axHelper);
            var validationResult = validator.ValidateSpec(spec);

            if (validationResult.HasErrors)
            {
                result.Success = false;
                result.ValidationResult = validationResult;
                result.Log = validationResult.GetSummary();
                return result;
            }

            result.ValidationResult = validationResult;

            // Execute objects in dependency order: Enum → EDT → Table → Form → MenuItem → SecurityPrivilege
            var orderedObjects = OrderByDependency(spec.Objects);

            foreach (var obj in orderedObjects)
            {
                try
                {
                    ExecuteObject(obj);
                    result.CreatedObjects.Add($"{obj.ObjectType}: {GetObjectName(obj)}");
                }
                catch (Exception ex)
                {
                    result.Errors.Add($"Failed to create {obj.ObjectType} '{GetObjectName(obj)}': {ex.Message}");
                }
            }

            result.Success = result.Errors.Count == 0;
            result.Log = _logBuilder.ToString();
            return result;
        }

        private List<MetadataObjectSpec> OrderByDependency(List<MetadataObjectSpec> objects)
        {
            // Dependency order: Enum → EDT → Table → Form → MenuItem → SecurityPrivilege
            var priorityMap = new Dictionary<MetadataObjectType, int>
            {
                { MetadataObjectType.Enum, 0 },
                { MetadataObjectType.Edt, 1 },
                { MetadataObjectType.Table, 2 },
                { MetadataObjectType.Form, 3 },
                { MetadataObjectType.MenuItem, 4 },
                { MetadataObjectType.SecurityPrivilege, 5 }
            };

            return objects.OrderBy(o => priorityMap.ContainsKey(o.ObjectType) ? priorityMap[o.ObjectType] : 99).ToList();
        }

        private string GetObjectName(MetadataObjectSpec obj)
        {
            switch (obj.ObjectType)
            {
                case MetadataObjectType.Table: return obj.Table?.Name ?? "(unnamed)";
                case MetadataObjectType.Form: return obj.Form?.Name ?? "(unnamed)";
                case MetadataObjectType.SecurityPrivilege: return obj.SecurityPrivilege?.Name ?? "(unnamed)";
                case MetadataObjectType.MenuItem: return obj.MenuItem?.Name ?? "(unnamed)";
                case MetadataObjectType.Edt: return obj.Edt?.Name ?? "(unnamed)";
                case MetadataObjectType.Enum: return obj.Enum?.Name ?? "(unnamed)";
                default: return "(unknown)";
            }
        }

        private void ExecuteObject(MetadataObjectSpec obj)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            switch (obj.ObjectType)
            {
                case MetadataObjectType.Table:
                    CreateTable(obj.Table);
                    break;
                case MetadataObjectType.Form:
                    CreateForm(obj.Form);
                    break;
                case MetadataObjectType.SecurityPrivilege:
                    CreateSecurityPrivilege(obj.SecurityPrivilege);
                    break;
                case MetadataObjectType.MenuItem:
                    CreateMenuItem(obj.MenuItem);
                    break;
                case MetadataObjectType.Edt:
                    CreateEdt(obj.Edt);
                    break;
                case MetadataObjectType.Enum:
                    CreateEnum(obj.Enum);
                    break;
            }
        }

        #region Table Creation

        private void CreateTable(TableSpec spec)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (_axHelper.MetadataProvider.Tables.Read(spec.Name) != null)
            {
                AddLog($"Table '{spec.Name}' already exists, skipping");
                return;
            }

            var newTable = new AxTable
            {
                Name = spec.Name,
                Label = spec.Label,
                CreatedBy = spec.CreatedBy ? NoYes.Yes : NoYes.No,
                CreatedDateTime = spec.CreatedDateTime ? NoYes.Yes : NoYes.No,
                ModifiedBy = spec.ModifiedBy ? NoYes.Yes : NoYes.No,
                ModifiedDateTime = spec.ModifiedDateTime ? NoYes.Yes : NoYes.No
            };

            // TableGroup
            if (System.Enum.TryParse(spec.TableGroup, out TableGroup tableGroup))
            {
                newTable.TableGroup = tableGroup;
            }

            // CacheLevel
            if (System.Enum.TryParse(spec.CacheLevel, out RecordCacheLevel cacheLevel))
            {
                newTable.CacheLookup = cacheLevel;
            }

            if (!string.IsNullOrWhiteSpace(spec.TitleField1))
            {
                newTable.TitleField1 = spec.TitleField1;
            }
            if (!string.IsNullOrWhiteSpace(spec.TitleField2))
            {
                newTable.TitleField2 = spec.TitleField2;
            }

            // Fields
            foreach (var fieldSpec in spec.Fields)
            {
                AxTableField field = CreateTableField(fieldSpec);
                newTable.AddField(field);
            }

            // Indexes
            foreach (var indexSpec in spec.Indexes)
            {
                var index = new AxTableIndex
                {
                    Name = indexSpec.Name,
                    AlternateKey = indexSpec.AlternateKey ? NoYes.Yes : NoYes.No,
                    AllowDuplicates = indexSpec.AllowDuplicates ? NoYes.Yes : NoYes.No,
                };

                foreach (var fieldName in indexSpec.Fields)
                {
                    index.AddField(new AxTableIndexField { Name = fieldName, DataField = fieldName });
                }

                newTable.AddIndex(index);
            }

            // Set clustered/primary index to the first alternate key index
            var primaryIndex = spec.Indexes.FirstOrDefault(i => i.AlternateKey);
            if (primaryIndex != null)
            {
                newTable.ClusteredIndex = primaryIndex.Name;
                newTable.PrimaryIndex = primaryIndex.Name;
                newTable.ReplacementKey = primaryIndex.Name;
            }

            // Field Groups
            foreach (var groupSpec in spec.FieldGroups)
            {
                var group = new AxTableFieldGroup
                {
                    Name = groupSpec.Name,
                    Label = groupSpec.Label,
                    IsSystemGenerated = groupSpec.IsSystemGenerated ? NoYes.Yes : NoYes.No,
                    AutoPopulate = groupSpec.AutoPopulate ? NoYes.Yes : NoYes.No
                };

                foreach (var fieldName in groupSpec.Fields)
                {
                    group.AddField(new AxTableFieldGroupField { Name = fieldName, DataField = fieldName });
                }

                newTable.AddFieldGroup(group);
            }

            // Add default system field groups if not specified
            EnsureSystemFieldGroups(newTable, spec);

            // Source code declaration
            var sb = new StringBuilder();
            sb.AppendLine($"public class {newTable.Name} extends common");
            sb.AppendLine("{");
            sb.AppendLine("}");
            newTable.SourceCode.Declaration = sb.ToString();

            // Relations
            foreach (var relationSpec in spec.Relations)
            {
                var relation = new AxTableRelation
                {
                    Name = relationSpec.Name,
                    RelatedTable = relationSpec.RelatedTable
                };

                if (System.Enum.TryParse(relationSpec.Cardinality, out Cardinality cardinality))
                {
                    relation.Cardinality = cardinality;
                }
                if (System.Enum.TryParse(relationSpec.RelatedTableCardinality, out RelatedTableCardinality relCardinality))
                {
                    relation.RelatedTableCardinality = relCardinality;
                }

                foreach (var constraint in relationSpec.Constraints)
                {
                    relation.AddRelationConstraint(new AxTableRelationConstraintField
                    {
                        Name = constraint.Field,
                        Field = constraint.Field,
                        RelatedField = constraint.RelatedField
                    });
                }

                newTable.AddRelation(relation);
            }

            _axHelper.MetaModelService.CreateTable(newTable, _axHelper.ModelSaveInfo);
            _axHelper.AppendToActiveProject(newTable);

            AddLog($"Created Table: {newTable.Name}");
        }

        private AxTableField CreateTableField(TableFieldSpec fieldSpec)
        {
            AxTableField field;

            switch (fieldSpec.Type)
            {
                case TableFieldType.String:
                    var strField = new AxTableFieldString();
                    if (fieldSpec.StringSize > 0)
                    {
                        strField.StringSize = fieldSpec.StringSize;
                    }
                    field = strField;
                    break;
                case TableFieldType.Int:
                    field = new AxTableFieldInt();
                    break;
                case TableFieldType.Int64:
                    field = new AxTableFieldInt64();
                    break;
                case TableFieldType.Real:
                    field = new AxTableFieldReal();
                    break;
                case TableFieldType.Date:
                    field = new AxTableFieldDate();
                    break;
                case TableFieldType.DateTime:
                    field = new AxTableFieldUtcDateTime();
                    break;
                case TableFieldType.Enum:
                    var enumField = new AxTableFieldEnum();
                    if (!string.IsNullOrWhiteSpace(fieldSpec.EnumType))
                    {
                        enumField.EnumType = fieldSpec.EnumType;
                    }
                    field = enumField;
                    break;
                case TableFieldType.Guid:
                    field = new AxTableFieldGuid();
                    break;
                case TableFieldType.Container:
                    field = new AxTableFieldContainer();
                    break;
                default:
                    field = new AxTableFieldString();
                    break;
            }

            field.Name = fieldSpec.Name;
            if (!string.IsNullOrWhiteSpace(fieldSpec.Edt))
            {
                field.ExtendedDataType = fieldSpec.Edt;
            }
            if (!string.IsNullOrWhiteSpace(fieldSpec.Label))
            {
                field.Label = fieldSpec.Label;
            }
            if (!string.IsNullOrWhiteSpace(fieldSpec.HelpText))
            {
                field.HelpText = fieldSpec.HelpText;
            }
            field.Mandatory = fieldSpec.Mandatory ? NoYes.Yes : NoYes.No;
            field.AllowEdit = fieldSpec.AllowEdit ? NoYes.Yes : NoYes.No;
            field.AllowEditOnCreate = fieldSpec.AllowEditOnCreate ? NoYes.Yes : NoYes.No;
            field.IgnoreEDTRelation = fieldSpec.IgnoreEdtRelation ? NoYes.Yes : NoYes.No;

            return field;
        }

        private void EnsureSystemFieldGroups(AxTable table, TableSpec spec)
        {
            var existingGroups = spec.FieldGroups.Select(g => g.Name).ToHashSet();
            string[] systemGroups = { "AutoReport", "AutoLookup", "AutoIdentification", "AutoSummary", "AutoBrowse" };

            foreach (var groupName in systemGroups)
            {
                if (!existingGroups.Contains(groupName))
                {
                    var group = new AxTableFieldGroup
                    {
                        Name = groupName,
                        IsSystemGenerated = NoYes.Yes
                    };

                    if (groupName == "AutoIdentification")
                    {
                        group.AutoPopulate = NoYes.Yes;
                    }

                    table.AddFieldGroup(group);
                }
            }
        }

        #endregion

        #region Form Creation

        private void CreateForm(FormSpec spec)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (_axHelper.MetadataProvider.Forms.Read(spec.Name) != null)
            {
                AddLog($"Form '{spec.Name}' already exists, skipping");
                return;
            }

            var newForm = new AxForm { Name = spec.Name };

            // Class declaration
            var classDecl = new AxMethod
            {
                Name = "classDeclaration",
                Source = $"[Form]{Environment.NewLine}public class {spec.Name} extends FormRun {Environment.NewLine}{{{Environment.NewLine}}}"
            };
            newForm.AddMethod(classDecl);

            // Data source
            string dsName = spec.DataSource;
            var dataSource = new AxFormDataSourceRoot
            {
                Name = dsName,
                Table = dsName,
                InsertIfEmpty = NoYes.No
            };
            newForm.AddDataSource(dataSource);

            // Design
            newForm.Design.Caption = spec.Label;
            newForm.Design.TitleDataSource = dsName;
            newForm.Design.DataSource = dsName;

            switch (spec.Pattern)
            {
                case FormPatternType.SimpleList:
                    BuildSimpleListForm(newForm, spec, dsName);
                    break;
                case FormPatternType.SimpleListDetails:
                    BuildSimpleListDetailsForm(newForm, spec, dsName);
                    break;
            }

            _axHelper.MetaModelService.CreateForm(newForm, _axHelper.ModelSaveInfo);
            _axHelper.AppendToActiveProject(newForm);

            AddLog($"Created Form: {newForm.Name}");

            // Optionally create menu item
            if (spec.CreateMenuItem)
            {
                CreateMenuItemForForm(spec);
            }
        }

        private void BuildSimpleListForm(AxForm form, FormSpec spec, string dsName)
        {
            form.Design.Pattern = "SimpleList";
            form.Design.PatternVersion = "1.1";

            form.Design.AddControl(new AxFormActionPaneControl { Name = "MainActionPane" });

            // Filter group with QuickFilter
            var filterGrp = new AxFormGroupControl
            {
                Name = "FilterGroup",
                Pattern = "CustomAndQuickFilters",
                PatternVersion = "1.1"
            };

            var quickFilterExt = new AxFormControlExtension { Name = "QuickFilterControl" };
            quickFilterExt.ExtensionProperties.Add(new AxFormControlExtensionProperty
            {
                Name = "targetControlName",
                Type = CompilerBaseType.String,
                Value = "MainGrid"
            });
            filterGrp.AddControl(new AxFormControl { Name = "QuickFilter", FormControlExtension = quickFilterExt });
            form.Design.AddControl(filterGrp);

            // Grid
            var grid = new AxFormGridControl { Name = "MainGrid", DataSource = dsName };
            foreach (var fieldName in spec.GridFields)
            {
                grid.AddControl(new AxFormStringControl { Name = fieldName, DataSource = dsName, DataField = fieldName });
            }
            form.Design.AddControl(grid);
        }

        private void BuildSimpleListDetailsForm(AxForm form, FormSpec spec, string dsName)
        {
            form.Design.Pattern = "SimpleListDetails";
            form.Design.PatternVersion = "1.1";

            form.Design.AddControl(new AxFormActionPaneControl { Name = "MainActionPane" });

            // Navigation list group
            var navGroup = new AxFormGroupControl { Name = "NavigationListGroup" };
            var quickFilterExt = new AxFormControlExtension { Name = "QuickFilterControl" };
            quickFilterExt.ExtensionProperties.Add(new AxFormControlExtensionProperty
            {
                Name = "targetControlName",
                Type = CompilerBaseType.String,
                Value = "MainGrid"
            });
            navGroup.AddControl(new AxFormControl { Name = "NavListQuickFilter", FormControlExtension = quickFilterExt });

            var grid = new AxFormGridControl { Name = "MainGrid", DataSource = dsName };
            foreach (var fieldName in spec.GridFields)
            {
                grid.AddControl(new AxFormStringControl { Name = fieldName, DataSource = dsName, DataField = fieldName });
            }
            navGroup.AddControl(grid);
            form.Design.AddControl(navGroup);

            // Details header
            if (spec.DetailsHeaderFields.Count > 0)
            {
                var headerGroup = new AxFormGroupControl { Name = "DetailsHeaderGroup", DataSource = dsName };
                foreach (var fieldName in spec.DetailsHeaderFields)
                {
                    headerGroup.AddControl(new AxFormStringControl { Name = $"Header_{fieldName}", DataSource = dsName, DataField = fieldName });
                }
                form.Design.AddControl(headerGroup);
            }

            // Tab pages
            if (spec.TabPages.Count > 0)
            {
                var tabControl = new AxFormTabControl { Name = "DetailsTab" };
                foreach (var tabSpec in spec.TabPages)
                {
                    var tabPage = new AxFormTabPageControl { Name = tabSpec.Name, Caption = tabSpec.Caption, DataSource = dsName };
                    foreach (var fieldName in tabSpec.Fields)
                    {
                        tabPage.AddControl(new AxFormStringControl { Name = $"{tabSpec.Name}_{fieldName}", DataSource = dsName, DataField = fieldName });
                    }
                    tabControl.AddControl(tabPage);
                }
                form.Design.AddControl(tabControl);
            }
        }

        private void CreateMenuItemForForm(FormSpec spec)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (_axHelper.MetadataProvider.MenuItemDisplays.Read(spec.Name) != null)
            {
                return;
            }

            var menuItem = new AxMenuItemDisplay
            {
                Name = spec.Name,
                Object = spec.Name,
                Label = spec.Label,
                HelpText = spec.HelpText
            };

            _axHelper.MetaModelService.CreateMenuItemDisplay(menuItem, _axHelper.ModelSaveInfo);
            _axHelper.AppendToActiveProject(menuItem);

            AddLog($"Created MenuItemDisplay: {menuItem.Name}");
        }

        #endregion

        #region Security Privilege Creation

        private void CreateSecurityPrivilege(SecurityPrivilegeSpec spec)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (_axHelper.MetadataProvider.SecurityPrivileges.Read(spec.Name) != null)
            {
                AddLog($"Security privilege '{spec.Name}' already exists, skipping");
                return;
            }

            var privilege = new AxSecurityPrivilege
            {
                Name = spec.Name,
                Label = spec.Label
            };

            var accessGrant = GetAccessGrant(spec.AccessLevel);

            foreach (var epSpec in spec.EntryPoints)
            {
                var entryPoint = new AxSecurityEntryPointReference
                {
                    Name = epSpec.Name,
                    ObjectName = epSpec.ObjectName,
                    Grant = accessGrant,
                    ObjectType = MapEntryPointType(epSpec.ObjectType)
                };

                foreach (var formName in epSpec.Forms)
                {
                    entryPoint.Forms.Add(new AxSecurityEntryPointReferenceForm { Name = formName });
                }

                privilege.EntryPoints.Add(entryPoint);
            }

            foreach (var depSpec in spec.DataEntityPermissions)
            {
                var dep = new AxSecurityDataEntityPermission
                {
                    Name = depSpec.Name,
                    Grant = accessGrant,
                    IntegrationMode = IntegrationMode.All
                };

                privilege.DataEntityPermissions.Add(dep);
            }

            _axHelper.MetaModelService.CreateSecurityPrivilege(privilege, _axHelper.ModelSaveInfo);
            _axHelper.AppendToActiveProject(privilege);

            AddLog($"Created SecurityPrivilege: {privilege.Name}");
        }

        private AccessGrant GetAccessGrant(PrivilegeAccessLevelSpec level)
        {
            switch (level)
            {
                case PrivilegeAccessLevelSpec.Read: return AccessGrant.ConstructGrantRead();
                case PrivilegeAccessLevelSpec.Update: return AccessGrant.ConstructGrantUpdate();
                case PrivilegeAccessLevelSpec.Create: return AccessGrant.ConstructGrantCreate();
                case PrivilegeAccessLevelSpec.Correct: return AccessGrant.ConstructGrantCorrect();
                case PrivilegeAccessLevelSpec.Delete: return AccessGrant.ConstructGrantDelete();
                default: return AccessGrant.ConstructGrantRead();
            }
        }

        private EntryPointType MapEntryPointType(EntryPointObjectTypeSpec type)
        {
            switch (type)
            {
                case EntryPointObjectTypeSpec.MenuItemDisplay: return EntryPointType.MenuItemDisplay;
                case EntryPointObjectTypeSpec.MenuItemAction: return EntryPointType.MenuItemAction;
                case EntryPointObjectTypeSpec.MenuItemOutput: return EntryPointType.MenuItemOutput;
                default: return EntryPointType.MenuItemDisplay;
            }
        }

        #endregion

        #region MenuItem Creation

        private void CreateMenuItem(MenuItemSpec spec)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            switch (spec.Type)
            {
                case MenuItemTypeSpec.Display:
                    if (_axHelper.MetadataProvider.MenuItemDisplays.Read(spec.Name) != null)
                    {
                        AddLog($"MenuItemDisplay '{spec.Name}' already exists, skipping");
                        return;
                    }
                    var displayItem = new AxMenuItemDisplay
                    {
                        Name = spec.Name,
                        Object = spec.ObjectName,
                        Label = spec.Label,
                        HelpText = spec.HelpText
                    };
                    _axHelper.MetaModelService.CreateMenuItemDisplay(displayItem, _axHelper.ModelSaveInfo);
                    _axHelper.AppendToActiveProject(displayItem);
                    AddLog($"Created MenuItemDisplay: {spec.Name}");
                    break;

                case MenuItemTypeSpec.Action:
                    if (_axHelper.MetadataProvider.MenuItemActions.Read(spec.Name) != null)
                    {
                        AddLog($"MenuItemAction '{spec.Name}' already exists, skipping");
                        return;
                    }
                    var actionItem = new AxMenuItemAction
                    {
                        Name = spec.Name,
                        Object = spec.ObjectName,
                        Label = spec.Label,
                        HelpText = spec.HelpText
                    };
                    _axHelper.MetaModelService.CreateMenuItemAction(actionItem, _axHelper.ModelSaveInfo);
                    _axHelper.AppendToActiveProject(actionItem);
                    AddLog($"Created MenuItemAction: {spec.Name}");
                    break;

                case MenuItemTypeSpec.Output:
                    if (_axHelper.MetadataProvider.MenuItemOutputs.Read(spec.Name) != null)
                    {
                        AddLog($"MenuItemOutput '{spec.Name}' already exists, skipping");
                        return;
                    }
                    var outputItem = new AxMenuItemOutput
                    {
                        Name = spec.Name,
                        Object = spec.ObjectName,
                        Label = spec.Label,
                        HelpText = spec.HelpText
                    };
                    _axHelper.MetaModelService.CreateMenuItemOutput(outputItem, _axHelper.ModelSaveInfo);
                    _axHelper.AppendToActiveProject(outputItem);
                    AddLog($"Created MenuItemOutput: {spec.Name}");
                    break;
            }
        }

        #endregion

        #region EDT Creation

        private void CreateEdt(EdtSpec spec)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var existing = _axHelper.MetadataProvider.Edts.Read(spec.Name);
            if (existing != null)
            {
                // Update existing EDT
                UpdateExistingEdt(spec, existing);
                return;
            }

            AxEdt newEdt;

            switch (spec.Type)
            {
                case EdtTypeSpec.String:
                    var strEdt = new AxEdtString { Name = spec.Name };
                    if (!string.IsNullOrWhiteSpace(spec.Extends))
                    {
                        strEdt.Extends = spec.Extends;
                    }
                    if (spec.StringSize > 0 && string.IsNullOrWhiteSpace(spec.Extends))
                    {
                        strEdt.StringSize = spec.StringSize;
                    }
                    newEdt = strEdt;
                    break;
                case EdtTypeSpec.Int:
                    newEdt = new AxEdtInt { Name = spec.Name };
                    break;
                case EdtTypeSpec.Int64:
                    newEdt = new AxEdtInt64 { Name = spec.Name };
                    break;
                case EdtTypeSpec.Real:
                    newEdt = new AxEdtReal { Name = spec.Name };
                    break;
                case EdtTypeSpec.Date:
                    newEdt = new AxEdtDate { Name = spec.Name };
                    break;
                case EdtTypeSpec.DateTime:
                    newEdt = new AxEdtDateTime { Name = spec.Name };
                    break;
                case EdtTypeSpec.Enum:
                    var enumEdt = new AxEdtEnum { Name = spec.Name };
                    if (!string.IsNullOrWhiteSpace(spec.EnumType))
                    {
                        enumEdt.EnumType = spec.EnumType;
                    }
                    newEdt = enumEdt;
                    break;
                case EdtTypeSpec.Guid:
                    newEdt = new AxEdtGuid { Name = spec.Name };
                    break;
                case EdtTypeSpec.Container:
                    newEdt = new AxEdtContainer { Name = spec.Name };
                    break;
                default:
                    newEdt = new AxEdtString { Name = spec.Name };
                    break;
            }

            newEdt.Label = spec.Label;
            newEdt.HelpText = spec.HelpText;

            if (!string.IsNullOrWhiteSpace(spec.ReferenceTable))
            {
                newEdt.ReferenceTable = spec.ReferenceTable;
            }

            _axHelper.MetaModelService.CreateExtendedDataType(newEdt, _axHelper.ModelSaveInfo);
            _axHelper.AppendToActiveProject(newEdt);

            // Add table reference if specified
            if (!string.IsNullOrWhiteSpace(spec.ReferenceTable) && !string.IsNullOrWhiteSpace(spec.ReferenceField))
            {
                newEdt = _axHelper.MetadataProvider.Edts.Read(spec.Name);
                if (newEdt != null)
                {
                    newEdt.AddTableReference(spec.ReferenceTable, spec.ReferenceField);
                    _axHelper.MetaModelService.UpdateExtendedDataType(newEdt, _axHelper.ModelSaveInfo);
                }
            }

            AddLog($"Created EDT: {spec.Name}");
        }

        private void UpdateExistingEdt(EdtSpec spec, AxEdt existing)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var edt = _axHelper.MetaModelService.GetExtendedDataType(spec.Name);
            if (!string.IsNullOrWhiteSpace(spec.Label))
            {
                edt.Label = spec.Label;
            }
            if (!string.IsNullOrWhiteSpace(spec.HelpText))
            {
                edt.HelpText = spec.HelpText;
            }

            if (edt is AxEdtString strEdt && spec.StringSize > 0)
            {
                strEdt.StringSize = spec.StringSize;
            }

            _axHelper.MetaModelService.UpdateExtendedDataType(edt, _axHelper.ModelSaveInfo);
            AddLog($"Updated existing EDT: {spec.Name}");
        }

        #endregion

        #region Enum Creation

        private void CreateEnum(EnumSpec spec)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var existingEnum = _axHelper.MetadataProvider.Enums.Read(spec.Name);

            if (existingEnum == null)
            {
                existingEnum = new AxEnum
                {
                    Name = spec.Name,
                    Label = spec.Label,
                    Help = spec.HelpText,
                    UseEnumValue = NoYes.No
                };

                _axHelper.MetaModelService.CreateEnum(existingEnum, _axHelper.ModelSaveInfo);
                _axHelper.AppendToActiveProject(existingEnum);

                AddLog($"Created Enum: {spec.Name}");

                // Refresh from store
                existingEnum = _axHelper.MetadataProvider.Enums.Read(spec.Name);
            }

            // Add values
            int index = 0;
            foreach (var valueSpec in spec.Values)
            {
                var enumValue = new AxEnumValue
                {
                    Name = valueSpec.Name,
                    Label = valueSpec.Label,
                    Value = valueSpec.Value ?? index
                };
                existingEnum.AddEnumValue(enumValue);
                index++;
            }

            _axHelper.MetaModelService.UpdateEnum(existingEnum, _axHelper.ModelSaveInfo);

            // Create EDT type if requested
            if (spec.CreateEdtType)
            {
                string edtTypeName = !string.IsNullOrWhiteSpace(spec.EdtTypeName) ? spec.EdtTypeName : $"{spec.Name}Type";

                if (_axHelper.MetadataProvider.Edts.Read(edtTypeName) == null)
                {
                    var enumEdt = new AxEdtEnum
                    {
                        Name = edtTypeName,
                        EnumType = spec.Name
                    };

                    _axHelper.MetaModelService.CreateExtendedDataType(enumEdt, _axHelper.ModelSaveInfo);
                    _axHelper.AppendToActiveProject(enumEdt);

                    AddLog($"Created Enum EDT: {edtTypeName}");
                }
            }
        }

        #endregion

        private void AddLog(string message)
        {
            _logBuilder.AppendLine(message);
        }
    }

    public class BuildResult
    {
        public bool Success { get; set; }
        public string Log { get; set; } = "";
        public List<string> CreatedObjects { get; set; } = new List<string>();
        public List<string> Errors { get; set; } = new List<string>();
        public ValidationResult ValidationResult { get; set; }

        public string GetDisplayMessage()
        {
            var sb = new StringBuilder();

            if (ValidationResult != null && ValidationResult.HasWarnings)
            {
                sb.AppendLine("=== Validation Warnings ===");
                sb.AppendLine(ValidationResult.GetSummary());
            }

            if (CreatedObjects.Count > 0)
            {
                sb.AppendLine("=== Created Objects ===");
                foreach (var obj in CreatedObjects)
                {
                    sb.AppendLine($"  ✓ {obj}");
                }
            }

            if (Errors.Count > 0)
            {
                sb.AppendLine("=== Errors ===");
                foreach (var err in Errors)
                {
                    sb.AppendLine($"  ✗ {err}");
                }
            }

            if (!string.IsNullOrWhiteSpace(Log))
            {
                sb.AppendLine();
                sb.AppendLine("=== Build Log ===");
                sb.AppendLine(Log);
            }

            return sb.ToString();
        }
    }
}
