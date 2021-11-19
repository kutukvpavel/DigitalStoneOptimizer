using System;
using System.Collections.Generic;
using System.Text;
using g3;
using SixLabors.ImageSharp;

namespace DigitalStoneOptimizer
{
    public static class StlProvider
    {
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

        public static PointF ToPointF(this Vector2f v) => new PointF(v.x, v.y);
    }
}
