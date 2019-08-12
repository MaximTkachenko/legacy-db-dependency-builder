using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace DbDependencyBuilder
{
    public class Searcher
    {
        private static readonly Dictionary<RefObjectType, string> DbObjects = new Dictionary<RefObjectType, string>
        {
            { RefObjectType.Tbl, GetDescription(RefObjectType.Tbl) },
            { RefObjectType.Syn, GetDescription(RefObjectType.Syn) },
            { RefObjectType.Sp, GetDescription(RefObjectType.Sp) },
            { RefObjectType.Fun, GetDescription(RefObjectType.Fun) },
            { RefObjectType.V, GetDescription(RefObjectType.V) }
        };

        private static readonly Dictionary<RefObjectType, RefObjectType[]> SqlSearchRules =
            new Dictionary<RefObjectType, RefObjectType[]>
            {
                { RefObjectType.Tbl, new [] { RefObjectType.Syn, RefObjectType.Sp, RefObjectType.Fun, RefObjectType.V } },
                { RefObjectType.Syn, new [] { RefObjectType.Sp, RefObjectType.Fun, RefObjectType.V } },
                { RefObjectType.Sp, new [] { RefObjectType.Sp } },
                { RefObjectType.Fun, new [] { RefObjectType.Sp, RefObjectType.Fun, } },
                { RefObjectType.V, new [] { RefObjectType.Syn, RefObjectType.Sp, RefObjectType.Fun, RefObjectType.V } }
            };

        private readonly Dictionary<string, Dictionary<RefObjectType, List<(string Name, string Schema, string Script)>>> _sql;
        private readonly Dictionary<string, string> _etl;
        private readonly Dictionary<string, Dictionary<string, string>> _csharp;

        private readonly ConcurrentDictionary<string, List<RefObject>> _cache;

        private readonly bool _dbSearch;
        private readonly bool _etlSearch;
        private readonly bool _csharpSearch;

        public Searcher(SearchConfig config)
        {
            _cache = new ConcurrentDictionary<string, List<RefObject>>();

            _dbSearch = config.DbPath != null && config.DbPath.Keys.Count > 0;
            _etlSearch = !string.IsNullOrEmpty(config.EtlPath);
            _csharpSearch = !string.IsNullOrEmpty(config.CsharpPath);

            if (_dbSearch)
            {
                _sql = new Dictionary<string, Dictionary<RefObjectType, List<(string Name, string Schema, string Script)>>>();
                foreach (var root in config.DbPath)
                {
                    _sql[root.Key] = new Dictionary<RefObjectType, List<(string Name, string Schema, string Script)>>();

                    var schemaFolders = Directory.GetDirectories(root.Value)
                        .Where(s => Directory.GetDirectories(s).Any(f => DbObjects.Values.Contains(Path.GetFileName(f))));

                    foreach (var schemaFolder in schemaFolders)
                    {
                        var schema = Path.GetFileName(schemaFolder);
                        foreach (var pair in DbObjects)
                        {
                            var folder = Path.Combine(schemaFolder, pair.Value);
                            if (!Directory.Exists(folder))
                            {
                                continue;
                            }

                            if (!_sql[root.Key].ContainsKey(pair.Key))
                            {
                                _sql[root.Key][pair.Key] = new List<(string Name, string Schema, string Script)>();
                            }

                            _sql[root.Key][pair.Key].AddRange(Directory.GetFiles(folder, "*.sql")
                                .Select(x => (Path.GetFileNameWithoutExtension(x), schema, File.ReadAllText(x))));
                        }
                    }
                }
            }

            if (_etlSearch)
            {
                _etl = Directory.GetFiles(config.EtlPath, "*.dtsx").ToDictionary(x => Path.GetFileNameWithoutExtension(x), x => File.ReadAllText(x));
            }

            if (_csharpSearch)
            {
                _csharp = new Dictionary<string, Dictionary<string, string>>();
                foreach (var sln in Directory.GetFiles(config.CsharpPath, "*.sln", SearchOption.AllDirectories))
                {
                    _csharp[Path.GetFileNameWithoutExtension(sln)] = Directory.GetFiles(Path.GetDirectoryName(sln),
                            "*.*", SearchOption.AllDirectories)
                        .Where(s => s.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) || s.EndsWith(".edmx", StringComparison.OrdinalIgnoreCase))
                        .ToDictionary(x => x, x => File.ReadAllText(x));
                }
            }
        }

        public List<RefObject> FindUsages(RefObject obj)
        {
            var key = $"{obj.DbSchema}.{obj.Name}".ToLower();
            if (_cache.TryGetValue(key, out var result))
            {
                return result;
            }

            result = new List<RefObject>();
            var sqlRegex = new Regex($@"[^a-zA-Z0-9]{{1}}{obj.Name}[^a-zA-Z0-9]{{1}}", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

            if (_dbSearch)
            {
                foreach (var db in _sql)
                {
                    if (!SqlSearchRules.TryGetValue(obj.Type, out var rules))
                    {
                        continue;
                    }

                    foreach (var t in rules)
                    {
                        if (!db.Value.TryGetValue(t, out var scripts))
                        {
                            continue;
                        }

                        result.AddRange(scripts
                            .Where(x => !(x.Name.Equals(obj.Name, StringComparison.OrdinalIgnoreCase) &&
                                          t == obj.Type && db.Key.Equals(obj.Db)))
                            .Where(x => sqlRegex.IsMatch(x.Script))
                            .Select(x => new RefObject {Db = db.Key, Type = t, Name = x.Name, DbSchema = x.Schema}));
                    }
                }
            }

            if (obj.Type != RefObjectType.Cs || obj.Type != RefObjectType.Etl)
            {
                if (_etlSearch)
                {
                    result.AddRange(_etl.Where(x => sqlRegex.IsMatch(x.Value)).Select(x => new RefObject {Type = RefObjectType.Etl, Name = x.Key}));
                }

                if (_csharpSearch)
                {
                    var condition = GetCsharpSearchCondition(obj);
                    if (condition != null)
                    {
                        foreach (var sln in _csharp)
                        {
                            result.AddRange(sln.Value.Where(condition)
                                .Select(x => new RefObject { Type = RefObjectType.Cs, Name = Path.GetFileName(x.Key) }));
                        }
                    }
                }
            }

            result = result.GroupBy(x => new { x.Name, x.Type, x.Db }).Select(group => group.First()).ToList();
            _cache.TryAdd(key, result);
            return result;
        }

        public List<RefObject> FindRoots(string[] names, RefObjectType[] types, bool exactMatch)
        {
            var result = new List<RefObject>();
            if (_dbSearch)
            {
                foreach (var db in _sql)
                {
                    foreach (var t in types)
                    {
                        if (!db.Value.TryGetValue(t, out var scripts))
                        {
                            continue;
                        }

                        foreach (var name in names)
                        {
                            var found = exactMatch
                                ? scripts.Where(x => x.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                                : scripts.Where(x => x.Name.Contains(name, StringComparison.InvariantCultureIgnoreCase));
                            result.AddRange(found.Select(x => new RefObject { Db = db.Key, Type = t, Name = x.Name, DbSchema = x.Schema}));
                        }
                    }
                }
            }

            return result;
        }

        private static Func<KeyValuePair<string, string>, bool> GetCsharpSearchCondition(RefObject obj)
        {
            if (obj.Type == RefObjectType.Tbl || obj.Type == RefObjectType.V)
            {
                var tablePattern1 = $"\"{obj.Name}\"";
                var tablePattern2 = $"\"{obj.DbSchema}.{obj.Name}\"";
                var tablePattern3 = $"from {obj.Name}";
                var tablePattern4 = $"from {obj.DbSchema}.{obj.Name}";
                var tablePattern5 = $"join {obj.Name}";
                var tablePattern6 = $"join {obj.DbSchema}.{obj.Name}";
                var tablePattern7 = $"into {obj.Name}";
                var tablePattern8 = $"into {obj.DbSchema}.{obj.Name}";
                Func<KeyValuePair<string, string>, bool> tableCondition = x =>
                    x.Value.Contains(tablePattern1, StringComparison.InvariantCultureIgnoreCase)
                    || x.Value.Contains(tablePattern2, StringComparison.InvariantCultureIgnoreCase)
                    || x.Value.Contains(tablePattern3, StringComparison.InvariantCultureIgnoreCase)
                    || x.Value.Contains(tablePattern4, StringComparison.InvariantCultureIgnoreCase)
                    || x.Value.Contains(tablePattern5, StringComparison.InvariantCultureIgnoreCase)
                    || x.Value.Contains(tablePattern6, StringComparison.InvariantCultureIgnoreCase)
                    || x.Value.Contains(tablePattern7, StringComparison.InvariantCultureIgnoreCase)
                    || x.Value.Contains(tablePattern8, StringComparison.InvariantCultureIgnoreCase);

                return tableCondition;
            }

            if (obj.Type == RefObjectType.Sp)
            {
                var spPattern1 = $"\"{obj.Name}\"";
                var spPattern2 = $"\"{obj.DbSchema}.{obj.Name}\"";
                var spPattern3 = $"\"{obj.Name} ";
                var spPattern4 = $"\"{obj.DbSchema}.{obj.Name} ";
                var spPattern5 = $"exec {obj.Name}\"";
                var spPattern6 = $"exec {obj.DbSchema}.{obj.Name}\"";
                var spPattern7 = $"exec {obj.Name} ";
                var spPattern8 = $"exec {obj.DbSchema}.{obj.Name} ";
                Func<KeyValuePair<string, string>, bool> spCondition = x =>
                    x.Value.Contains(spPattern1, StringComparison.InvariantCultureIgnoreCase)
                    || x.Value.Contains(spPattern2, StringComparison.InvariantCultureIgnoreCase)
                    || x.Value.Contains(spPattern3, StringComparison.InvariantCultureIgnoreCase)
                    || x.Value.Contains(spPattern4, StringComparison.InvariantCultureIgnoreCase)
                    || x.Value.Contains(spPattern5, StringComparison.InvariantCultureIgnoreCase)
                    || x.Value.Contains(spPattern6, StringComparison.InvariantCultureIgnoreCase)
                    || x.Value.Contains(spPattern7, StringComparison.InvariantCultureIgnoreCase)
                    || x.Value.Contains(spPattern8, StringComparison.InvariantCultureIgnoreCase);

                return spCondition;
            }

            return null;
        }

        private static string GetDescription<T>(T item)
        {
            var memInfo = typeof(RefObjectType).GetMember(item.ToString());
            var attributes = memInfo[0].GetCustomAttributes(typeof(DescriptionAttribute), false);
            return ((DescriptionAttribute)attributes[0]).Description;
        }
    }
}
