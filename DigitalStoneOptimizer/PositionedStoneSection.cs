using netDxf;
using netDxf.Entities;
using netDxf.Objects;
using netDxf.Tables;
using System.Linq;

namespace DigitalStoneOptimizer
{
    public struct PositionedStoneSection
    {
        public PositionedStoneSection(StoneSection model, float elevation)
        {
            Model = model;
            Elevation = elevation;
        }

        public StoneSection Model { get; }
        public float Elevation { get; }
        public float Top { get => Elevation + Model.Thickness; }

        public void DrawDxf(DxfDocument doc, Layer outerLayer, Layer innerLayer, float extraElevation = 0, float xOffset = 0)
        {
            float el = Elevation + extraElevation;
            float top = Top + extraElevation;
            //Outer
            var g = new Group($"{el:F0} from {Model.Elevation:F0} and {extraElevation:F0}");
            g.Entities.Add(new Polyline(
                Model.Poly.Outer.Vertices.ToDxfVectorsWithOffsetAndElevation(el, xOffset), true) { Layer = outerLayer });
            g.Entities.Add(new Polyline(
                Model.Poly.Outer.Vertices.ToDxfVectorsWithOffsetAndElevation(top, xOffset), true) { Layer = outerLayer });
            //Inner
            if (Model.Poly.Holes.Any())
            {
                g.Entities.Add(new Polyline(
                    Model.Poly.Holes[0].Vertices.ToDxfVectorsWithOffsetAndElevation(el, xOffset), true) { Layer = innerLayer });
                g.Entities.Add(new Polyline(
                    Model.Poly.Holes[0].Vertices.ToDxfVectorsWithOffsetAndElevation(top, xOffset), true) { Layer = innerLayer });
            }
            doc.Groups.Add(g);
        }
    }
}
