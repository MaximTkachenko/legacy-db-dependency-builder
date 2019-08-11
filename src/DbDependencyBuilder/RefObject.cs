using System.Collections.Generic;
using Newtonsoft.Json;

namespace DbDependencyBuilder
{
    //todo refactor
    public class RefObject
    {
        private const string Root = "root";

        [JsonIgnore]
        public string Db { get; set; }

        [JsonIgnore]
        public RefObjectType Type { get; set; }

        [JsonIgnore]
        public string DbSchema { get; set; }
        
        [JsonIgnore]
        public string Name { get; set; } = Root;

        [JsonProperty("children")]
        public List<RefObject> Usages { get; set; }

        [JsonIgnore]
        public bool IsRoot => Name == Root;

        [JsonProperty("name")]
        public string NameToRender => IsRoot
            ? Name
            : string.IsNullOrEmpty(Db) ? $"{Name} [{Type.ToString().ToUpper()}]" : $"{Db.ToUpper()}.{Name} [{Type.ToString().ToUpper()}]";
    }
}
