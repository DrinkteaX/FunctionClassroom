using System;
using System.Collections.Generic;
using System.Globalization;
using FunctionClassroom;

internal static class MathCoreTests
{
    private static int _passed;
    private static int _failed;

    private static int Main()
    {
        Run("addition", delegate { AssertNear(4.0, Eval("2+2", 0.0)); });
        Run("power", delegate { AssertNear(9.0, Eval("x^2", 3.0)); });
        Run("implicit multiplication", delegate { AssertNear(7.0, Eval("2x+1", 3.0)); });
        Run("implicit parentheses", delegate { AssertNear(8.0, Eval("2(x+1)", 3.0)); });
        Run("right associative power", delegate { AssertNear(512.0, Eval("2^3^2", 0.0)); });
        Run("negative square precedence", delegate { AssertNear(-4.0, Eval("-x^2", 2.0)); });
        Run("sine and pi", delegate { AssertNear(1.0, Eval("sin(pi/2)", 0.0)); });
        Run("parameters", delegate
        {
            FunctionExpression expression = ExpressionParser.Parse("a*x+b");
            double value = expression.Evaluate(2.0, delegate(string name)
            {
                if (name == "a") return 3.0;
                if (name == "b") return 4.0;
                throw new ArgumentException("Unknown parameter");
            });
            AssertNear(10.0, value);
        });
        Run("invalid expression", delegate
        {
            AssertThrows<ExpressionException>(delegate { ExpressionParser.Parse("sin("); });
        });
        Run("sampler keeps reciprocal branches separate", delegate
        {
            FunctionExpression expression = ExpressionParser.Parse("1/x");
            IList<PlotSegment> segments = PlotSampler.Sample(
                expression, -10.0, 10.0, 1000, NoParameters);
            AssertTrue(segments.Count >= 2, "Expected at least two reciprocal branches.");
            for (int i = 0; i < segments.Count; i++)
            {
                PlotSegment segment = segments[i];
                for (int j = 1; j < segment.Points.Count; j++)
                {
                    bool crossesZero = segment.Points[j - 1].X <= 0.0 && segment.Points[j].X >= 0.0;
                    AssertTrue(!crossesZero, "A reciprocal segment crossed x=0.");
                }
            }
        });
        Run("sampler respects square root domain", delegate
        {
            FunctionExpression expression = ExpressionParser.Parse("sqrt(x)");
            IList<PlotSegment> segments = PlotSampler.Sample(
                expression, -4.0, 4.0, 400, NoParameters);
            AssertTrue(segments.Count > 0, "Expected a square-root segment.");
            for (int i = 0; i < segments.Count; i++)
            {
                for (int j = 0; j < segments[i].Points.Count; j++)
                    AssertTrue(segments[i].Points[j].X >= -1e-12, "Square root sampled outside its domain.");
            }
        });
        Run("sampler returns finite sine data", delegate
        {
            IList<PlotSegment> segments = PlotSampler.Sample(
                ExpressionParser.Parse("sin(x)"), -6.0, 6.0, 300, NoParameters);
            AssertTrue(segments.Count > 0, "Expected sine samples.");
            AssertTrue(segments[0].Points.Count > 100, "Expected dense sine samples.");
        });
        Run("intersection finds two quadratic roots", delegate
        {
            Func<double, double> parabola = Compile("x^2-4");
            Func<double, double> zero = Compile("0");
            IntersectionResult result = IntersectionFinder.Find(parabola, zero, -5.0, 5.0, 1001);
            AssertTrue(result.Points.Count == 2, "Expected two intersections.");
            AssertNear(-2.0, result.Points[0].X);
            AssertNear(2.0, result.Points[1].X);
        });
        Run("intersection finds tangent point", delegate
        {
            IntersectionResult result = IntersectionFinder.Find(
                Compile("x^2"), Compile("0"), -5.0, 5.0, 1001);
            AssertTrue(result.Points.Count == 1, "Expected one tangent intersection.");
            AssertNear(0.0, result.Points[0].X);
        });
        Run("intersection reports no crossing", delegate
        {
            IntersectionResult result = IntersectionFinder.Find(
                Compile("x^2+1"), Compile("0"), -5.0, 5.0, 1001);
            AssertTrue(result.Points.Count == 0, "Expected no intersections.");
        });
        Run("intersection suppresses duplicates", delegate
        {
            IntersectionResult result = IntersectionFinder.Find(
                Compile("x"), Compile("0"), -5.0, 5.0, 1001);
            AssertTrue(result.Points.Count == 1, "Expected one deduplicated intersection.");
        });
        Run("workspace round trip preserves functions", delegate
        {
            string path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "function-classroom-test-" + Guid.NewGuid().ToString("N") + ".xml");
            try
            {
                SavedWorkspace workspace = new SavedWorkspace();
                workspace.XMin = -8.0;
                workspace.XMax = 12.0;
                workspace.YMin = -3.0;
                workspace.YMax = 9.0;
                SavedFunction function = new SavedFunction();
                function.Id = 7;
                function.Name = "f1";
                function.Expression = "a*x^2";
                function.Color = "#1565C0";
                function.IsVisible = true;
                function.ParameterNames = new List<string> { "a" };
                function.ParameterValues = new List<double> { 1.5 };
                workspace.Functions.Add(function);

                ProjectStore.Save(path, workspace);
                SavedWorkspace loaded = ProjectStore.Load(path);
                AssertNear(-8.0, loaded.XMin);
                AssertTrue(loaded.Functions.Count == 1, "Expected one saved function.");
                AssertTrue(loaded.Functions[0].Expression == "a*x^2", "Expression changed.");
                AssertNear(1.5, loaded.Functions[0].ParameterValues[0]);
            }
            finally
            {
                if (System.IO.File.Exists(path))
                    System.IO.File.Delete(path);
            }
        });
        Run("workspace load tolerates corrupt input", delegate
        {
            string path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "function-classroom-corrupt-" + Guid.NewGuid().ToString("N") + ".xml");
            try
            {
                System.IO.File.WriteAllText(path, "not xml");
                SavedWorkspace loaded = ProjectStore.Load(path);
                AssertTrue(loaded.Functions.Count == 0, "Corrupt input should return an empty workspace.");
            }
            finally
            {
                if (System.IO.File.Exists(path))
                    System.IO.File.Delete(path);
            }
        });

        Console.WriteLine();
        Console.WriteLine("Passed: {0}, Failed: {1}", _passed, _failed);
        return _failed == 0 ? 0 : 1;
    }

    private static double Eval(string source, double x)
    {
        return ExpressionParser.Parse(source).Evaluate(x, delegate(string name)
        {
            throw new ArgumentException("Unexpected parameter: " + name);
        });
    }

    private static double NoParameters(string name)
    {
        throw new ArgumentException("Unexpected parameter: " + name);
    }

    private static Func<double, double> Compile(string source)
    {
        FunctionExpression expression = ExpressionParser.Parse(source);
        return delegate(double x) { return expression.Evaluate(x, NoParameters); };
    }

    private static void Run(string name, Action test)
    {
        try
        {
            test();
            _passed++;
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
        double tolerance = Math.Max(1e-8, Math.Abs(expected) * 1e-8);
        if (double.IsNaN(actual) || Math.Abs(expected - actual) > tolerance)
        {
            throw new Exception(string.Format(
                CultureInfo.InvariantCulture,
                "Expected {0}, got {1}",
                expected,
                actual));
        }
    }

    private static void AssertThrows<TException>(Action action) where TException : Exception
    {
        try
        {
            action();
        }
        catch (TException)
        {
            return;
        }
        throw new Exception("Expected " + typeof(TException).Name);
    }

    private static void AssertTrue(bool condition, string message)
    {
        if (!condition)
            throw new Exception(message);
    }
}
