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
                x.ModelFile = CheckPath(x.ModelFile);
                var data = GeometryProvider.LoadStl(x.ModelFile);
                if (data == null)
                {
                    Console.WriteLine("Failed to load input data.");
                    return;
                }
                ApproximatedStone s;
                try
                {
                    s = new ApproximatedStone(data, x.SheetThickness, x.DesiredOverlap);
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
                        PreviewGeometry(s, data, x);
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

        static void AssessProductionVolume(ApproximatedStone s, CliOptions options)
        {
            var res = new MillingContext(s, options.ToolDiameter);
            for (int i = 1; i <= options.NumberOfStones; i++)
            {
                res.Calculate(i);
                Console.WriteLine("---");
                Console.WriteLine(GenerateOutputStats(s, res, i));
            }
        }

        static void GenerateMilling(ApproximatedStone s, CliOptions options)
        {
            var res = new MillingContext(s, options.ToolDiameter);
            res.Calculate(options.NumberOfStones);
            string stats = GenerateOutputStats(s, res, options.NumberOfStones);
            Console.WriteLine(stats);
            var doc = new DxfDocument();
            res.DrawDxf(doc);
            doc.Save(CheckPath("positioned.dxf"));
            File.WriteAllText("milling.txt",
                $@"{stats}

Positioned bins:
{string.Join(Environment.NewLine, res.PositionedElevations.Select(x => string.Join(", ", x.Select(y => y.ToString("F0")))))}

Unable to fit:
{string.Join(Environment.NewLine, res.NonFitElevations.Select(x => x.ToString("F0")))}");
        }

        static void PreviewGeometry(ApproximatedStone s, StoneMeshData d, CliOptions options)
        {
            GeometryProvider.SaveStl(s.GetMesh(), CheckPath("geometry.stl"));
            GeometryProvider.SaveDxf(s, CheckPath("geometry.dxf"));
            //s.GetImage().SaveAsPng(CheckPath("output.png"));
            //Export bounding box dimensions
            string stats = $@"Input mesh bounds: {d.Bounds:F0};
dimensions: {d.Bounds.Diagonal:F0}.

Output height for machining: {s.TotalHeight:F0}.";
            Console.WriteLine(stats);
            File.WriteAllText("geometry.txt", stats);
        }

        static string CheckPath(string src)
        {
            if (Path.IsPathFullyQualified(src)) return src;
            return Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, src));
        }

        static string GenerateOutputStats(ApproximatedStone s, MillingContext res, int productionVolume)
        {
            return @$"Stats: ProductionVolume = {productionVolume};
Total Sections = {s.Sections.Length}, TotalSheets = {res.TotalSheets};
UnableToFit = {res.UnableToFit.Count}, FitSheets = {res.PositionedSections.Count};
VolumeEfficiency = {res.VolumeEfficiency:F3}, Compactization = {res.CompactizationFactor:F2}.";
        }
    }
}
