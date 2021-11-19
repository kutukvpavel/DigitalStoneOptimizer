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
    }
}
