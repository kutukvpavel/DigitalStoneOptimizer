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
                switch (x.Mode)
                {
                    case Modes.PreviewGeometry:
                        PreviewGeometry(s, x);
                        break;
                    default:
                        break;
                }
            });
        }

        static void PreviewGeometry(ApproximatedStone s, CliOptions options)
        {
            GeometryProvider.SaveStl(s.GetMesh(), CheckPath("output.stl"));
            GeometryProvider.SaveDxf(s, CheckPath("output.dxf"));
            //s.GetImage().SaveAsPng(CheckPath("output.png"));
        }

        static string CheckPath(string src)
        {
            if (Path.IsPathFullyQualified(src)) return src;
            return Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, src));
        }
    }
}
