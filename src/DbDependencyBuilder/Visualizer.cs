using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace DbDependencyBuilder
{
    public class Visualizer
    {
        private static readonly string Invalid = new string (Path.GetInvalidFileNameChars()) + new string (Path.GetInvalidPathChars());

        private readonly long _ts;
        private readonly (List<RefObject> Objects, int MaxChildren, int Nesting) _data;
        private readonly string _output;
        private readonly string _title;

        public Visualizer((List<RefObject> Objects, int MaxChildren, int Nesting) data, string output, IEnumerable<string> names)
        {
            _ts = (long) (DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            _data = data;
            _output = output;

            _title = string.Join('_', names);
            foreach (char c in Invalid)
            {
                _title = _title.Replace(c.ToString(), "");
            }

            _title = _title.Replace(" ", "_");
        }

        public string BuildTree()
        {
            var height = _data.MaxChildren * 100;
            var width = _data.Nesting * 600;

            var tree = new[] { new RefObject { Usages = _data.Objects } };

            var markup = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "templates", "tree.html"))
                .Replace("%title%", _title)
                .Replace("%data%", JsonConvert.SerializeObject(tree))
                .Replace("%height%", height.ToString())
                .Replace("%width%", width.ToString());

            var file = Path.Combine(GetFileName("tree", _title));
            File.WriteAllText(file, markup);
            return file;
        }

        public string BuildGraph()
        {
            var height = 1000;
            var width = 1000;

            var nodes = new List<Node>();
            var links = new List<Link>();
            var toCheck = _data.Objects;
            while (toCheck.Count > 0)
            {
                var nextToCheck = new List<RefObject>();
                foreach (var obj in toCheck)
                {
                    nodes.Add(new Node(obj.NameToRender, (int)obj.Type));
                    links.AddRange(obj.Usages.Select(x => new Link(obj.NameToRender, x.NameToRender)));
                    nextToCheck.AddRange(obj.Usages);
                }

                toCheck = nextToCheck;
            }

            var graph = new GraphData
            {
                Nodes = nodes.GroupBy(customer => customer.Id.ToUpper()).Select(group => group.First()).ToList(),
                Links = links
            };

            var markup = File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), "templates", "graph.html"))
                .Replace("%title%", _title)
                .Replace("%data%", JsonConvert.SerializeObject(graph))
                .Replace("%height%", height.ToString())
                .Replace("%width%", width.ToString());

            var file = Path.Combine(GetFileName("graph", _title));
            File.WriteAllText(file, markup);
            return file;
        }

        private string GetFileName(string type, string title)
        {
            var filename = $"{_ts}_{type}_{title}";
            filename = filename.Length > 40 ? filename.Substring(0, 40) : filename;
            return Path.Combine(_output, $"{filename}.html");
        }

        private sealed class GraphData
        {
            [JsonProperty("nodes")]
            public List<Node> Nodes { get; set; }

            [JsonProperty("links")]
            public List<Link> Links { get; set; }
        }

        private sealed class Node
        {
            public Node(string id, int group)
            {
                Id = id;
                Group = group;
            }

            [JsonProperty("id")]
            public string Id { get; }

            [JsonProperty("group")]
            public int Group { get; }
        }

        private sealed class Link
        {
            public Link(string sourceId, string targetId)
            {
                SourceId = sourceId;
                TargetId = targetId;
            }

            [JsonProperty("source")]
            public string SourceId { get; }

            [JsonProperty("target")]
            public string TargetId { get; }
        }
    }
}
