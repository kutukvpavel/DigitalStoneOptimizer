using g3;
using netDxf.Entities;
using netDxf.Objects;
using System;
using System.Collections.Generic;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.PixelFormats;
using System.Linq;

namespace DigitalStoneOptimizer
{
    public class ApproximatedStone
    {
        public static float RayAngleStep { get; set; } = 1; //deg

        public ApproximatedStone(StoneMeshData data, float step, float desiredOverlap) // strip width calculation - ????
        {
            double fraction = data.Mesh.GetBounds().Height / step;
            int numberOfSlices = (int)Math.Ceiling(fraction);
            Sections = new StoneSection[numberOfSlices];
            double startFrom = data.Mesh.CachedBounds.Min.z
                + (data.Mesh.CachedBounds.Height - Math.Floor(fraction) * step) / 2;
            //First and last slices have no pair and have to be calculated separately
            Polygon2d lastPoints = data.GetSectionPoints((float)startFrom, RayAngleStep);
            Polygon2d lastUnion = lastPoints;
            Sections[0] = new StoneSection(lastPoints, step, (float)startFrom - step); //First and last sections don't have a hole
            for (int i = 1; i < numberOfSlices; i++)
            {
                float currentLevel = (float)startFrom + step * i;
                //Build each sheet from 2 adjacent sections taking their union
                //Growth direction doesn't matter this way
                var currentPoints = data.GetSectionPoints(currentLevel, RayAngleStep);
                //Since ray configuration is constant, we can compare points at equal indexes
                Polygon2d union = new Polygon2d();
                for (int j = 0; j < currentPoints.VertexCount; j++)
                {
                    union.AppendVertex(lastPoints[j].LengthSquared > currentPoints[j].LengthSquared ?
                        lastPoints[j] : currentPoints[j]);
                }
                Sections[i] = new StoneSection(union, lastUnion, desiredOverlap, step, currentLevel);
                lastPoints = currentPoints;
                lastUnion = union;
            }
            Sections[^1] = new StoneSection(lastPoints, step, (float)startFrom + step * numberOfSlices);
        }

        public StoneSection[] Sections { get; }
        public float Elevation { get; set; }

        public PositionedStoneSection GetPositionedStoneSection(int i)
        {
            return new PositionedStoneSection(Sections[i], Sections[i].Elevation + Elevation);
        }

        public DMesh3 GetMesh()
        {
            int sectors = GeometryProvider.DivideCircleInSectors(RayAngleStep);
            Vector3d[] vertices = new Vector3d[Sections.Length * sectors * 2];
            List<Triangle3d> triangles = new List<Triangle3d>(vertices.Length * 2);
            int currentIndex = 0;

            //Bottom cap
            Sections[0].Poly.Outer.Vertices.CopyWithElevation(vertices, currentIndex, Sections[0].Elevation);
            GeometryProvider.MeshCap(vertices, triangles, currentIndex, sectors);
            currentIndex += sectors;
            Sections[0].Poly.Outer.Vertices.CopyWithElevation(vertices, currentIndex, Sections[0].Top);
            GeometryProvider.MeshJoint(vertices, triangles, currentIndex, sectors);
            currentIndex += sectors;
            //Inner layers
            for (int i = 1; i < Sections.Length; i++)
            {
                Sections[i].Poly.Outer.Vertices.CopyWithElevation(vertices, currentIndex, Sections[i].Elevation);
                GeometryProvider.MeshJoint(vertices, triangles, currentIndex, sectors);
                currentIndex += sectors;
                Sections[i].Poly.Outer.Vertices.CopyWithElevation(vertices, currentIndex, Sections[i].Top);
                GeometryProvider.MeshJoint(vertices, triangles, currentIndex, sectors);
                currentIndex += sectors;
            }
            //Top cap
            Sections[^1].Poly.Outer.Vertices.CopyWithElevation(vertices, currentIndex, Sections[^1].Top);
            GeometryProvider.MeshCap(vertices, triangles, currentIndex, sectors);

            return DMesh3Builder.Build<Vector3d, Triangle3d, Vector3d>(vertices, triangles);
        }

        public Group[] GetDxfGroups()
        {
            var res = new Group[Sections.Length];
            for (int i = 0; i < res.Length; i++)
            {
                var s = GetPositionedStoneSection(i);
                res[i] = new Group();
                res[i].Entities.Add(new Polyline(s.Model.Poly.Outer.Vertices.ToDxfVectorsWithElevation(s.Elevation)));
                res[i].Entities.Add(new Polyline(s.Model.Poly.Outer.Vertices.ToDxfVectorsWithElevation(s.Top)));
            }
            return res;
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
