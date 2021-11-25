using g3;
using MathNet.Numerics.LinearAlgebra;
using netDxf;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DigitalStoneOptimizer
{
    public static class GeometryProvider
    {
        private const float TwoEpsilon = 2 * float.Epsilon;

        public static StoneMeshData LoadFbx(string path)
        {
            DMesh3Builder builder = new DMesh3Builder();
            StandardMeshReader reader = new StandardMeshReader() { MeshBuilder = builder };
            IOReadResult result = reader.Read(path, ReadOptions.Defaults);
            if (result.code == IOCode.Ok)
            {
                var m = builder.Meshes[0];
                var s = new DMeshAABBTree3(m);
                s.Build();
                return new StoneMeshData(m, s);
            }
            return null;
        }

        public static void SaveStl(DMesh3 mesh, string path)
        {
            using StandardMeshWriter writer = new StandardMeshWriter();
            writer.Write(path, new List<WriteMesh>() { new WriteMesh(mesh) }, WriteOptions.Defaults);
        }

        public static void SaveDxf(ApproximatedStone s, string path)
        {
            var doc = new DxfDocument();
            s.DrawDxf(doc);
            doc.Save(path);
        }

        /// <summary>
        /// Move <paramref name="current"/> along the normal vector of current polygon edge.
        /// Generated normals point to the left side of the edge vector (next - current),
        /// so positive <paramref name="width"/> works for counterclockwise path traversal
        /// </summary>
        /// <param name="current"></param>
        /// <param name="next"></param>
        /// <param name="width">Distance to move</param>
        /// <returns></returns>
        public static Vector2d OffsetPointAlongNormal(Vector2d current, Vector2d next, float width)
        {
            Vector2d norm = (next - current).GetNormalVector();
            norm *= width / norm.Length;
            return current + norm;
        }

        /// <summary>
        /// Offset one point out of 3 towards the center of a circle constructed upon those points
        /// </summary>
        /// <param name="shapeCenter">Expected center of mass of the section to check the offset direction against</param>
        /// <param name="prev"></param>
        /// <param name="current">The point to offset (presumably the middle one)</param>
        /// <param name="next"></param>
        /// <param name="width"></param>
        /// <returns></returns>
        public static Vector2d OffsetPointToCenter(Vector2d prev, Vector2d current, Vector2d next, float width)
        {
            //https://math.stackexchange.com/questions/213658/get-the-equation-of-a-circle-when-given-3-points
            var arr = new Vector2d[] { prev, current, next };
            var m11 = CreateMatrix.Dense<double>(3, 3);
            var m12 = CreateMatrix.Dense<double>(3, 3);
            var m13 = CreateMatrix.Dense<double>(3, 3);
            for (int i = 0; i < 3; i++)
            {
                m11[i, 0] = arr[i].x;
                m11[i, 1] = arr[i].y;
                m11[i, 2] = 1;
                m12[i, 0] = m11[i, 0] * m11[i, 0] + m11[i, 1] * m11[i, 1];
                m12[i, 1] = m11[i, 1];
                m12[i, 2] = 1;
                m13[i, 0] = m12[i, 0];
                m13[i, 1] = m11[i, 0];
                m13[i, 2] = 1;
            }
            double det11x2 = m11.Determinant() * 2;
            Vector2d displacement;
            if (Math.Abs(det11x2) <= TwoEpsilon)
            {
                //Points lie on a straight line
                displacement = (next - prev).GetNormalVector();
            }
            else
            {
                //Points form a triangle
                double x0 = m12.Determinant() / det11x2;
                double y0 = m13.Determinant() / det11x2;
                displacement = current - new Vector2d(x0, y0);
            }
            displacement *= width / displacement.Length;
            //We already rely (during raytracing section calculation) on points (0,0,z) being inside our mesh
            //Therefore we can use current radius-vector to verify direction of the offset to handle linear points and concave shapes
            return Math.Sign(displacement.Dot(current)) != MathF.Sign(width) ? 
                (current + displacement) : (current - displacement);
        }

        public static void MeshCap(List<Index3i> t, int index, int count)
        {
            count += index;
            for (int i = index + 1; i < count; i++)
            {
                t.Add(new Index3i(index, i - 1, i)); //Simple fan triangulation
            }
        }

        public static void MeshJoint(List<Index3i> t, int index, int count)
        {
            for (int i = 1; i < count; i++)
            {
                t.Add(new Index3i(i - 1 + index, i + index, i - 1 - count + index));
                t.Add(new Index3i(i - 1 - count + index, i + index, i - count + index));
            }
            //Close this surface
            t.Add(new Index3i(index + count - 1, index, index - 1));
            t.Add(new Index3i(index - 1, index, index - count));
        }

        #region Extensions

        public static PointF ToPointF(this Vector2f v) => new PointF(v.x, v.y);
        public static PointF ToPointF(this Vector2d v) => new PointF((float)v.x, (float)v.y);
        public static ComplexPolygon ToComplexPolygon(this GeneralPolygon2d p)
        {
            var pb = new PathBuilder();
            pb.AddLines(p.Outer.Vertices.Select(x => x.ToPointF()));
            var outer = pb.Build();
            if (p.Holes.Any())
            {
                pb.Reset();
                pb.AddLines(p.Holes[0].Vertices.Select(x => x.ToPointF()));
                return new ComplexPolygon(outer, pb.Build());
            }
            else
            {
                return new ComplexPolygon(outer);
            }
        }
        public static RectangleF ToRectangleF(this AxisAlignedBox2d b) 
            => new RectangleF(b.Min.ToPointF(), new SizeF((float)b.Width, (float)b.Height));
        public static int DivideCircleInSectors(float sectorAngle) => (int)MathF.Floor(360 / sectorAngle);
        public static void CopyWithElevation(this System.Collections.ObjectModel.ReadOnlyCollection<Vector2d> arr, 
            Vector3d[] dest, int index, float elev)
        {
            foreach (var item in arr)
            {
                dest[index++] = new Vector3d(item.x, item.y, elev);
            }
        }
        public static IEnumerable<Vector3> ToDxfVectorsWithElevation(this IEnumerable<Vector2d> arr, float elevation)
            => arr.Select(x => new Vector3(x.x, x.y, elevation));
        public static IEnumerable<Vector3> ToDxfVectorsWithOffsetAndElevation(this IEnumerable<Vector2d> arr, float elevation, float xOffset)
            => arr.Select(x => new Vector3(x.x + xOffset, x.y, elevation));
        /// <summary>
        /// Rotates current vector 90deg counterclockwise to create a normal vector (non-normalized)
        /// </summary>
        /// <param name="v"></param>
        /// <returns></returns>
        public static Vector2d GetNormalVector(this Vector2d v) => new Vector2d(-v.y, v.x);
        public static Vector3f Abs(this Vector3f v) => new Vector3f(MathF.Abs(v.x), MathF.Abs(v.y), MathF.Abs(v.z));
        public static Polygon2d FixedAngularStepUnion(this Polygon2d first, Polygon2d second)
        {
            //Since ray configuration is constant, we can compare points at equal indexes
            Polygon2d union = new Polygon2d();
            for (int j = 0; j < first.VertexCount; j++)
            {
                union.AppendVertex(second[j].LengthSquared > first[j].LengthSquared ? second[j] : first[j]);
            }
            return union;
        }
        public static Polygon2d FixedAngularStepIntersection(this Polygon2d first, Polygon2d second)
        {
            Polygon2d intersection = new Polygon2d();
            for (int i = 0; i < first.VertexCount; i++)
            {
                intersection.AppendVertex(second[i].LengthSquared < first[i].LengthSquared ? second[i] : first[i]);
            }
            return intersection;
        }

        #endregion
    }
}
