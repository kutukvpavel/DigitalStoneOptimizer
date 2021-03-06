using g3;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace DigitalStoneOptimizer
{
    public class StoneSection
    {
        public static float PenThickness { get; set; } = 5;

        private StoneSection(ApproximatedStone parent, float thickness, float elevation, float widthParam)
        {
            Parent = parent;
            Elevation = elevation;
            Thickness = thickness;
            Overlap = widthParam;
        }
        public StoneSection(ApproximatedStone parent, Polygon2d outerBoundary, Polygon2d innerBoundaryToInset, float overlap, float thickness, float elevation)
            : this(parent, thickness, elevation, overlap)
        {
            Polygon2d inner = new Polygon2d(innerBoundaryToInset);
            inner.PolyOffset(inner.IsClockwise ? overlap : -overlap);
            inner.RemoveSelfIntersections();
            Poly = new GeneralPolygon2d(outerBoundary);
            Poly.AddHole(inner, false, false);
        }
        public StoneSection(ApproximatedStone parent, Polygon2d currentBoundary, float thickness, float elevation)
            : this(parent, thickness, elevation, float.NaN)
        {
            Poly = new GeneralPolygon2d(currentBoundary);
        }

        public ApproximatedStone Parent { get; }
        public float Overlap { get; private set; }
        public GeneralPolygon2d Poly { get; private set; }
        public float Thickness { get; private set; }
        public float Elevation { get; private set; }
        public float Top { get => Elevation + Thickness; }
        public string Name { get => $"{Parent.OriginalData.Name} - {Elevation:F0}"; }

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
