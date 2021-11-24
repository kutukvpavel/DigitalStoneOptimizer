using g3;
using MathNet.Numerics.LinearAlgebra;
using netDxf;
using netDxf.Objects;
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

        public static void SaveFbx(DMesh3 mesh, string path)
        {
            using StandardMeshWriter writer = new StandardMeshWriter();
            writer.Write(path, new List<WriteMesh>() { new WriteMesh(mesh) }, WriteOptions.Defaults);
        }

        public static void SaveDxf(IEnumerable<Group> obj, string path)
        {
            var doc = new DxfDocument();
            foreach (var item in obj)
            {
                doc.Groups.Add(item);
            }
            doc.Save(path);
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
                displacement =
                    new Vector2d(current.y - prev.y, prev.x - current.x); //original (x,y) -> normal (y,-x)
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

        public static void MeshCap(Vector3d[] v, List<Triangle3d> t, int index, int count)
        {
            count += index;
            for (int i = index + 1; i < count; i++)
            {
                t.Add(new Triangle3d(v[index], v[i - 1], v[i])); //Simple fan triangulation
            }
        }

        public static void MeshJoint(Vector3d[] v, List<Triangle3d> t, int index, int count)
        {
            for (int i = 0; i < count; i++)
            {
                var r = i % 2 == 0 ?
                    new Triangle3d(v[i + index], v[i + 1 + index], v[i - count + index]) :
                    new Triangle3d(v[i + index], v[i - count + index], v[i + 1 - count + index]);
                t.Add(r);
            }
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

        #endregion
    }
}
