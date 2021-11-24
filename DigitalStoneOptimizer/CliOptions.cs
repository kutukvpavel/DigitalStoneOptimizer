using System;
using System.Collections.Generic;
using System.Text;
using CommandLine;

namespace DigitalStoneOptimizer
{
    public enum Modes
    {
        PreviewGeometry
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
    }
}
