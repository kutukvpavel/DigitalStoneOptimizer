using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalStoneOptimizer
{
    public struct PositionedStoneSection
    {
        public PositionedStoneSection(StoneSection model, float elevation)
        {
            Model = model;
            Elevation = elevation;
        }

        public StoneSection Model { get; }
        public float Elevation { get; }
        public float Top { get => Elevation + Model.Thickness; }
    }
}
