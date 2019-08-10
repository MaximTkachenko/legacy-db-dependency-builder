using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace DbDependencyBuilder
{
    public class Searcher
    {
        private static readonly Dictionary<RefObjectType, string> RefObjectNames;

        static Searcher()
        {
            RefObjectNames = new Dictionary<RefObjectType, string>();
            foreach (RefObjectType t in Enum.GetValues(typeof(RefObjectType)))
            {
                var memInfo = typeof(RefObjectType).GetMember(t.ToString());
                var attributes = memInfo[0].GetCustomAttributes(typeof(DescriptionAttribute), false);
                RefObjectNames[t] = ((DescriptionAttribute) attributes[0]).Description;
            }
        }

        private static readonly Dictionary<RefObjectType, RefObjectType[]> SqlSearchRules =
            new Dictionary<RefObjectType, RefObjectType[]>
            {
                { RefObjectType.Tbl, new [] { RefObjectType.Syn, RefObjectType.Sp, RefObjectType.Fun, RefObjectType.V } },
                { RefObjectType.Syn, new [] { RefObjectType.Sp, RefObjectType.Fun, RefObjectType.V } },
                { RefObjectType.Sp, new [] { RefObjectType.Sp } },
                { RefObjectType.Fun, new [] { RefObjectType.Sp, RefObjectType.Fun, } },
                { RefObjectType.V, new [] { RefObjectType.Syn, RefObjectType.Sp, RefObjectType.Fun, RefObjectType.V } }
            };

        private readonly Dictionary<string, Dictionary<RefObjectType, List<(string Name, string Script)>>> _sql;
        private readonly Dictionary<string, string> _etl;
        private readonly Dictionary<string, Dictionary<string, string>> _csharp;

        private readonly bool _dbSearch;
        private readonly bool _etlSearch;
        private readonly bool _csharpSearch;

        public Searcher(SearchConfig config)
        {
            _dbSearch = config.DbPath != null && config.DbPath.Keys.Count > 0;
            _etlSearch = !string.IsNullOrEmpty(config.EtlPath);
            _csharpSearch = !string.IsNullOrEmpty(config.CsharpPath);

            if (_dbSearch)
            {
                _sql = new Dictionary<string, Dictionary<RefObjectType, List<(string Name, string Script)>>>();
                foreach (var root in config.DbPath)
                {
                    _sql[root.Key] = new Dictionary<RefObjectType, List<(string Name, string Script)>>();
                    foreach (RefObjectType t in Enum.GetValues(typeof(RefObjectType)))
                    {
                        var folder = $@"{root.Value}\{RefObjectNames[t]}";
                        if (!Directory.Exists(folder))
                        {
                            continue;
                        }

                        _sql[root.Key][t] = Directory.GetFiles(folder, "*.sql")
                            .Select(x => (Path.GetFileNameWithoutExtension(x), File.ReadAllText(x))).ToList();
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
            var result = new List<RefObject>();
            var sqlRegex = new Regex($@"[ .\t[]{{1}}{obj.Name}[ \]\t]{{1}}", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

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
                            .Select(x => new RefObject {Db = db.Key, Type = t, Name = x.Name}));
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
                    var csharpMapRegex = $"\"{obj.Name}\""; //table mapping or exec sp without parameters
                    var csharpExecRegex1 = $"exec {obj.Name}"; //exec sp
                    var csharpExecRegex2 = $"\"{obj.Name} "; //exec sp with parameters
                    var csharpFromRegex = $"from {obj.Name}"; //select from table
                    foreach (var sln in _csharp)
                    {
                        result.AddRange(sln.Value.Where(x =>
                                x.Value.Contains(csharpMapRegex, StringComparison.InvariantCultureIgnoreCase)
                                || x.Value.Contains(csharpExecRegex1, StringComparison.InvariantCultureIgnoreCase)
                                || x.Value.Contains(csharpExecRegex2, StringComparison.InvariantCultureIgnoreCase)
                                || x.Value.Contains(csharpFromRegex, StringComparison.InvariantCultureIgnoreCase))
                            .Select(x => new RefObject {Type = RefObjectType.Cs, Name = Path.GetFileName(x.Key)}));
                    }
                }
            }

            return result.GroupBy(x => new { x.Name, x.Type, x.Db }).Select(group => group.First()).ToList();
        }

        public List<RefObject> FindRoots(string[] names, RefObjectType[] types)
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
                            result.AddRange(scripts.Where(x => x.Name.Contains(name, StringComparison.InvariantCultureIgnoreCase))
                                .Select(x => new RefObject { Db = db.Key, Type = t, Name = x.Name }));
                        }
                    }
                }
            }

            return result;
        }
    }
}
