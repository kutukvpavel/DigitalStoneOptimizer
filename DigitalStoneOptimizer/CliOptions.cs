using CommandLine;
using System.Collections.Generic;

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
        [Option('f', Required = true, HelpText = "STL model [F]ile path(es)")]
        public IEnumerable<string> ModelFiles { get; set; }

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
        [Option("flatten", Required = false, Default = false, HelpText = "Flatten milling output drawing.")]
        public bool FlattenMillingOutput { get; set; }
    }
}
