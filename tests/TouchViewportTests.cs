using System;
using FunctionClassroom;

internal static class TouchViewportTests
{
    private static int _failed;

    private static int Main()
    {
        Run("pinch zoom keeps anchor fixed", delegate
        {
            GraphViewport viewport = new GraphViewport(-10.0, 10.0, -10.0, 10.0);
            double beforeX;
            double beforeY;
            viewport.ScreenToWorld(500.0, 300.0, 1000.0, 600.0, out beforeX, out beforeY);
            viewport.ZoomAt(2.0, 500.0, 300.0, 1000.0, 600.0);
            double afterX;
            double afterY;
            viewport.ScreenToWorld(500.0, 300.0, 1000.0, 600.0, out afterX, out afterY);
            AssertNear(beforeX, afterX);
            AssertNear(beforeY, afterY);
            AssertNear(-5.0, viewport.XMin);
            AssertNear(5.0, viewport.XMax);
            AssertNear(-5.0, viewport.YMin);
            AssertNear(5.0, viewport.YMax);
        });

        Run("one-finger pan preserves zoom", delegate
        {
            GraphViewport viewport = new GraphViewport(-10.0, 10.0, -10.0, 10.0);
            viewport.PanByScreen(100.0, 60.0, 1000.0, 600.0);
            AssertNear(-12.0, viewport.XMin);
            AssertNear(8.0, viewport.XMax);
            AssertNear(-8.0, viewport.YMin);
            AssertNear(12.0, viewport.YMax);
        });

        Console.WriteLine();
        Console.WriteLine(_failed == 0 ? "Touch viewport tests passed." : "Touch viewport tests failed.");
        return _failed == 0 ? 0 : 1;
    }

    private static void Run(string name, Action test)
    {
        try
        {
            test();
            Console.WriteLine("PASS " + name);
        }
        catch (Exception error)
        {
            _failed++;
            Console.WriteLine("FAIL " + name + ": " + error.Message);
        }
    }

    private static void AssertNear(double expected, double actual)
    {
        if (double.IsNaN(actual) || Math.Abs(expected - actual) > 1e-8)
            throw new Exception(string.Format("Expected {0}, got {1}", expected, actual));
    }
}
