using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using FunctionClassroom;

internal static class RenderingTests
{
    [STAThread]
    private static int Main()
    {
        string path = Path.Combine(
            Path.GetTempPath(),
            "function-classroom-render-" + Guid.NewGuid().ToString("N") + ".png");
        try
        {
            GraphView graph = new GraphView();
            graph.Width = 800.0;
            graph.Height = 600.0;
            graph.Measure(new Size(800.0, 600.0));
            graph.Arrange(new Rect(0.0, 0.0, 800.0, 600.0));
            graph.UpdateLayout();

            FunctionPlotItem function = new FunctionPlotItem();
            function.Id = 1;
            function.Name = "f1";
            function.ExpressionText = "x^2";
            function.Color = Colors.SteelBlue;
            function.IsVisible = true;
            function.Expression = ExpressionParser.Parse("x^2");
            graph.SetFunctions(new[] { function });

            ImageExporter.SavePng(path, graph);
            FileInfo file = new FileInfo(path);
            if (!file.Exists || file.Length < 1000)
                throw new Exception("PNG export did not produce a valid-sized file.");

            Console.WriteLine("PASS graph rendering and PNG export");
            return 0;
        }
        catch (Exception error)
        {
            Console.WriteLine("FAIL graph rendering and PNG export: " + error.Message);
            return 1;
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}
