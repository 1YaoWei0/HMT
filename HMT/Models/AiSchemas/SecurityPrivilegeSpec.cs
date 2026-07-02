using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.Generic;

namespace HMT.Models.AiSchemas
{
    public class SecurityPrivilegeSpec
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("label")]
        public string Label { get; set; } = "";

        [JsonProperty("accessLevel")]
        [JsonConverter(typeof(StringEnumConverter))]
        public PrivilegeAccessLevelSpec AccessLevel { get; set; } = PrivilegeAccessLevelSpec.Read;

        [JsonProperty("entryPoints")]
        public List<EntryPointSpec> EntryPoints { get; set; } = new List<EntryPointSpec>();

        [JsonProperty("dataEntityPermissions")]
        public List<DataEntityPermissionSpec> DataEntityPermissions { get; set; } = new List<DataEntityPermissionSpec>();
    }

    public enum PrivilegeAccessLevelSpec
    {
        Read,
        Update,
        Create,
        Correct,
        Delete
    }

    public class EntryPointSpec
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("objectName")]
        public string ObjectName { get; set; }

        [JsonProperty("objectType")]
        [JsonConverter(typeof(StringEnumConverter))]
        public EntryPointObjectTypeSpec ObjectType { get; set; } = EntryPointObjectTypeSpec.MenuItemDisplay;

        [JsonProperty("forms")]
        public List<string> Forms { get; set; } = new List<string>();
    }

    public enum EntryPointObjectTypeSpec
    {
        MenuItemDisplay,
        MenuItemAction,
        MenuItemOutput
    }

    public class DataEntityPermissionSpec
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("integrationMode")]
        public string IntegrationMode { get; set; } = "All";
    }
}
