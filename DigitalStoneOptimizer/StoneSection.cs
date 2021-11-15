using System;
using System.Collections.Generic;
using System.Text;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing;
using MathNet.Numerics.LinearAlgebra;

namespace DigitalStoneOptimizer
{
    public class StoneSection
    {
        public StoneSection(PointF[] points, float stripWidth)
        {
            PathBuilder pb = new PathBuilder();
            pb.SetOrigin(points[0]);
            pb.AddLines(points[1..]);
            pb.CloseFigure();
            IPath outer = pb.Build();
            pb.Reset();
            PointF last = OffsetPointToCenter(points[points.Length - 1], points[0], points[1], stripWidth);
            pb.SetOrigin(last);
            int len = points.Length - 1;
            for (int i = 1; i < len; i++)
            {
                var cur = OffsetPointToCenter(points[i - 1], points[i], points[i + 1], stripWidth);
                pb.AddLine(last, cur);
                last = cur;
            }
            pb.AddLine(last, OffsetPointToCenter(points[points.Length - 2], points[points.Length - 1], points[0], stripWidth));
            Poly = new ComplexPolygon(outer, pb.Build());
        }

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
            float x0 = m12.Determinant() / det11x2;
            float y0 = m13.Determinant() / det11x2;
            float r = (float)Math.Sqrt(current.X * current.X + current.Y * current.Y) - 
                (float)Math.Sqrt(x0 * x0 + y0 * y0);
            float ratio = 1 - width / r;
            return new PointF((current.X - x0) * ratio + x0, (current.Y - y0) * ratio + y0);
        }

        public ComplexPolygon Poly { get; private set; }
    }
}
