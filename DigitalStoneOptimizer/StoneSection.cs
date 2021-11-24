using g3;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;
using System.Linq;

namespace DigitalStoneOptimizer
{
    public class StoneSection
    {
        public static float PenThickness { get; set; } = 5;

        private StoneSection(float thickness, float elevation, float widthParam)
        {
            Elevation = elevation;
            Thickness = thickness;
            Overlap = widthParam;
        }
        public StoneSection(Polygon2d currentBoundary, Polygon2d lastBoundary, float overlap, float thickness, float elevation)
            : this(thickness, elevation, overlap)
        {
            if (lastBoundary.Contains(currentBoundary))
            {
                var t = lastBoundary;
                lastBoundary = currentBoundary;
                currentBoundary = t;
            }
            Polygon2d inner = new Polygon2d();
            inner.AppendVertex(
                GeometryProvider.OffsetPointToCenter(lastBoundary.Vertices[^1], lastBoundary[0], lastBoundary[1], overlap));
            int len = lastBoundary.VertexCount - 1;
            for (int i = 1; i < len; i++)
            {
                //Build inner path maintaining strip width
                //Offset each point towards the center of an inscribed circle
                inner.AppendVertex(
                    GeometryProvider.OffsetPointToCenter(lastBoundary[i - 1], lastBoundary[i], lastBoundary[i + 1], overlap));
            }
            inner.AppendVertex( 
                GeometryProvider.OffsetPointToCenter(lastBoundary.Vertices[^2], lastBoundary.Vertices[^1], lastBoundary[0], overlap));
            Poly = new GeneralPolygon2d(currentBoundary);
            Poly.AddHole(inner, false, false);
        }
        public StoneSection(Polygon2d currentBoundary, float thickness, float elevation)
            : this(thickness, elevation, float.NaN)
        {
            Poly = new GeneralPolygon2d(currentBoundary);
        }

        public float Overlap { get; private set; }
        public GeneralPolygon2d Poly { get; private set; }
        public float Thickness { get; private set; }
        public float Elevation { get; private set; }
        public float Top { get => Elevation + Thickness; }

        public Image<Rgba32> GetImage()
        {
            var bounds = Rectangle.Ceiling(Poly.Outer.Bounds.ToRectangleF());
            var res = new Image<Rgba32>(bounds.Width, bounds.Height, Color.Transparent);
            var poly = Poly.ToComplexPolygon();
            res.Mutate(x => x.Draw(Pens.Solid(Color.Black, PenThickness), new PathCollection(poly.Paths)));
            /*res.Mutate(x =>
            {
                x.Fill(Brushes.Solid(Color.LightBlue), poly.Paths.First());
                x.Fill(Brushes.Solid(Color.WhiteSmoke), poly.Paths.ElementAt(1));
            });*/
            return res;
        }
    }
}
