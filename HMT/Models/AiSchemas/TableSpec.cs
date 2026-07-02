using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.Generic;

namespace HMT.Models.AiSchemas
{
    public class TableSpec
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("label")]
        public string Label { get; set; } = "";

        [JsonProperty("tableGroup")]
        public string TableGroup { get; set; } = "Group";

        [JsonProperty("cacheLevel")]
        public string CacheLevel { get; set; } = "Found";

        [JsonProperty("createdBy")]
        public bool CreatedBy { get; set; } = true;

        [JsonProperty("createdDateTime")]
        public bool CreatedDateTime { get; set; } = true;

        [JsonProperty("modifiedBy")]
        public bool ModifiedBy { get; set; } = true;

        [JsonProperty("modifiedDateTime")]
        public bool ModifiedDateTime { get; set; } = true;

        [JsonProperty("titleField1")]
        public string TitleField1 { get; set; } = "";

        [JsonProperty("titleField2")]
        public string TitleField2 { get; set; } = "";

        [JsonProperty("fields")]
        public List<TableFieldSpec> Fields { get; set; } = new List<TableFieldSpec>();

        [JsonProperty("indexes")]
        public List<TableIndexSpec> Indexes { get; set; } = new List<TableIndexSpec>();

        [JsonProperty("fieldGroups")]
        public List<TableFieldGroupSpec> FieldGroups { get; set; } = new List<TableFieldGroupSpec>();

        [JsonProperty("relations")]
        public List<TableRelationSpec> Relations { get; set; } = new List<TableRelationSpec>();

        /// <summary>
        /// Optionally create companion objects (form, menuItem, privileges) together with the table.
        /// </summary>
        [JsonProperty("createForm")]
        public bool CreateForm { get; set; } = false;

        [JsonProperty("createMenuItem")]
        public bool CreateMenuItem { get; set; } = false;

        [JsonProperty("createPrivileges")]
        public bool CreatePrivileges { get; set; } = false;
    }

    public class TableFieldSpec
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("type")]
        [JsonConverter(typeof(StringEnumConverter))]
        public TableFieldType Type { get; set; } = TableFieldType.String;

        [JsonProperty("edt")]
        public string Edt { get; set; } = "";

        [JsonProperty("enumType")]
        public string EnumType { get; set; } = "";

        [JsonProperty("label")]
        public string Label { get; set; } = "";

        [JsonProperty("helpText")]
        public string HelpText { get; set; } = "";

        [JsonProperty("mandatory")]
        public bool Mandatory { get; set; } = false;

        [JsonProperty("allowEdit")]
        public bool AllowEdit { get; set; } = true;

        [JsonProperty("allowEditOnCreate")]
        public bool AllowEditOnCreate { get; set; } = true;

        [JsonProperty("ignoreEdtRelation")]
        public bool IgnoreEdtRelation { get; set; } = false;

        [JsonProperty("stringSize")]
        public int StringSize { get; set; } = 0;
    }

    public enum TableFieldType
    {
        String,
        Int,
        Int64,
        Real,
        Date,
        DateTime,
        Enum,
        Guid,
        Container
    }

    public class TableIndexSpec
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("fields")]
        public List<string> Fields { get; set; } = new List<string>();

        [JsonProperty("alternateKey")]
        public bool AlternateKey { get; set; } = false;

        [JsonProperty("allowDuplicates")]
        public bool AllowDuplicates { get; set; } = false;
    }

    public class TableFieldGroupSpec
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("label")]
        public string Label { get; set; } = "";

        [JsonProperty("fields")]
        public List<string> Fields { get; set; } = new List<string>();

        [JsonProperty("isSystemGenerated")]
        public bool IsSystemGenerated { get; set; } = false;

        [JsonProperty("autoPopulate")]
        public bool AutoPopulate { get; set; } = false;
    }

    public class TableRelationSpec
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("relatedTable")]
        public string RelatedTable { get; set; }

        [JsonProperty("constraints")]
        public List<RelationConstraintSpec> Constraints { get; set; } = new List<RelationConstraintSpec>();

        [JsonProperty("cardinality")]
        public string Cardinality { get; set; } = "ZeroMore";

        [JsonProperty("relatedTableCardinality")]
        public string RelatedTableCardinality { get; set; } = "ExactlyOne";
    }

    public class RelationConstraintSpec
    {
        [JsonProperty("field")]
        public string Field { get; set; }

        [JsonProperty("relatedField")]
        public string RelatedField { get; set; }
    }
}
