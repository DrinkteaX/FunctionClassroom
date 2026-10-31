namespace FunctionClassroom
{
    internal static class MathDisplay
    {
        public static string Pretty(string expression)
        {
            if (string.IsNullOrEmpty(expression))
                return string.Empty;
            return expression
                .Replace("^2", "²")
                .Replace("^3", "³")
                .Replace("*", "·")
                .Replace("pi", "π");
        }
    }
}
