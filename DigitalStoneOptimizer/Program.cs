using CommandLine;
using netDxf;
using SixLabors.ImageSharp;
using System;
using System.IO;
using System.Linq;

namespace DigitalStoneOptimizer
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("DigitalStone toolkit started.");
            Parser.Default.ParseArguments<CliOptions>(args).WithParsed(x =>
            {
                x.ModelFiles = x.ModelFiles.Select(y => CheckPath(y)).ToArray();
                var data = x.ModelFiles.Select(y => GeometryProvider.LoadStl(y));
                if (data.Any(y => y == null))
                {
                    Console.WriteLine("Failed to load input data.");
                    return;
                }
                ApproximatedStone[] s;
                try
                {
                    s = data.Select(y => new ApproximatedStone(y, x.SheetThickness, x.DesiredOverlap)).ToArray();
                }
                catch (InvalidOperationException ex)
                {
                    Console.WriteLine(ex);
                    return;
                }
                Console.WriteLine($"Specified mode = {Enum.GetName(typeof(Modes), x.Mode)}");
                switch (x.Mode)
                {
                    case Modes.PreviewGeometry:
                        foreach (var item in s)
                        {
                            PreviewGeometry(item, x);
                        }
                        break;
                    case Modes.GenerateMillingData:
                        GenerateMilling(s, x);
                        break;
                    case Modes.AssessProductionVolume:
                        AssessProductionVolume(s, x);
                        break;
                    default:
                        Console.WriteLine("Invalid mode specified.");
                        break;
                }
                Console.WriteLine("Finished.");
            });
        }

        static void AssessProductionVolume(ApproximatedStone[] s, CliOptions options)
        {
            var res = new MillingContext(s, options.ToolDiameter);
            for (int i = 1; i <= options.NumberOfStones; i++)
            {
                res.Calculate(i);
                Console.WriteLine("---");
                Console.WriteLine(GenerateOutputStats(res, i));
            }
        }

        static void GenerateMilling(ApproximatedStone[] s, CliOptions options)
        {
            var res = new MillingContext(s, options.ToolDiameter);
            res.Calculate(options.NumberOfStones);
            string stats = GenerateOutputStats(res, options.NumberOfStones);
            Console.WriteLine(stats);
            var doc = new DxfDocument();
            res.DrawDxf(doc, options.FlattenMillingOutput);
            doc.Save(CheckPath("positioned.dxf"));
            File.WriteAllText("milling.txt",
                $@"{stats}

Positioned bins:
{string.Join(Environment.NewLine, res.PositionedSections.Select(x => string.Join(", ", x.Select(y => y.Name))))}

Unable to fit:
{string.Join(Environment.NewLine, res.UnableToFit.Select(x => x.Name))}");
        }

        static void PreviewGeometry(ApproximatedStone s, CliOptions options)
        {
            GeometryProvider.SaveStl(s.GetMesh(), CheckPath($"geometry_{s.OriginalData.Name}.stl"));
            GeometryProvider.SaveDxf(s, CheckPath($"geometry_{s.OriginalData.Name}.dxf"));
            //s.GetImage().SaveAsPng(CheckPath("output.png"));
            //Export bounding box dimensions
            string stats = $@"Input mesh bounds: {s.OriginalData.Bounds:F0};
dimensions: {s.OriginalData.Bounds.Diagonal:F0}.

Output height for machining: {s.TotalHeight:F0}.";
            Console.WriteLine(stats);
            File.WriteAllText($"geometry_{s.OriginalData.Name}.txt", stats);
        }

        static string CheckPath(string src)
        {
            if (Path.IsPathFullyQualified(src)) return src;
            return Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, src));
        }

        static string GenerateOutputStats(MillingContext res, int productionVolume)
        {
            return @$"Stats: ProductionVolume = {productionVolume};
Total Sections = {res.TotalSections}, TotalSheets = {res.TotalSheets};
UnableToFit = {res.UnableToFit.Count}, FitSheets = {res.PositionedSections.Count};
VolumeEfficiency = {res.VolumeEfficiency:F3}, Compactization = {res.CompactizationFactor:F2}.";
        }
    }
}
