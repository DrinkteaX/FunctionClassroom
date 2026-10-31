using System;

namespace FunctionClassroom
{
    /// <summary>
    /// Owns only the visible world rectangle. Screen gestures update this model so
    /// drawing, tracing and intersection searches all use the same coordinates.
    /// </summary>
    public sealed class GraphViewport
    {
        private const double MinimumSpan = 1e-8;
        private const double MaximumSpan = 1e10;

        public GraphViewport(double xMin, double xMax, double yMin, double yMax)
        {
            Set(xMin, xMax, yMin, yMax);
        }

        public double XMin { get; private set; }
        public double XMax { get; private set; }
        public double YMin { get; private set; }
        public double YMax { get; private set; }

        public double XSpan
        {
            get { return XMax - XMin; }
        }

        public double YSpan
        {
            get { return YMax - YMin; }
        }

        public void Set(double xMin, double xMax, double yMin, double yMax)
        {
            if (!IsFinite(xMin) || !IsFinite(xMax) ||
                !IsFinite(yMin) || !IsFinite(yMax) ||
                xMin >= xMax || yMin >= yMax)
                throw new ArgumentException("The viewport must contain finite, increasing ranges.");

            XMin = xMin;
            XMax = xMax;
            YMin = yMin;
            YMax = yMax;
        }

        public PlotPoint WorldToScreen(double x, double y, double width, double height)
        {
            return new PlotPoint(
                (x - XMin) / XSpan * width,
                (YMax - y) / YSpan * height);
        }

        public void ScreenToWorld(
            double screenX,
            double screenY,
            double width,
            double height,
            out double x,
            out double y)
        {
            if (!(width > 0.0) || !(height > 0.0))
            {
                x = double.NaN;
                y = double.NaN;
                return;
            }

            x = XMin + screenX / width * XSpan;
            y = YMax - screenY / height * YSpan;
        }

        /// <summary>
        /// Zooms around the supplied screen point. The world coordinate below the
        /// fingers remains fixed, which prevents pinch gestures from drifting.
        /// </summary>
        public void ZoomAt(
            double factor,
            double screenX,
            double screenY,
            double width,
            double height)
        {
            if (!IsFinite(factor) || factor <= 0.0 ||
                !(width > 0.0) || !(height > 0.0))
                return;

            double anchorX;
            double anchorY;
            ScreenToWorld(screenX, screenY, width, height, out anchorX, out anchorY);

            double newXSpan = ClampSpan(XSpan / factor);
            double newYSpan = ClampSpan(YSpan / factor);
            double normalizedX = screenX / width;
            double normalizedY = screenY / height;
            double xMin = anchorX - normalizedX * newXSpan;
            double yMax = anchorY + normalizedY * newYSpan;

            Set(xMin, xMin + newXSpan, yMax - newYSpan, yMax);
        }

        /// <summary>
        /// Moves the visible range in the opposite direction of the content drag.
        /// Positive X moves the graph to the right; positive Y moves it down.
        /// </summary>
        public void PanByScreen(double deltaX, double deltaY, double width, double height)
        {
            if (!IsFinite(deltaX) || !IsFinite(deltaY) ||
                !(width > 0.0) || !(height > 0.0))
                return;

            double worldX = deltaX / width * XSpan;
            double worldY = deltaY / height * YSpan;
            Set(XMin - worldX, XMax - worldX, YMin + worldY, YMax + worldY);
        }

        private static double ClampSpan(double span)
        {
            if (span < MinimumSpan)
                return MinimumSpan;
            if (span > MaximumSpan)
                return MaximumSpan;
            return span;
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }
    }
}
