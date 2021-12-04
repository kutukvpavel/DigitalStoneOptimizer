using g3;
using netDxf;
using netDxf.Tables;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DigitalStoneOptimizer
{
    public class ApproximatedStone
    {
        public static float RayAngleStep { get; set; } = 1; //deg

        public ApproximatedStone(IEnumerable<StoneSection> ss)
        {
            Sections = ss.ToArray();
        }
        public ApproximatedStone(StoneMeshData data, float step, float desiredOverlap) // strip width calculation - ????
        {
            OriginalData = data;
            double fraction = data.Mesh.GetBounds().Diagonal.z / step; //Height is not height! Diagonal vector somwhow is composed of (len,wid,height)
            if (fraction < 2) throw new InvalidOperationException("Input stone is too thin. Check the units.");
            int numberOfSlices = (int)Math.Ceiling(fraction);
            Sections = new StoneSection[numberOfSlices];
            float currentLevel = (float)(data.Mesh.CachedBounds.Min.z
                + step * (1 - Math.Ceiling(data.Mesh.CachedBounds.Diagonal.z / step) / 2) + data.Mesh.CachedBounds.Diagonal.z / 2);
            //First and last slices have no pair and have to be calculated separately
            Sections[0] = new StoneSection(this, data.GetSectionPoints(currentLevel, RayAngleStep), step, currentLevel - step); //First and last sections don't have a hole
            Polygon2d lastSection = Sections[0].Poly.Outer;
            Polygon2d nextSection = null;
            int len = numberOfSlices - 1;
            for (int i = 1; i < len; i++)
            {
                nextSection = data.GetSectionPoints(currentLevel + step, RayAngleStep);
                var union = lastSection.FixedAngularStepUnion(nextSection);
                var intersection = lastSection.FixedAngularStepIntersection(nextSection);
                Sections[i] = new StoneSection(this, union, intersection, desiredOverlap, step, currentLevel);
                currentLevel += step;
                lastSection = nextSection;
            }
            Sections[^1] = new StoneSection(this, nextSection, step, currentLevel);
            TotalHeight = Sections.Sum(x => x.Thickness);
        }

        public StoneMeshData OriginalData { get; }
        public StoneSection[] Sections { get; }
        public float Elevation { get; set; }
        public float TotalHeight { get; }

        public PositionedStoneSection GetPositionedStoneSection(int i)
        {
            return new PositionedStoneSection(Sections[i], Sections[i].Elevation + Elevation);
        }

        public DMesh3 GetMesh()
        {
            int sectors = GeometryProvider.DivideCircleInSectors(RayAngleStep);
            Vector3d[] vertices = new Vector3d[(Sections.Length * 2 + 1) * sectors];
            List<Index3i> triangles = new List<Index3i>(vertices.Length * 2);
            int currentIndex = 0;

            //Bottom cap
            Sections[0].Poly.Outer.Vertices.CopyWithElevation(vertices, currentIndex, Sections[0].Elevation);
            GeometryProvider.MeshCap(triangles, currentIndex, sectors);
            currentIndex += sectors;
            Sections[0].Poly.Outer.Vertices.CopyWithElevation(vertices, currentIndex, Sections[0].Top);
            GeometryProvider.MeshJoint(triangles, currentIndex, sectors);
            currentIndex += sectors;
            //Inner layers
            for (int i = 1; i < Sections.Length; i++)
            {
                Sections[i].Poly.Outer.Vertices.CopyWithElevation(vertices, currentIndex, Sections[i].Elevation);
                GeometryProvider.MeshJoint(triangles, currentIndex, sectors);
                currentIndex += sectors;
                Sections[i].Poly.Outer.Vertices.CopyWithElevation(vertices, currentIndex, Sections[i].Top);
                GeometryProvider.MeshJoint(triangles, currentIndex, sectors);
                currentIndex += sectors;
            }
            //Top cap
            Sections[^1].Poly.Outer.Vertices.CopyWithElevation(vertices, currentIndex, Sections[^1].Top);
            GeometryProvider.MeshCap(triangles, currentIndex, sectors);

            return DMesh3Builder.Build<Vector3d, Index3i, Vector3d>(vertices, triangles);
        }

        public void DrawDxf(DxfDocument doc)
        {
            var outerLayer = new Layer($"{Elevation:F0} Outer") { Color = AciColor.Cyan };
            var innerLayer = new Layer($"{Elevation:F0} Inner") { Color = AciColor.Green };
            doc.Layers.Add(outerLayer);
            doc.Layers.Add(innerLayer);
            for (int i = 0; i < Sections.Length; i++)
            {
                GetPositionedStoneSection(i).DrawDxf(doc, outerLayer, innerLayer, Elevation);
            }
        }

        public Image<Rgba32> GetImage()
        {
            var mw = (int)Math.Ceiling(Sections.Max(x => x.Poly.Bounds.Width));
            var mh = (int)Math.Ceiling(Sections.Max(x => x.Poly.Bounds.Height));
            var res = new Image<Rgba32>(mw, mh);
            foreach (var item in Sections)
            {
                res.Mutate(x => x.DrawImage(item.GetImage(), 0.5f));
            }
            if (res.Width > 1800) res.Mutate(x => x.Resize(1800, res.Height * 1800 / res.Width));
            GC.Collect();
            return res;
        }
    }
}
