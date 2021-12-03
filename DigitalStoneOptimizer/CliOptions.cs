using System;
using System.Collections.Generic;
using System.Text;
using CommandLine;

namespace DigitalStoneOptimizer
{
    public enum Modes
    {
        PreviewGeometry,
        GenerateMillingData,
        AssessProductionVolume
    }

    public class CliOptions
    {
        [Option('f', Required = true, HelpText = "STL model [F]ile path")]
        public string ModelFile { get; set; }

        [Option('m', Required = true, HelpText = "Application [M]ode")]
        public Modes Mode { get; set; }
        [Option('t', Required = true, HelpText = "Sheet [T]hickness")]
        public float SheetThickness { get; set; }
        [Option('o', Required = true, HelpText = "Neighbouring section [O]verlap")]
        public float DesiredOverlap { get; set; }
        [Option('n', Required = false, Default = 1, HelpText = "[N]umber of stones to mill")]
        public int NumberOfStones { get; set; }
        [Option('d', Required = false, Default = 0, HelpText = "Tool [D]iameter. Default = 0.")]
        public float ToolDiameter { get; set; }
    }
}
