using System.Collections.Generic;
using Newtonsoft.Json;

namespace DbDependencyBuilder
{
    public class SearchConfig
    {
        [JsonProperty("db")]
        public Dictionary<string, string> DbPath { get; set; }

        [JsonProperty("etl")]
        public string EtlPath { get; set; }

        [JsonProperty("csharp")]
        public string CsharpPath { get; set; }
    }
}
