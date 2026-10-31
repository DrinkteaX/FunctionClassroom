using System;
using System.Collections.Generic;

namespace FunctionClassroom
{
    public struct PlotPoint
    {
        public double X;
        public double Y;

        public PlotPoint(double x, double y)
        {
            X = x;
            Y = y;
        }
    }

    public sealed class PlotSegment
    {
        public PlotSegment()
        {
            Points = new List<PlotPoint>();
        }

        public List<PlotPoint> Points { get; private set; }
    }

    public static class PlotSampler
    {
        public static IList<PlotSegment> Sample(
            FunctionExpression expression,
            double xMin,
            double xMax,
            int width,
            Func<string, double> parameterLookup)
        {
            if (expression == null)
                throw new ArgumentNullException("expression");
            if (!(xMin < xMax))
                throw new ArgumentException("The horizontal range is invalid.");

            int sampleCount = Math.Max(2, Math.Min(10000, width));
            List<PlotSegment> segments = new List<PlotSegment>();
            PlotSegment current = null;
            PlotPoint previous = new PlotPoint();
            bool hasPrevious = false;

            for (int i = 0; i < sampleCount; i++)
            {
                double x = xMin + (xMax - xMin) * i / (sampleCount - 1);
                double y = expression.Evaluate(x, parameterLookup);
                bool finite = IsFinite(y);

                if (!finite)
                {
                    FinishSegment(segments, current);
                    current = null;
                    hasPrevious = false;
                    continue;
                }

                PlotPoint point = new PlotPoint(x, y);
                if (current == null)
                {
                    current = new PlotSegment();
                    current.Points.Add(point);
                }
                else if (hasPrevious && IsLikelyDiscontinuity(expression, parameterLookup, previous, point))
                {
                    FinishSegment(segments, current);
                    current = new PlotSegment();
                    current.Points.Add(point);
                }
                else
                {
                    current.Points.Add(point);
                }

                previous = point;
                hasPrevious = true;
            }

            FinishSegment(segments, current);
            return segments;
        }

        private static bool IsLikelyDiscontinuity(
            FunctionExpression expression,
            Func<string, double> parameterLookup,
            PlotPoint left,
            PlotPoint right)
        {
            double jump = Math.Abs(right.Y - left.Y);
            double scale = Math.Max(Math.Abs(left.Y), Math.Abs(right.Y));
            if (jump > 1e12)
                return true;

            double middleX = (left.X + right.X) * 0.5;
            double middleY = expression.Evaluate(middleX, parameterLookup);
            if (!IsFinite(middleY) || Math.Abs(middleY) > 1e12)
                return true;

            if (left.Y * right.Y < 0.0 && Math.Abs(middleY) > 100.0)
                return true;

            return scale > 100.0 && Math.Abs(middleY) > scale * 6.0;
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        private static void FinishSegment(List<PlotSegment> segments, PlotSegment segment)
        {
            if (segment != null && segment.Points.Count > 1)
                segments.Add(segment);
        }
    }
}
