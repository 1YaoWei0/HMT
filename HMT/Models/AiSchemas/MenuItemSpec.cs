using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace HMT.Models.AiSchemas
{
    public class MenuItemSpec
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("objectName")]
        public string ObjectName { get; set; }

        [JsonProperty("type")]
        [JsonConverter(typeof(StringEnumConverter))]
        public MenuItemTypeSpec Type { get; set; } = MenuItemTypeSpec.Display;

        [JsonProperty("label")]
        public string Label { get; set; } = "";

        [JsonProperty("helpText")]
        public string HelpText { get; set; } = "";
    }

    public enum MenuItemTypeSpec
    {
        Display,
        Action,
        Output
    }
}
