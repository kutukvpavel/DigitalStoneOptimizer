using g3;
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
            Shift = new Vector2f();
        }
        public PositionedStoneSection(StoneSection model, float elevation, Vector2f shift)
        {
            Model = model;
            Elevation = elevation;
            Shift = shift;
        }

        public StoneSection Model { get; }
        public float Elevation { get; }
        public Vector2f Shift { get; }
        public float Top { get => Elevation + Model.Thickness; }
        public string Name { get => $"{Model.Name} at {Elevation:F0}"; }

        public void DrawDxf(DxfDocument doc, Layer outerLayer, Layer innerLayer, float extraElevation = 0, float xOffset = 0,
            bool flatten = false)
        {
            float el = Elevation + extraElevation;
            float top = Top + extraElevation;
            //Outer
            var g = new Group($"{Name} + {extraElevation:F0}");
            g.Entities.Add(new Polyline(
                Model.Poly.Outer.Vertices.ToDxfVectorsWith(flatten ? 0 : el, xOffset, Shift), true) { Layer = outerLayer });
            g.Entities.Add(new Polyline(
                Model.Poly.Outer.Vertices.ToDxfVectorsWith(flatten ? Model.Thickness : top, xOffset, Shift), true) 
            { Layer = outerLayer });
            //Inner
            if (Model.Poly.Holes.Any())
            {
                g.Entities.Add(new Polyline(
                    Model.Poly.Holes[0].Vertices.ToDxfVectorsWith(flatten ? 0 : el, xOffset, Shift), true) { Layer = innerLayer });
                g.Entities.Add(new Polyline(
                    Model.Poly.Holes[0].Vertices.ToDxfVectorsWith(flatten ? Model.Thickness : top, xOffset, Shift), true) 
                { Layer = innerLayer });
            }
            doc.Groups.Add(g);
        }
    }
}
