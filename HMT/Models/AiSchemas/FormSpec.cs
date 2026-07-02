using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.Generic;

namespace HMT.Models.AiSchemas
{
    public class FormSpec
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("label")]
        public string Label { get; set; } = "";

        [JsonProperty("helpText")]
        public string HelpText { get; set; } = "";

        [JsonProperty("pattern")]
        [JsonConverter(typeof(StringEnumConverter))]
        public FormPatternType Pattern { get; set; } = FormPatternType.SimpleList;

        [JsonProperty("dataSource")]
        public string DataSource { get; set; }

        [JsonProperty("gridFields")]
        public List<string> GridFields { get; set; } = new List<string>();

        [JsonProperty("detailsHeaderFields")]
        public List<string> DetailsHeaderFields { get; set; } = new List<string>();

        [JsonProperty("tabPages")]
        public List<FormTabPageSpec> TabPages { get; set; } = new List<FormTabPageSpec>();

        [JsonProperty("createMenuItem")]
        public bool CreateMenuItem { get; set; } = true;
    }

    public enum FormPatternType
    {
        SimpleList,
        SimpleListDetails
    }

    public class FormTabPageSpec
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("caption")]
        public string Caption { get; set; } = "";

        [JsonProperty("fields")]
        public List<string> Fields { get; set; } = new List<string>();
    }
}
