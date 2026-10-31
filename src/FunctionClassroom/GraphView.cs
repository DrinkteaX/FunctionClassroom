using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FunctionClassroom
{
    public sealed class GraphView : FrameworkElement
    {
        private readonly List<FunctionPlotItem> _functions = new List<FunctionPlotItem>();
        private readonly Dictionary<int, Point> _touches = new Dictionary<int, Point>();
        private readonly List<PlotPoint> _intersections = new List<PlotPoint>();
        private double _xMin = -10.0;
        private double _xMax = 10.0;
        private double _yMin = -7.0;
        private double _yMax = 7.0;
        private PlotPoint? _tracePoint;
        private int _traceFunctionId = -1;
        private int _selectedIntersection = -1;
        private Point _mouseStart;
        private bool _mouseDragging;
        private bool _mouseMoved;
        private bool _singleTouchMoved;
        private DateTime _touchStarted;
        private Point _lastSingleTouch;
        private double _lastPinchDistance;
        private Point _lastPinchCenter;

        private static readonly Brush BackgroundBrush = FrozenBrush("#F7F9FC");
        private static readonly Brush GridBrush = FrozenBrush("#DCE3EC");
        private static readonly Brush AxisBrush = FrozenBrush("#526173");
        private static readonly Brush MainTextBrush = FrozenBrush("#1D2938");
        private static readonly Brush MutedTextBrush = FrozenBrush("#687789");
        private static readonly Brush MarkerBrush = FrozenBrush("#111827");
        private static readonly Brush LabelBackgroundBrush = FrozenBrush("#F4FFFFFF");

        public GraphView()
        {
            Focusable = true;
            ClipToBounds = true;
            SnapsToDevicePixels = true;
            Stylus.SetIsPressAndHoldEnabled(this, false);
            Stylus.SetIsFlicksEnabled(this, false);
        }

        public event EventHandler ViewportChanged;

        public double XMin { get { return _xMin; } }
        public double XMax { get { return _xMax; } }
        public double YMin { get { return _yMin; } }
        public double YMax { get { return _yMax; } }

        public void SetViewport(double xMin, double xMax, double yMin, double yMax)
        {
            if (!(xMin < xMax) || !(yMin < yMax))
                return;
            _xMin = xMin;
            _xMax = xMax;
            _yMin = yMin;
            _yMax = yMax;
            RaiseViewportChanged();
            InvalidateVisual();
        }

        public void SetFunctions(IEnumerable<FunctionPlotItem> functions)
        {
            _functions.Clear();
            if (functions != null)
                _functions.AddRange(functions);
            _tracePoint = null;
            _traceFunctionId = -1;
            InvalidateVisual();
        }

        public void SetIntersections(IEnumerable<PlotPoint> points)
        {
            _intersections.Clear();
            if (points != null)
                _intersections.AddRange(points);
            _selectedIntersection = -1;
            InvalidateVisual();
        }

        public void ClearIntersections()
        {
            _intersections.Clear();
            _selectedIntersection = -1;
            InvalidateVisual();
        }

        public void ZoomIn()
        {
            ZoomAt(1.3, new Point(ActualWidth * 0.5, ActualHeight * 0.5));
        }

        public void ZoomOut()
        {
            ZoomAt(1.0 / 1.3, new Point(ActualWidth * 0.5, ActualHeight * 0.5));
        }

        public void ResetView()
        {
            SetViewport(-10.0, 10.0, -7.0, 7.0);
        }

        public void FitVisibleFunctions()
        {
            List<double> values = new List<double>();
            for (int i = 0; i < _functions.Count; i++)
            {
                FunctionPlotItem function = _functions[i];
                if (!function.IsVisible || function.Expression == null)
                    continue;
                for (int j = 0; j <= 500; j++)
                {
                    double x = _xMin + (_xMax - _xMin) * j / 500.0;
                    double y = function.Evaluate(x);
                    if (!double.IsNaN(y) && !double.IsInfinity(y) && Math.Abs(y) < 1e8)
                        values.Add(y);
                }
            }

            if (values.Count < 2)
            {
                ResetView();
                return;
            }

            values.Sort();
            int lowIndex = Math.Max(0, (int)(values.Count * 0.02));
            int highIndex = Math.Min(values.Count - 1, (int)(values.Count * 0.98));
            double low = values[lowIndex];
            double high = values[highIndex];
            double span = high - low;
            if (span < 1e-8)
            {
                double center = (high + low) * 0.5;
                low = center - 5.0;
                high = center + 5.0;
                span = 10.0;
            }
            double padding = span * 0.15;
            SetViewport(_xMin, _xMax, low - padding, high + padding);
        }

        public BitmapSource RenderBitmap(double scale)
        {
            scale = Math.Max(0.5, Math.Min(4.0, scale));
            if (ActualWidth <= 0.0 || ActualHeight <= 0.0)
                return null;

            DrawingVisual visual = new DrawingVisual();
            using (DrawingContext context = visual.RenderOpen())
            {
                context.PushTransform(new ScaleTransform(scale, scale));
                DrawScene(context, ActualWidth, ActualHeight);
                context.Pop();
            }

            RenderTargetBitmap bitmap = new RenderTargetBitmap(
                Math.Max(1, (int)Math.Ceiling(ActualWidth * scale)),
                Math.Max(1, (int)Math.Ceiling(ActualHeight * scale)),
                96.0 * scale,
                96.0 * scale,
                PixelFormats.Pbgra32);
            bitmap.Render(visual);
            bitmap.Freeze();
            return bitmap;
        }

        protected override void OnRender(DrawingContext context)
        {
            base.OnRender(context);
            DrawScene(context, ActualWidth, ActualHeight);
        }

        private void DrawScene(DrawingContext context, double width, double height)
        {
            context.DrawRectangle(BackgroundBrush, null, new Rect(0.0, 0.0, width, height));
            if (width < 20.0 || height < 20.0)
                return;

            context.PushClip(new RectangleGeometry(new Rect(0.0, 0.0, width, height)));
            DrawGrid(context, width, height);
            DrawFunctions(context);
            DrawLegend(context);
            DrawIntersections(context);
            DrawTrace(context);
            context.Pop();
        }

        private void DrawGrid(DrawingContext context, double width, double height)
        {
            double xStep = NiceStep((_xMax - _xMin) * 90.0 / Math.Max(1.0, width));
            double yStep = NiceStep((_yMax - _yMin) * 80.0 / Math.Max(1.0, height));
            Pen gridPen = new Pen(GridBrush, 1.0);
            Pen axisPen = new Pen(AxisBrush, 2.2);
            Typeface typeface = new Typeface("Microsoft YaHei UI");

            double firstX = Math.Ceiling(_xMin / xStep) * xStep;
            for (double x = firstX; x <= _xMax + xStep * 0.1; x += xStep)
            {
                double px = MapX(x, width);
                context.DrawLine(gridPen, new Point(px, 0.0), new Point(px, height));
                if (Math.Abs(x) > xStep * 0.1)
                    DrawText(context, FormatTick(x, xStep), new Point(px + 4.0, Math.Min(height - 22.0, MapY(0.0, height) + 5.0)),
                        13.0, MutedTextBrush, typeface);
            }

            double firstY = Math.Ceiling(_yMin / yStep) * yStep;
            for (double y = firstY; y <= _yMax + yStep * 0.1; y += yStep)
            {
                double py = MapY(y, height);
                context.DrawLine(gridPen, new Point(0.0, py), new Point(width, py));
                if (Math.Abs(y) > yStep * 0.1)
                    DrawText(context, FormatTick(y, yStep), new Point(Math.Min(width - 50.0, MapX(0.0, width) + 6.0), py - 18.0),
                        13.0, MutedTextBrush, typeface);
            }

            double axisY = MapY(0.0, height);
            double axisX = MapX(0.0, width);
            if (axisY >= 0.0 && axisY <= height)
                context.DrawLine(axisPen, new Point(0.0, axisY), new Point(width, axisY));
            if (axisX >= 0.0 && axisX <= width)
                context.DrawLine(axisPen, new Point(axisX, 0.0), new Point(axisX, height));
        }

        private void DrawFunctions(DrawingContext context)
        {
            int sampleWidth = (int)Math.Max(200.0, Math.Min(4200.0, ActualWidth * 1.35));
            for (int index = 0; index < _functions.Count; index++)
            {
                FunctionPlotItem function = _functions[index];
                if (!function.IsVisible || function.Expression == null)
                    continue;

                IList<PlotSegment> segments = PlotSampler.Sample(
                    function.Expression, _xMin, _xMax, sampleWidth, function.GetParameter);
                Pen pen = new Pen(new SolidColorBrush(function.Color), 3.4);
                pen.StartLineCap = PenLineCap.Round;
                pen.EndLineCap = PenLineCap.Round;
                pen.LineJoin = PenLineJoin.Round;
                pen.Freeze();

                for (int segmentIndex = 0; segmentIndex < segments.Count; segmentIndex++)
                {
                    PlotSegment segment = segments[segmentIndex];
                    if (segment.Points.Count < 2)
                        continue;
                    StreamGeometry geometry = new StreamGeometry();
                    using (StreamGeometryContext geometryContext = geometry.Open())
                    {
                        PlotPoint first = segment.Points[0];
                        geometryContext.BeginFigure(
                            new Point(SafeMapX(first.X), SafeMapY(first.Y)), false, false);
                        for (int pointIndex = 1; pointIndex < segment.Points.Count; pointIndex++)
                        {
                            PlotPoint point = segment.Points[pointIndex];
                            geometryContext.LineTo(
                                new Point(SafeMapX(point.X), SafeMapY(point.Y)), true, false);
                        }
                    }
                    geometry.Freeze();
                    context.DrawGeometry(null, pen, geometry);
                }
            }
        }

        private void DrawLegend(DrawingContext context)
        {
            List<FunctionPlotItem> visible = _functions.Where(function => function.IsVisible && function.Expression != null).ToList();
            if (visible.Count == 0)
            {
                DrawText(context, "输入函数后点击“绘制”", new Point(28.0, 26.0),
                    20.0, MutedTextBrush, new Typeface("Microsoft YaHei UI"));
                return;
            }

            double width = Math.Min(420.0, Math.Max(230.0, ActualWidth * 0.32));
            double height = 12.0 + visible.Count * 28.0;
            context.DrawRoundedRectangle(
                LabelBackgroundBrush,
                new Pen(GridBrush, 1.0),
                new Rect(16.0, 16.0, width, height),
                8.0,
                8.0);
            Typeface typeface = new Typeface("Microsoft YaHei UI");
            for (int i = 0; i < visible.Count; i++)
            {
                FunctionPlotItem function = visible[i];
                double y = 28.0 + i * 28.0;
                context.DrawLine(
                    new Pen(new SolidColorBrush(function.Color), 4.0),
                    new Point(28.0, y),
                    new Point(52.0, y));
                DrawText(context, function.Name + "(x) = " + function.ExpressionText,
                    new Point(60.0, y - 11.0), 16.0, MainTextBrush, typeface);
            }
        }

        private void DrawIntersections(DrawingContext context)
        {
            if (_intersections.Count == 0)
                return;
            Typeface typeface = new Typeface("Microsoft YaHei UI");

            for (int i = 0; i < _intersections.Count; i++)
            {
                PlotPoint point = _intersections[i];
                double x = SafeMapX(point.X);
                double y = SafeMapY(point.Y);
                if (x < -30.0 || x > ActualWidth + 30.0 || y < -30.0 || y > ActualHeight + 30.0)
                    continue;

                bool selected = i == _selectedIntersection;
                double radius = selected ? 9.0 : 7.0;
                context.DrawEllipse(
                    selected ? Brushes.White : MarkerBrush,
                    new Pen(MarkerBrush, selected ? 3.0 : 2.0),
                    new Point(x, y),
                    radius,
                    radius);

                string text = FormatPoint(point);
                FormattedText formatted = new FormattedText(
                    text,
                    CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    typeface,
                    14.0,
                    MainTextBrush);
                Rect label = new Rect(x + 11.0, y - formatted.Height - 8.0,
                    formatted.Width + 12.0, formatted.Height + 6.0);
                if (label.Right > ActualWidth - 8.0)
                    label.X = x - label.Width - 11.0;
                if (label.Top < 8.0)
                    label.Y = y + 10.0;
                context.DrawRoundedRectangle(
                    LabelBackgroundBrush,
                    new Pen(GridBrush, 1.0),
                    label,
                    5.0,
                    5.0);
                context.DrawText(formatted, new Point(label.X + 6.0, label.Y + 3.0));
            }
        }

        private void DrawTrace(DrawingContext context)
        {
            if (!_tracePoint.HasValue)
                return;

            PlotPoint point = _tracePoint.Value;
            double px = SafeMapX(point.X);
            double py = SafeMapY(point.Y);
            if (px < 0.0 || px > ActualWidth || py < 0.0 || py > ActualHeight)
                return;

            Pen crossPen = new Pen(MutedTextBrush, 1.2);
            crossPen.DashStyle = DashStyles.Dash;
            context.DrawLine(crossPen, new Point(px, 0.0), new Point(px, ActualHeight));
            context.DrawLine(crossPen, new Point(0.0, py), new Point(ActualWidth, py));
            context.DrawEllipse(Brushes.White, new Pen(MarkerBrush, 2.0), new Point(px, py), 6.0, 6.0);

            string label = FormatCoordinate(point.X) + ", " + FormatCoordinate(point.Y);
            DrawLabel(context, label, new Point(px + 12.0, py + 12.0));
        }

        private void DrawLabel(DrawingContext context, string text, Point origin)
        {
            Typeface typeface = new Typeface("Microsoft YaHei UI");
            FormattedText formatted = new FormattedText(
                text,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                typeface,
                16.0,
                MainTextBrush);
            Rect background = new Rect(origin.X, origin.Y, formatted.Width + 16.0, formatted.Height + 10.0);
            if (background.Right > ActualWidth - 8.0)
                background.X = ActualWidth - background.Width - 8.0;
            if (background.Bottom > ActualHeight - 8.0)
                background.Y = ActualHeight - background.Height - 8.0;
            context.DrawRoundedRectangle(
                LabelBackgroundBrush,
                new Pen(GridBrush, 1.0),
                background,
                6.0,
                6.0);
            context.DrawText(formatted, new Point(background.X + 8.0, background.Y + 5.0));
        }

        private void TraceAt(Point position)
        {
            double x = UnmapX(position.X);
            double bestDistance = 32.0;
            PlotPoint? bestPoint = null;
            int bestId = -1;

            for (int i = 0; i < _functions.Count; i++)
            {
                FunctionPlotItem function = _functions[i];
                if (!function.IsVisible || function.Expression == null)
                    continue;
                double y = function.Evaluate(x);
                if (double.IsNaN(y) || double.IsInfinity(y))
                    continue;
                double py = SafeMapY(y);
                double distance = Math.Abs(py - position.Y);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestPoint = new PlotPoint(x, y);
                    bestId = function.Id;
                }
            }

            _tracePoint = bestPoint;
            _traceFunctionId = bestId;
            _selectedIntersection = FindIntersectionAt(position);
            InvalidateVisual();
        }

        private int FindIntersectionAt(Point position)
        {
            for (int i = 0; i < _intersections.Count; i++)
            {
                double dx = SafeMapX(_intersections[i].X) - position.X;
                double dy = SafeMapY(_intersections[i].Y) - position.Y;
                if (dx * dx + dy * dy <= 24.0 * 24.0)
                    return i;
            }
            return -1;
        }

        private void PanPixels(double dx, double dy)
        {
            if (Math.Abs(dx) < 0.01 && Math.Abs(dy) < 0.01)
                return;
            double xPerPixel = (_xMax - _xMin) / Math.Max(1.0, ActualWidth);
            double yPerPixel = (_yMax - _yMin) / Math.Max(1.0, ActualHeight);
            double xShift = dx * xPerPixel;
            double yShift = dy * yPerPixel;
            _xMin -= xShift;
            _xMax -= xShift;
            _yMin += yShift;
            _yMax += yShift;
            RaiseViewportChanged();
            InvalidateVisual();
        }

        private void ZoomAt(double factor, Point anchor)
        {
            if (factor <= 0.0 || double.IsNaN(factor) || double.IsInfinity(factor))
                return;
            factor = Math.Max(0.2, Math.Min(5.0, factor));
            double worldX = UnmapX(anchor.X);
            double worldY = UnmapY(anchor.Y);
            _xMin = worldX - (worldX - _xMin) / factor;
            _xMax = worldX + (_xMax - worldX) / factor;
            _yMin = worldY - (worldY - _yMin) / factor;
            _yMax = worldY + (_yMax - worldY) / factor;
            RaiseViewportChanged();
            InvalidateVisual();
        }

        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            Focus();
            CaptureMouse();
            _mouseStart = e.GetPosition(this);
            _lastSingleTouch = _mouseStart;
            _mouseDragging = true;
            _mouseMoved = false;
            e.Handled = true;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (!_mouseDragging)
                return;
            Point point = e.GetPosition(this);
            double dx = point.X - _lastSingleTouch.X;
            double dy = point.Y - _lastSingleTouch.Y;
            if (Math.Abs(point.X - _mouseStart.X) + Math.Abs(point.Y - _mouseStart.Y) > 4.0)
                _mouseMoved = true;
            if (_mouseMoved)
                PanPixels(dx, dy);
            _lastSingleTouch = point;
            e.Handled = true;
        }

        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);
            if (_mouseDragging && !_mouseMoved)
                TraceAt(e.GetPosition(this));
            _mouseDragging = false;
            ReleaseMouseCapture();
            e.Handled = true;
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);
            double factor = e.Delta > 0 ? 1.16 : 1.0 / 1.16;
            ZoomAt(factor, e.GetPosition(this));
            e.Handled = true;
        }

        protected override void OnTouchDown(TouchEventArgs e)
        {
            base.OnTouchDown(e);
            Point position = e.GetTouchPoint(this).Position;
            if (_touches.Count == 0)
            {
                _touchStarted = DateTime.UtcNow;
                _lastSingleTouch = position;
                _singleTouchMoved = false;
            }
            _touches[e.TouchDevice.Id] = position;
            CaptureTouch(e.TouchDevice);
            if (_touches.Count >= 2)
                ResetPinch();
            e.Handled = true;
        }

        protected override void OnTouchMove(TouchEventArgs e)
        {
            base.OnTouchMove(e);
            if (!_touches.ContainsKey(e.TouchDevice.Id))
                return;

            Point position = e.GetTouchPoint(this).Position;
            if (_touches.Count == 1)
            {
                Point previous = _touches[e.TouchDevice.Id];
                double dx = position.X - previous.X;
                double dy = position.Y - previous.Y;
                if (Math.Abs(position.X - _lastSingleTouch.X) + Math.Abs(position.Y - _lastSingleTouch.Y) > 5.0)
                    _singleTouchMoved = true;
                PanPixels(dx, dy);
            }

            _touches[e.TouchDevice.Id] = position;
            if (_touches.Count >= 2)
                HandlePinch();
            e.Handled = true;
        }

        protected override void OnTouchUp(TouchEventArgs e)
        {
            base.OnTouchUp(e);
            if (!_touches.ContainsKey(e.TouchDevice.Id))
                return;
            bool wasSingle = _touches.Count == 1;
            Point position = e.GetTouchPoint(this).Position;
            _touches.Remove(e.TouchDevice.Id);
            ReleaseTouchCapture(e.TouchDevice);

            if (wasSingle && !_singleTouchMoved &&
                (DateTime.UtcNow - _touchStarted).TotalMilliseconds < 700.0)
                TraceAt(position);

            if (_touches.Count >= 2)
                ResetPinch();
            else
                _lastPinchDistance = 0.0;
            e.Handled = true;
        }

        private void HandlePinch()
        {
            KeyValuePair<int, Point> first = _touches.First();
            KeyValuePair<int, Point> second = _touches.Skip(1).First();
            double distance = Distance(first.Value, second.Value);
            Point center = new Point(
                (first.Value.X + second.Value.X) * 0.5,
                (first.Value.Y + second.Value.Y) * 0.5);

            if (_lastPinchDistance > 1.0)
            {
                PanPixels(center.X - _lastPinchCenter.X, center.Y - _lastPinchCenter.Y);
                ZoomAt(distance / _lastPinchDistance, center);
            }

            _lastPinchDistance = distance;
            _lastPinchCenter = center;
        }

        private void ResetPinch()
        {
            if (_touches.Count < 2)
            {
                _lastPinchDistance = 0.0;
                return;
            }
            KeyValuePair<int, Point> first = _touches.First();
            KeyValuePair<int, Point> second = _touches.Skip(1).First();
            _lastPinchDistance = Distance(first.Value, second.Value);
            _lastPinchCenter = new Point(
                (first.Value.X + second.Value.X) * 0.5,
                (first.Value.Y + second.Value.Y) * 0.5);
        }

        private void RaiseViewportChanged()
        {
            EventHandler handler = ViewportChanged;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }

        private double MapX(double x, double width)
        {
            return (x - _xMin) / (_xMax - _xMin) * width;
        }

        private double MapY(double y, double height)
        {
            return height - (y - _yMin) / (_yMax - _yMin) * height;
        }

        private double SafeMapX(double x)
        {
            return SafeScreenCoordinate(MapX(x, ActualWidth));
        }

        private double SafeMapY(double y)
        {
            return SafeScreenCoordinate(MapY(y, ActualHeight));
        }

        private double UnmapX(double px)
        {
            return _xMin + px / Math.Max(1.0, ActualWidth) * (_xMax - _xMin);
        }

        private double UnmapY(double py)
        {
            return _yMin + (ActualHeight - py) / Math.Max(1.0, ActualHeight) * (_yMax - _yMin);
        }

        private static double SafeScreenCoordinate(double value)
        {
            if (double.IsNaN(value))
                return 0.0;
            return Math.Max(-1000000.0, Math.Min(1000000.0, value));
        }

        private static double NiceStep(double roughStep)
        {
            if (!(roughStep > 0.0) || double.IsInfinity(roughStep))
                return 1.0;
            double exponent = Math.Floor(Math.Log10(roughStep));
            double magnitude = Math.Pow(10.0, exponent);
            double normalized = roughStep / magnitude;
            double nice = normalized <= 1.0 ? 1.0 :
                normalized <= 2.0 ? 2.0 :
                normalized <= 5.0 ? 5.0 : 10.0;
            return nice * magnitude;
        }

        private static string FormatTick(double value, double step)
        {
            int decimals = Math.Max(0, Math.Min(6, (int)Math.Ceiling(-Math.Log10(step)) + 1));
            if (Math.Abs(value) < step * 1e-8)
                value = 0.0;
            return value.ToString("0." + new string('#', decimals), CultureInfo.CurrentCulture);
        }

        private static string FormatCoordinate(double value)
        {
            if (Math.Abs(value) < 5e-7)
                value = 0.0;
            if (Math.Abs(value) >= 10000.0 || (Math.Abs(value) > 0.0 && Math.Abs(value) < 0.001))
                return value.ToString("0.###E+0", CultureInfo.CurrentCulture);
            return value.ToString("0.####", CultureInfo.CurrentCulture);
        }

        private static string FormatPoint(PlotPoint point)
        {
            return "(" + FormatCoordinate(point.X) + ", " + FormatCoordinate(point.Y) + ")";
        }

        private static void DrawText(
            DrawingContext context,
            string text,
            Point origin,
            double size,
            Brush brush,
            Typeface typeface)
        {
            FormattedText formatted = new FormattedText(
                text,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                typeface,
                size,
                brush);
            context.DrawText(formatted, origin);
        }

        private static double Distance(Point first, Point second)
        {
            double dx = second.X - first.X;
            double dy = second.Y - first.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        private static SolidColorBrush FrozenBrush(string color)
        {
            SolidColorBrush brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
            brush.Freeze();
            return brush;
        }
    }
}
