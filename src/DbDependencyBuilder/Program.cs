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

        [Option('n', "names", Separator = ',', Required = false, HelpText = "Root sql objects.")]
        public IEnumerable<string> Names { get; set; }

        [Option('t', "types", Separator = ',', Required = false, HelpText = "Whitelist filter for sql object type of root. It works only together with fragment.")]
        public IEnumerable<RefObjectType> TypesToSearch { get; set; }

        [Option('f', "fragment", Required = false, HelpText = "Fragment of root sql object name.")]
        public string Fragment { get; set; }

        [Option('o', "output", Required = false, HelpText = "Directory for output files.")]
        public string OutputPath { get; set; }
    }

    class Program
    {
        private static Searcher _searcher;

        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(o =>
                {
                    if (!File.Exists(o.ConfigPath))
                    {
                        Console.WriteLine("Invalid path for json configuration file.");
                        return;
                    }

                    var config = JsonConvert.DeserializeObject<SearchConfig>(File.ReadAllText(o.ConfigPath));

                    if ((o.Names == null || !o.Names.Any()) && string.IsNullOrEmpty(o.Fragment))
                    {
                        Console.WriteLine("Provide --names or --fragment.");
                        return;
                    }

                    if (config.DbPath == null || config.DbPath.Count == 0)
                    {
                        Console.WriteLine("Provide at least one database in config file.");
                        return;
                    }

                    _searcher = new Searcher(config);

                    if (o.Names != null && o.Names.Any())
                    {
                        var objects = o.Names.Select(x =>_searcher.FindObjects(x)).SelectMany(x => x).ToList();
                        Process(objects, o);
                        return;
                    }

                    if (!string.IsNullOrEmpty(o.Fragment))
                    {
                        var objects = _searcher.FindObjects(o.Fragment, o.TypesToSearch);
                        Process(objects, o);
                    }
                });
        }

        static void Process(List<RefObject> roots, Options options)
        {
            Console.Write("loading...");
            var sw = Stopwatch.StartNew();
            
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine($" done {sw.Elapsed}");
            Console.ResetColor();

            Console.Write("searching...");
            sw = Stopwatch.StartNew();

            var result = FindUsages(roots);

            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine($" done {sw.Elapsed}");
            Console.ResetColor();

            Console.Write("vizualizing...");
            sw = Stopwatch.StartNew();

            var visualizer = new Visualizer(result, options.OutputPath);
            string treeFile = visualizer.BuildTree();
            string graphFile = visualizer.BuildGraph();

            Console.WriteLine($" done {sw.Elapsed}");
            Console.ResetColor();
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
