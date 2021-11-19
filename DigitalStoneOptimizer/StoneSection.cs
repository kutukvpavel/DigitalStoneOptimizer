using MathNet.Numerics.LinearAlgebra;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System;

namespace DigitalStoneOptimizer
{
    public class StoneSection
    {
        public static float PenThickness { get; set; } = 5;

        public StoneSection(PointF[] points, float stripWidth, float thickness, float elevation)
        {
            Elevation = elevation;
            Thickness = thickness;
            StripWidth = stripWidth;
            PathBuilder pb = new PathBuilder();
            pb.SetOrigin(points[0]);
            pb.AddLines(points[1..]);
            pb.CloseFigure();
            IPath outer = pb.Build();
            pb.Reset();
            PointF last = OffsetPointToCenter(points[^1], points[0], points[1], stripWidth);
            pb.SetOrigin(last);
            int len = points.Length - 1;
            for (int i = 1; i < len; i++)
            {
                //Build inner path maintaining strip width
                //Offset each point towards the center of an inscribed circle
                var cur = OffsetPointToCenter(points[i - 1], points[i], points[i + 1], stripWidth);
                pb.AddLine(last, cur);
                last = cur;
            }
            pb.AddLine(last, OffsetPointToCenter(points[^2], points[^1], points[0], stripWidth));
            var inner = pb.Build();
            Poly = new ComplexPolygon(outer, inner);
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
        private static PointF OffsetPointToCenter(PointF prev, PointF current, PointF next, float width)
        {
            //https://math.stackexchange.com/questions/213658/get-the-equation-of-a-circle-when-given-3-points
            var arr = new PointF[] { prev, current, next };
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
            g3.Vector2f displacement;
            g3.Vector2f currentVector = new g3.Vector2f(current.X, current.Y);
            if (MathF.Abs(det11x2) < float.Epsilon)
            {
                //Points lie on a straight line
                displacement = 
                    new g3.Vector2f(current.Y - prev.Y, prev.X - current.X); //original (x,y) -> normal (y,-x)
            }
            else
            {
                //Points form a triangle
                float x0 = m12.Determinant() / det11x2;
                float y0 = m13.Determinant() / det11x2;
                displacement = currentVector - new g3.Vector2f(x0, y0);
            }
            displacement *= width / displacement.Length;
            //We already rely (during raytracing section calculation) on points (0,0,z) being inside our mesh
            //Therefore we can use current radius-vector to verify direction of the offset to handle linear points and concave shapes
            currentVector = displacement.Dot(currentVector) < 0 ? (currentVector + displacement) : (currentVector - displacement);
            return new PointF(currentVector.x, currentVector.y);
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
