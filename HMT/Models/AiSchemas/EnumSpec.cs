using Newtonsoft.Json;
using System.Collections.Generic;

namespace HMT.Models.AiSchemas
{
    public class EnumSpec
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("label")]
        public string Label { get; set; } = "";

        [JsonProperty("helpText")]
        public string HelpText { get; set; } = "";

        [JsonProperty("values")]
        public List<EnumValueSpec> Values { get; set; } = new List<EnumValueSpec>();

        [JsonProperty("createEdtType")]
        public bool CreateEdtType { get; set; } = false;

        [JsonProperty("edtTypeName")]
        public string EdtTypeName { get; set; } = "";
    }

    public class EnumValueSpec
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("label")]
        public string Label { get; set; } = "";

        [JsonProperty("value")]
        public int? Value { get; set; }
    }
}
