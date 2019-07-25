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
    //todo tooltip with matched fragment
    //todo mark db objects without usage (no final csarp or etl node)
    //todo colors for different types and legend
    //todo make graph bigger

    class Options
    {
        [Option('c', "config", Required = true, HelpText = "Path to json configuration file.")]
        public string ConfigPath { get; set; }

        [Option('n', "names", Separator = ',', Required = false, HelpText = "Root objects.")]
        public IEnumerable<string> Names { get; set; }

        [Option('t', "types", Separator = ',', Required = false, HelpText = "Whitelist filter for sql object type. It works only together with fragment.")]
        public IEnumerable<RefObjectType> TypesToSearch { get; set; }

        [Option('f', "fragment", Required = false, HelpText = "Fragment of sql object name.")]
        public string Fragment { get; set; }
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

                    if (o.Names != null && o.Names.Any())
                    {
                        Process(o.Names.Select(t => new RefObject
                        {
                            Name = t.Split('.')[1],
                            Type = RefObjectType.Tbl,
                            Db = t.Split('.')[0]
                        }).ToList(), config);
                        return;
                    }

                    if (!string.IsNullOrEmpty(o.Fragment))
                    {
                        //todo find sql object
                        //todo find its usages
                    }
                });
        }

        static void Process(List<RefObject> objects, SearchConfig config)
        {
            Console.Write("loading...");
            var sw = Stopwatch.StartNew();

            _searcher = new Searcher(config);

            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine($" done {sw.Elapsed}");
            Console.ResetColor();

            Console.Write("searching...");
            sw = Stopwatch.StartNew();

            var result = FindUsages(objects);

            Console.ForegroundColor = ConsoleColor.DarkGreen;
            Console.WriteLine($" done {sw.Elapsed}");

            Console.Write("vizualizing...");
            sw = Stopwatch.StartNew();

            var visualizer = new Visualizer();
            string treeFile = visualizer.RenderTree(result);
            string graphFile = visualizer.RenderGraph(result);

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
                    var usages = _searcher.Find(obj);
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
