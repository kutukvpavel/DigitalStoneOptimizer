using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DigitalStoneOptimizer
{
    public class ApproximatedStone
    {
        public ApproximatedStone(StoneMeshData data, float step) // strip width calculation - ????
        {
            double fraction = data.Mesh.GetBounds().Height / step;
            int numberOfSlices = (int)Math.Ceiling(fraction);
            Sections = new StoneSection[numberOfSlices];
            double startFrom = data.Mesh.CachedBounds.Min.z
                + (data.Mesh.CachedBounds.Height - Math.Floor(fraction) * step) / 2;
            //First and last slices have no pair and have to be calculated separately
            g3.Vector2f[] lastPoints = StlProvider.GetSectionPoints(data, (float)startFrom);
            Sections[0] = new StoneSection(lastPoints, 
                , step, (float)startFrom - step);
            for (int i = 1; i < numberOfSlices; i++)
            {
                float currentLevel = (float)startFrom + step * i;
                //Build each sheet from 2 adjacent sections taking their union
                //Growth direction doesn't matter this way
                var currentPoints = StlProvider.GetSectionPoints(data, currentLevel);
                //Since ray configuration is constant, we can compare points at equal indexes
                g3.Vector2f[] union = new g3.Vector2f[currentPoints.Length];
                for (int j = 0; j < union.Length; j++)
                {
                    union[j] = lastPoints[j].LengthSquared > currentPoints[j].LengthSquared ?
                        lastPoints[j] : currentPoints[j];
                }
                lastPoints = currentPoints;
                Sections[i] = new StoneSection(union, , step, currentLevel);
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
