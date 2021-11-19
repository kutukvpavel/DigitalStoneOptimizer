using System;
using System.Collections.Generic;
using CommandLine;

namespace DigitalStoneOptimizer
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("DigitalStone toolkit started.");
            Parser.Default.ParseArguments<CliOptions>(args).WithParsed(x =>
            {
                var data = GeometryProvider.Load(x.ModelFile);
                ApproximatedStone s = new ApproximatedStone(data, x.SheetThickness);
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
            
        }
    }
}
