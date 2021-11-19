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

        public StoneSection(Vector2f[] vectors, float stripWidth, float thickness, float elevation)
        {
            Elevation = elevation;
            Thickness = thickness;
            StripWidth = stripWidth;
            var points = vectors.Select(x => x.ToPointF());
            PathBuilder pb = new PathBuilder();
            pb.SetOrigin(points.First());
            pb.AddLines(points.Skip(1));
            pb.CloseFigure();
            IPath outer = pb.Build();
            pb.Reset();
            PointF last = GeometryProvider.OffsetPointToCenter(vectors[^1], vectors[0], vectors[1], stripWidth).ToPointF();
            pb.SetOrigin(last);
            int len = vectors.Length - 1;
            for (int i = 1; i < len; i++)
            {
                //Build inner path maintaining strip width
                //Offset each point towards the center of an inscribed circle
                var cur = GeometryProvider.OffsetPointToCenter(vectors[i - 1], vectors[i], vectors[i + 1], stripWidth).ToPointF();
                pb.AddLine(last, cur);
                last = cur;
            }
            pb.AddLine(last, GeometryProvider.OffsetPointToCenter(vectors[^2], vectors[^1], vectors[0], stripWidth).ToPointF());
            var inner = pb.Build();
            Poly = new ComplexPolygon(outer, inner);
        }

        public ComplexPolygon Poly { get; private set; }
        public float StripWidth { get; private set; }
        public float Thickness { get; private set; }
        public float Elevation { get; private set; }

        public Image<Rgb24> GetImage()
        {
            var bounds = Rectangle.Ceiling(Poly.Bounds);
            var res = new Image<Rgb24>(bounds.Width, bounds.Height, Color.White);
            res.Mutate(x => x.Draw(Pens.Solid(Color.Black, PenThickness), new PathCollection(Poly.Paths)));
            return res;
        }
    }
}
