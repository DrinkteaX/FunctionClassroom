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
