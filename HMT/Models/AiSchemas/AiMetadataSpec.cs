using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.Generic;

namespace HMT.Models.AiSchemas
{
    /// <summary>
    /// Top-level wrapper for AI-generated metadata specifications.
    /// AI outputs this JSON, and the VSIX SDK consumes it to create X++ objects.
    /// </summary>
    public class AiMetadataSpec
    {
        [JsonProperty("version")]
        public string Version { get; set; } = "1.0";

        [JsonProperty("objects")]
        public List<MetadataObjectSpec> Objects { get; set; } = new List<MetadataObjectSpec>();
    }

    /// <summary>
    /// A single metadata object specification with a type discriminator.
    /// </summary>
    public class MetadataObjectSpec
    {
        [JsonProperty("objectType")]
        [JsonConverter(typeof(StringEnumConverter))]
        public MetadataObjectType ObjectType { get; set; }

        [JsonProperty("table")]
        public TableSpec Table { get; set; }

        [JsonProperty("form")]
        public FormSpec Form { get; set; }

        [JsonProperty("securityPrivilege")]
        public SecurityPrivilegeSpec SecurityPrivilege { get; set; }

        [JsonProperty("menuItem")]
        public MenuItemSpec MenuItem { get; set; }

        [JsonProperty("edt")]
        public EdtSpec Edt { get; set; }

        [JsonProperty("enum")]
        public EnumSpec Enum { get; set; }
    }

    public enum MetadataObjectType
    {
        Table,
        Form,
        SecurityPrivilege,
        MenuItem,
        Edt,
        Enum
    }
}
