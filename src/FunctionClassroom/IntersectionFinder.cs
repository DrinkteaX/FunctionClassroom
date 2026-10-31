using System;
using System.Collections.Generic;

namespace FunctionClassroom
{
    public sealed class IntersectionResult
    {
        public IntersectionResult()
        {
            Points = new List<PlotPoint>();
        }

        public List<PlotPoint> Points { get; private set; }
        public bool OverlapSuspected { get; set; }
        public bool LimitReached { get; set; }
    }

    public static class IntersectionFinder
    {
        private const int MaximumPoints = 120;

        public static IntersectionResult Find(
            Func<double, double> first,
            Func<double, double> second,
            double xMin,
            double xMax,
            int sampleCount)
        {
            if (first == null) throw new ArgumentNullException("first");
            if (second == null) throw new ArgumentNullException("second");
            if (!(xMin < xMax)) throw new ArgumentException("The horizontal range is invalid.");

            int count = Math.Max(32, Math.Min(5000, sampleCount));
            double[] xs = new double[count];
            double[] differences = new double[count];
            bool[] valid = new bool[count];
            double maximumScale = 1.0;
            int validCount = 0;
            int nearZeroCount = 0;

            for (int i = 0; i < count; i++)
            {
                double x = xMin + (xMax - xMin) * i / (count - 1);
                double a = SafeEvaluate(first, x);
                double b = SafeEvaluate(second, x);
                xs[i] = x;
                if (IsFinite(a) && IsFinite(b))
                {
                    valid[i] = true;
                    differences[i] = a - b;
                    validCount++;
                    maximumScale = Math.Max(maximumScale, Math.Max(Math.Abs(a), Math.Abs(b)));
                    if (IsNearZero(differences[i], maximumScale))
                        nearZeroCount++;
                }
            }

            IntersectionResult result = new IntersectionResult();
            double relativeTolerance = 1e-8 * maximumScale;
            if (validCount > 20 && nearZeroCount > validCount * 0.9)
            {
                result.OverlapSuspected = true;
                return result;
            }

            for (int i = 1; i < count; i++)
            {
                if (!valid[i - 1] || !valid[i])
                    continue;

                double leftDifference = differences[i - 1];
                double rightDifference = differences[i];
                if (Math.Abs(leftDifference) <= relativeTolerance)
                {
                    AddPoint(result, first, second, xs[i - 1], xs, count);
                    continue;
                }
                if (Math.Abs(rightDifference) <= relativeTolerance)
                {
                    AddPoint(result, first, second, xs[i], xs, count);
                    continue;
                }
                if (leftDifference * rightDifference > 0.0)
                    continue;

                double root = RefineBracket(
                    first, second, xs[i - 1], xs[i], leftDifference, relativeTolerance);
                if (!double.IsNaN(root))
                    AddPoint(result, first, second, root, xs, count);
            }

            for (int i = 1; i < count - 1; i++)
            {
                if (!valid[i - 1] || !valid[i] || !valid[i + 1])
                    continue;

                double magnitude = Math.Abs(differences[i]);
                if (magnitude <= Math.Abs(differences[i - 1]) &&
                    magnitude <= Math.Abs(differences[i + 1]) &&
                    magnitude > relativeTolerance)
                {
                    double tangent = RefineMinimum(
                        first, second, xs[i - 1], xs[i + 1]);
                    if (!double.IsNaN(tangent))
                    {
                        double a = SafeEvaluate(first, tangent);
                        double b = SafeEvaluate(second, tangent);
                        double tolerance = 1e-6 * Math.Max(1.0, Math.Max(Math.Abs(a), Math.Abs(b)));
                        if (IsFinite(a) && IsFinite(b) && Math.Abs(a - b) <= tolerance)
                            AddPoint(result, first, second, tangent, xs, count);
                    }
                }
            }

            result.Points.Sort(delegate(PlotPoint left, PlotPoint right)
            {
                return left.X.CompareTo(right.X);
            });

            List<PlotPoint> deduplicated = new List<PlotPoint>();
            double xTolerance = (xMax - xMin) / Math.Max(1, count - 1) * 1.5;
            for (int i = 0; i < result.Points.Count; i++)
            {
                PlotPoint point = result.Points[i];
                if (deduplicated.Count == 0 ||
                    Math.Abs(point.X - deduplicated[deduplicated.Count - 1].X) > xTolerance)
                    deduplicated.Add(point);
            }

            if (deduplicated.Count > MaximumPoints)
            {
                deduplicated.RemoveRange(MaximumPoints, deduplicated.Count - MaximumPoints);
                result.LimitReached = true;
            }
            result.Points.Clear();
            result.Points.AddRange(deduplicated);
            return result;
        }

        private static void AddPoint(
            IntersectionResult result,
            Func<double, double> first,
            Func<double, double> second,
            double x,
            double[] xs,
            int count)
        {
            double firstValue = SafeEvaluate(first, x);
            double secondValue = SafeEvaluate(second, x);
            if (!IsFinite(firstValue) || !IsFinite(secondValue))
                return;
            if (firstValue * secondValue < 0.0 && Math.Abs(firstValue) > 1e10)
                return;

            double xTolerance = (xs[count - 1] - xs[0]) / Math.Max(1, count - 1) * 1.5;
            for (int i = 0; i < result.Points.Count; i++)
            {
                if (Math.Abs(result.Points[i].X - x) <= xTolerance)
                    return;
            }
            result.Points.Add(new PlotPoint(x, (firstValue + secondValue) * 0.5));
        }

        private static double RefineBracket(
            Func<double, double> first,
            Func<double, double> second,
            double left,
            double right,
            double leftDifference,
            double tolerance)
        {
            bool leftNegative = leftDifference < 0.0;
            for (int i = 0; i < 60; i++)
            {
                double middle = (left + right) * 0.5;
                double a = SafeEvaluate(first, middle);
                double b = SafeEvaluate(second, middle);
                if (!IsFinite(a) || !IsFinite(b))
                    return double.NaN;

                double difference = a - b;
                if (Math.Abs(difference) <= tolerance ||
                    Math.Abs(right - left) <= 1e-12)
                    return middle;

                bool middleNegative = difference < 0.0;
                if (middleNegative == leftNegative)
                    left = middle;
                else
                    right = middle;
            }
            return (left + right) * 0.5;
        }

        private static double RefineMinimum(
            Func<double, double> first,
            Func<double, double> second,
            double left,
            double right)
        {
            double golden = 0.6180339887498949;
            double c = right - golden * (right - left);
            double d = left + golden * (right - left);
            double cValue = DifferenceMagnitude(first, second, c);
            double dValue = DifferenceMagnitude(first, second, d);

            for (int i = 0; i < 55; i++)
            {
                if (cValue < dValue)
                {
                    right = d;
                    d = c;
                    dValue = cValue;
                    c = right - golden * (right - left);
                    cValue = DifferenceMagnitude(first, second, c);
                }
                else
                {
                    left = c;
                    c = d;
                    cValue = dValue;
                    d = left + golden * (right - left);
                    dValue = DifferenceMagnitude(first, second, d);
                }
            }

            double candidate = cValue < dValue ? c : d;
            return Double.IsNaN(cValue) || Double.IsNaN(dValue)
                ? double.NaN
                : candidate;
        }

        private static double DifferenceMagnitude(
            Func<double, double> first,
            Func<double, double> second,
            double x)
        {
            double a = SafeEvaluate(first, x);
            double b = SafeEvaluate(second, x);
            if (!IsFinite(a) || !IsFinite(b))
                return double.NaN;
            return Math.Abs(a - b);
        }

        private static double SafeEvaluate(Func<double, double> function, double x)
        {
            try
            {
                return function(x);
            }
            catch
            {
                return double.NaN;
            }
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        private static bool IsNearZero(double value, double scale)
        {
            return Math.Abs(value) <= 1e-9 * scale;
        }
    }
}
