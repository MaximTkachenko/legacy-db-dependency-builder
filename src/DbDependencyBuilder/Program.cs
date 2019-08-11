using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using Newtonsoft.Json;

namespace DbDependencyBuilder
{
    class Options
    {
        [Option('c', "config", Required = true, HelpText = "Path to json configuration file.")]
        public string ConfigPath { get; set; }

        [Option('n', "names", Separator = ',', Required = true, HelpText = " Comma separated root sql objects. Provide fragment of name or full name.")]
        public IEnumerable<string> Names { get; set; }

        [Option('t', "types", Separator = ',', Required = false, HelpText = "Filter for sql object type of root. Possible values: Tbl (table), Syn (synonym), Sp (stored procedure), Fun (function), V (view). All types by default.")]
        public IEnumerable<RefObjectType> TypesToSearch { get; set; }

        [Option('o', "output", Required = true, HelpText = "Directory for output files.")]
        public string OutputPath { get; set; }

        [Option('e', "exact", Required = false, HelpText = "Define how to search for roots. 1 means 'equals', 0 means 'contains'")]
        public byte ExactMatch { get; set; } = 1;
    }

    class Program
    {
        private static Searcher _searcher;

        static void Main(string[] args)
        {
            //Process(new Options
            //{
            //    Names = new[] { "Person" },
            //    OutputPath = @"C:\code\repos\legacy-db-dependency-builder\src\DbDependencyBuilder\bin\Debug\netcoreapp2.2",
            //    TypesToSearch = new[] { RefObjectType.Tbl }
            //}, new SearchConfig
            //{
            //    DbPath = new Dictionary<string, string>
            //    {
            //        {"main", @"C:\code\repos\legacy-db-dependency-builder\sample\sample-db" }
            //    },
            //    CsharpPath = @"C:\code\repos\legacy-db-dependency-builder\sample\sample-app"
            //});
            //return;

            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(options =>
                {
                    if (options.TypesToSearch == null || !options.TypesToSearch.Any())
                    {
                        options.TypesToSearch = new[]
                        {
                            RefObjectType.Tbl, RefObjectType.Syn, RefObjectType.Sp, RefObjectType.Fun, RefObjectType.V
                        };
                    }

                    if (!File.Exists(options.ConfigPath))
                    {
                        Console.WriteLine("Invalid path for json configuration file.");
                        return;
                    }

                    var config = JsonConvert.DeserializeObject<SearchConfig>(File.ReadAllText(options.ConfigPath));

                    if (config.DbPath == null || config.DbPath.Count == 0)
                    {
                        Console.WriteLine("Provide at least one database in config file.");
                        return;
                    }

                    Process(options, config);
                });
        }

        static void Process(Options options, SearchConfig config)
        {
            Console.Write("loading...");
            var sw = Stopwatch.StartNew();

            _searcher = new Searcher(config);

            sw.Stop();
            Console.WriteLine($" done {sw.Elapsed}");

            Console.Write("searching for roots...");
            sw = Stopwatch.StartNew();

            var objects = _searcher.FindRoots(options.Names.ToArray(), options.TypesToSearch.ToArray(), options.ExactMatch > 0);

            if (objects.Count == 0)
            {
                Console.WriteLine("no objects found");
                return;
            }

            sw.Stop();
            Console.WriteLine($" done {sw.Elapsed}, found {objects.Count}:");
            foreach (var obj in objects)
            {
                Console.WriteLine($"- {obj.Db}.{obj.Name} {obj.Type.ToString().ToLower()}");
            }

            Console.Write("searching for usages...");
            sw = Stopwatch.StartNew();

            var result = FindUsages(objects);

            sw.Stop();
            Console.WriteLine($" done {sw.Elapsed}");

            Console.Write("visualizing...");
            sw = Stopwatch.StartNew();

            var visualizer = new Visualizer(result, options.OutputPath, options.Names);
            string treeFile = visualizer.BuildTree();
            string graphFile = visualizer.BuildGraph();

            sw.Stop();
            Console.WriteLine($" done {sw.Elapsed}");
            Console.WriteLine();
            Console.WriteLine($"tree: {treeFile}");
            Console.WriteLine($"graph: {graphFile}");
        }

        static (List<RefObject> Objects, int MaxChildren, int Nesting) FindUsages(List<RefObject> objects)
        {
            int maxChildren = 0;
            int nesting = 0;
            var toCheck = objects;
            while (toCheck.Count > 0)
            {
                toCheck = FindUsagesIml(toCheck);

                if (toCheck.Count > maxChildren)
                {
                    maxChildren = toCheck.Count;
                }

                nesting++;
            }

            return (objects, maxChildren, nesting);
        }

        static List<RefObject> FindUsagesIml(List<RefObject> objects)
        {
            var result = new List<RefObject>();
            var lockObj = new object();

            Parallel.ForEach(objects, 
                () => new List<RefObject>(),
                (obj, state, local) =>
                {
                    var usages = _searcher.FindUsages(obj);
                    obj.Usages = usages;
                    local.AddRange(usages);
                    return local;
                },
                final =>
                {
                    lock (lockObj)
                    {
                        result.AddRange(final);
                    }
                });

            return result;
        }
    }
}
