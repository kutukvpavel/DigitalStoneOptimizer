using System;
using MathNet.Numerics.LinearAlgebra;
using System.Collections.Generic;
using System.Text;
using g3;
using SixLabors.ImageSharp;

namespace DigitalStoneOptimizer
{
    public static class GeometryProvider
    {
        private const float TwoEpsilon = 2 * float.Epsilon;

        public static float RayAngleStep { get; set; } = 1; //deg

        public static StoneMeshData Load(string path)
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

        /// <summary>
        /// Get section points by tracing 360/AngleStep polar rays from (0,0,<paramref name="elevation"/>)
        /// </summary>
        /// <param name="mesh"></param>
        /// <param name="spatial"></param>
        /// <param name="elevation">z coordinate</param>
        /// <returns></returns>
        public static Vector2f[] GetSectionPoints(StoneMeshData data, float elevation)
        {
            var pts = new Vector2f[(int)MathF.Floor(360 / RayAngleStep)];
            var origin = new Vector3f(0, 0, elevation);
            var rotate = new TransformSequence();
            rotate.AppendRotation(new Quaternionf(origin, RayAngleStep));
            var direction = Vector3d.AxisX;
            for (int i = 0; i < pts.Length; i++)
            {
                var ray = new Ray3d(origin, direction);
                var res = data.Spatial.FindNearestHitTriangle(ray);
                if (res != DMesh3.InvalidID)
                {
                    var intersection = MeshQueries.TriangleIntersection(data.Mesh, res, ray);
                    pts[i] = (Vector2f)ray.PointAt(intersection.RayParameter).xy;
                }
                else
                {
                    pts[i] = Vector2f.Zero;
                }
                direction = rotate.TransformV(direction);
            }
            return pts;
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
        public static Vector2f OffsetPointToCenter(Vector2f prev, Vector2f current, Vector2f next, float width)
        {
            //https://math.stackexchange.com/questions/213658/get-the-equation-of-a-circle-when-given-3-points
            var arr = new PointF[] { prev.ToPointF(), current.ToPointF(), next.ToPointF() };
            var m11 = CreateMatrix.Dense<float>(3, 3);
            var m12 = CreateMatrix.Dense<float>(3, 3);
            var m13 = CreateMatrix.Dense<float>(3, 3);
            for (int i = 0; i < 3; i++)
            {
                m11[i, 0] = arr[i].X;
                m11[i, 1] = arr[i].Y;
                m11[i, 2] = 1;
                m12[i, 0] = m11[i, 0] * m11[i, 0] + m11[i, 1] * m11[i, 1];
                m12[i, 1] = m11[i, 1];
                m12[i, 2] = 1;
                m13[i, 0] = m12[i, 0];
                m13[i, 1] = m11[i, 0];
                m13[i, 2] = 1;
            }
            float det11x2 = m11.Determinant() * 2;
            Vector2f displacement;
            Vector2f currentVector = new Vector2f(current.x, current.y);
            if (MathF.Abs(det11x2) < TwoEpsilon)
            {
                //Points lie on a straight line
                displacement =
                    new Vector2f(current.y - prev.y, prev.x - current.x); //original (x,y) -> normal (y,-x)
            }
            else
            {
                //Points form a triangle
                float x0 = m12.Determinant() / det11x2;
                float y0 = m13.Determinant() / det11x2;
                displacement = currentVector - new Vector2f(x0, y0);
            }
            displacement *= width / displacement.Length;
            //We already rely (during raytracing section calculation) on points (0,0,z) being inside our mesh
            //Therefore we can use current radius-vector to verify direction of the offset to handle linear points and concave shapes
            return displacement.Dot(currentVector) < 0 ? (currentVector + displacement) : (currentVector - displacement);
        }

        public static PointF ToPointF(this Vector2f v) => new PointF(v.x, v.y);
    }
}
