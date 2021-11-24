using g3;
using System;
using System.Collections.Generic;
using System.Text;

namespace DigitalStoneOptimizer
{
    public class StoneMeshData
    {
        public StoneMeshData(DMesh3 mesh, DMeshAABBTree3 spatial)
        {
            Mesh = mesh;
            Spatial = spatial;
        }

        public DMesh3 Mesh { get; }
        public DMeshAABBTree3 Spatial { get; }

        /// <summary>
        /// Get section points by tracing 360/AngleStep polar rays from (0,0,<paramref name="elevation"/>)
        /// </summary>
        /// <param name="mesh"></param>
        /// <param name="spatial"></param>
        /// <param name="elevation">z coordinate</param>
        /// <returns></returns>
        public Polygon2d GetSectionPoints(float elevation, float rayAngularStep)
        {
            var pts = new Vector2d[GeometryProvider.DivideCircleInSectors(rayAngularStep)];
            var origin = new Vector3f(0, 0, elevation);
            var rotate = new TransformSequence();
            rotate.AppendRotation(new Quaternionf(Vector3f.AxisZ, rayAngularStep));
            var direction = Vector3d.AxisX;
            for (int i = 0; i < pts.Length; i++)
            {
                var ray = new Ray3d(origin, direction);
                var res = Spatial.FindNearestHitTriangle(ray);
                if (res != DMesh3.InvalidID)
                {
                    var intersection = MeshQueries.TriangleIntersection(Mesh, res, ray);
                    pts[i] = ray.PointAt(intersection.RayParameter).xy;
                }
                else
                {
                    pts[i] = Vector2d.Zero;
                }
                direction = rotate.TransformV(direction);
            }
            return new Polygon2d(pts);
        }
    }
}
