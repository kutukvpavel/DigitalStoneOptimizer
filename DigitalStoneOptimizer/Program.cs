using CommandLine;
using netDxf;
using SixLabors.ImageSharp;
using System;

namespace DigitalStoneOptimizer
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("DigitalStone toolkit started.");
            Parser.Default.ParseArguments<CliOptions>(args).WithParsed(x =>
            {
                var data = GeometryProvider.LoadFbx(x.ModelFile);
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
            GeometryProvider.SaveFbx(s.GetMesh(), "output.fbx");
            GeometryProvider.SaveDxf(s.GetDxfGroups(), "output.dxf");
            s.GetImage().SaveAsPng("output.png");
        }
    }
}
