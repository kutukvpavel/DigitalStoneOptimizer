using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalStoneOptimizer
{
    public class ApproximatedStone
    {
        public ApproximatedStone(StoneMeshData data, float step)
        {
            double fraction = data.Mesh.GetBounds().Height / step;
            int numberOfSlices = (int)Math.Ceiling(fraction);
            Sections = new StoneSection[numberOfSlices];
            double startFrom = data.Mesh.CachedBounds.Min.z
                + (data.Mesh.CachedBounds.Height - Math.Floor(fraction) * step) / 2;
            //First and last slices have no pair and have to be calculated separately
            Sections[0] = new StoneSection(StlProvider.GetSectionPoints(data, (float)startFrom), 
                , step, (float)startFrom - step);
            PointF[] lastPoints = null;
            for (int i = 1; i < numberOfSlices; i++)
            {
                //Build each sheet from 2 adjacent sections taking their union
                //Growth direction doesn't matter this way
                lastPoints = StlProvider.GetSectionPoints(data, (float)startFrom + step * i);
            }
            Sections[^1] = new StoneSection(lastPoints,
                , step, (float)startFrom + step * numberOfSlices);
        }

        public StoneSection[] Sections { get; }
        public float Elevation { get; set; }

        public PositionedStoneSection GetPositionedStoneSection(int i)
        {
            return new PositionedStoneSection(Sections[i], Sections[i].Elevation + Elevation);
        }
    }
}
