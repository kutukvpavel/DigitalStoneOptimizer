using CommandLine;
using netDxf;
using SixLabors.ImageSharp;
using System;
using System.IO;

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
                var data = GeometryProvider.LoadFbx(x.ModelFile);
                if (data == null)
                {
                    Console.WriteLine("Failed to load input data.");
                    return;
                }
                ApproximatedStone s = new ApproximatedStone(data, x.SheetThickness, x.DesiredOverlap);
                Console.WriteLine($"Specified mode = {Enum.GetName(typeof(Modes), x.Mode)}");
                switch (x.Mode)
                {
                    case Modes.PreviewGeometry:
                        PreviewGeometry(s, x);
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
            var res = new MillingContext(s);
            for (int i = 1; i <= options.NumberOfStones; i++)
            {
                res.Calculate(i);
                Console.WriteLine("---");
                OutputStats(res, i);
            }
        }

        static void GenerateMilling(ApproximatedStone s, CliOptions options)
        {
            var res = new MillingContext(s);
            res.Calculate(options.NumberOfStones);
            var doc = new DxfDocument();
            res.DrawDxf(doc);
            doc.Save(CheckPath("positioned.dxf"));
            OutputStats(res, options.NumberOfStones);
        }

        static void PreviewGeometry(ApproximatedStone s, CliOptions options)
        {
            GeometryProvider.SaveStl(s.GetMesh(), CheckPath("geometry.stl"));
            GeometryProvider.SaveDxf(s, CheckPath("geometry.dxf"));
            //s.GetImage().SaveAsPng(CheckPath("output.png"));
        }

        static string CheckPath(string src)
        {
            if (Path.IsPathFullyQualified(src)) return src;
            return Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, src));
        }

        static void OutputStats(MillingContext res, int productionVolume)
        {
            Console.WriteLine(@$"Stats: Volume = {productionVolume}, TotalSheets = {res.TotalSheets};
UnableToFit = {res.UnableToFit.Count}, FitSheets = {res.PositionedSections.Count};
VolumeEfficiency = {res.VolumeEfficiency:F3}");
        }
    }
}
