using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace HMT.Models.AiSchemas
{
    public class EdtSpec
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("type")]
        [JsonConverter(typeof(StringEnumConverter))]
        public EdtTypeSpec Type { get; set; } = EdtTypeSpec.String;

        [JsonProperty("extends")]
        public string Extends { get; set; } = "";

        [JsonProperty("label")]
        public string Label { get; set; } = "";

        [JsonProperty("helpText")]
        public string HelpText { get; set; } = "";

        [JsonProperty("stringSize")]
        public int StringSize { get; set; } = 0;

        [JsonProperty("referenceTable")]
        public string ReferenceTable { get; set; } = "";

        [JsonProperty("referenceField")]
        public string ReferenceField { get; set; } = "";

        /// <summary>
        /// For Enum EDT only - the base enum name
        /// </summary>
        [JsonProperty("enumType")]
        public string EnumType { get; set; } = "";
    }

    public enum EdtTypeSpec
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
}
