using System;
using System.Collections.Generic;
using System.Globalization;

namespace FunctionClassroom
{
    public sealed class ExpressionException : Exception
    {
        public int Position { get; private set; }

        public ExpressionException(string message, int position)
            : base(message)
        {
            Position = position;
        }
    }

    public static class ExpressionParser
    {
        public static FunctionExpression Parse(string source)
        {
            if (string.IsNullOrWhiteSpace(source))
                throw new ExpressionException("Please enter a function.", 0);

            string normalized = Normalize(source);
            Lexer lexer = new Lexer(normalized);
            Parser parser = new Parser(lexer);
            Node root = parser.ParseExpression();
            parser.ExpectEnd();
            return new FunctionExpression(root, parser.Parameters);
        }

        private static string Normalize(string source)
        {
            string text = source.Trim();
            text = text.Replace(" ", string.Empty);
            text = text.Replace("×", "*").Replace("÷", "/").Replace("−", "-");
            text = text.Replace("π", "pi");
            text = text.Replace("²", "^2").Replace("³", "^3");
            if (text.StartsWith("y=", StringComparison.OrdinalIgnoreCase))
                text = text.Substring(2);
            return text;
        }

        private enum TokenKind
        {
            Number,
            Identifier,
            Plus,
            Minus,
            Multiply,
            Divide,
            Power,
            LeftParen,
            RightParen,
            Comma,
            End
        }

        private sealed class Token
        {
            public TokenKind Kind;
            public string Text;
            public double Number;
            public int Position;
        }

        private sealed class Lexer
        {
            private readonly string _text;
            private int _index;

            public Lexer(string text)
            {
                _text = text;
            }

            public Token Next()
            {
                while (_index < _text.Length && char.IsWhiteSpace(_text[_index]))
                    _index++;

                if (_index >= _text.Length)
                    return new Token { Kind = TokenKind.End, Position = _index, Text = string.Empty };

                int start = _index;
                char current = _text[_index];
                if (char.IsDigit(current) ||
                    (current == '.' && _index + 1 < _text.Length && char.IsDigit(_text[_index + 1])))
                    return ReadNumber();
                if (char.IsLetter(current) || current == '_')
                    return ReadIdentifier();

                _index++;
                TokenKind kind;
                switch (current)
                {
                    case '+': kind = TokenKind.Plus; break;
                    case '-': kind = TokenKind.Minus; break;
                    case '*': kind = TokenKind.Multiply; break;
                    case '/': kind = TokenKind.Divide; break;
                    case '^': kind = TokenKind.Power; break;
                    case '(': kind = TokenKind.LeftParen; break;
                    case ')': kind = TokenKind.RightParen; break;
                    case ',': kind = TokenKind.Comma; break;
                    default:
                        throw new ExpressionException("Unexpected character: " + current, start);
                }

                return new Token { Kind = kind, Position = start, Text = current.ToString() };
            }

            private Token ReadNumber()
            {
                int start = _index;
                while (_index < _text.Length &&
                       (char.IsDigit(_text[_index]) || _text[_index] == '.'))
                    _index++;

                if (_index < _text.Length && (_text[_index] == 'e' || _text[_index] == 'E'))
                {
                    int exponentStart = _index;
                    _index++;
                    if (_index < _text.Length && (_text[_index] == '+' || _text[_index] == '-'))
                        _index++;
                    int digitsStart = _index;
                    while (_index < _text.Length && char.IsDigit(_text[_index]))
                        _index++;
                    if (digitsStart == _index)
                        _index = exponentStart;
                }

                string text = _text.Substring(start, _index - start);
                double value;
                if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
                    throw new ExpressionException("Invalid number: " + text, start);
                return new Token { Kind = TokenKind.Number, Number = value, Text = text, Position = start };
            }

            private Token ReadIdentifier()
            {
                int start = _index;
                while (_index < _text.Length &&
                       (char.IsLetterOrDigit(_text[_index]) || _text[_index] == '_'))
                    _index++;
                string text = _text.Substring(start, _index - start);
                return new Token { Kind = TokenKind.Identifier, Text = text, Position = start };
            }
        }

        private sealed class Parser
        {
            private readonly Lexer _lexer;
            private Token _current;

            public readonly ISet<string> Parameters = new HashSet<string>(StringComparer.Ordinal);

            public Parser(Lexer lexer)
            {
                _lexer = lexer;
                _current = _lexer.Next();
            }

            public Node ParseExpression()
            {
                return ParseAdditive();
            }

            public void ExpectEnd()
            {
                if (_current.Kind != TokenKind.End)
                    throw new ExpressionException("Unexpected token: " + _current.Text, _current.Position);
            }

            private Node ParseAdditive()
            {
                Node left = ParseMultiplicative();
                while (_current.Kind == TokenKind.Plus || _current.Kind == TokenKind.Minus)
                {
                    TokenKind operation = _current.Kind;
                    Advance();
                    Node right = ParseMultiplicative();
                    left = new BinaryNode(
                        operation == TokenKind.Plus ? BinaryOperation.Add : BinaryOperation.Subtract,
                        left,
                        right);
                }
                return left;
            }

            private Node ParseMultiplicative()
            {
                Node left = ParseUnary();
                while (true)
                {
                    BinaryOperation operation;
                    if (_current.Kind == TokenKind.Multiply)
                    {
                        operation = BinaryOperation.Multiply;
                        Advance();
                    }
                    else if (_current.Kind == TokenKind.Divide)
                    {
                        operation = BinaryOperation.Divide;
                        Advance();
                    }
                    else if (_current.Kind == TokenKind.Identifier || _current.Kind == TokenKind.LeftParen)
                    {
                        operation = BinaryOperation.Multiply;
                    }
                    else
                    {
                        break;
                    }

                    Node right = ParseUnary();
                    left = new BinaryNode(operation, left, right);
                }
                return left;
            }

            private Node ParseUnary()
            {
                if (_current.Kind == TokenKind.Plus)
                {
                    Advance();
                    return ParseUnary();
                }
                if (_current.Kind == TokenKind.Minus)
                {
                    Advance();
                    return new UnaryNode(ParseUnary());
                }
                return ParsePower();
            }

            private Node ParsePower()
            {
                Node left = ParsePrimary();
                if (_current.Kind == TokenKind.Power)
                {
                    Advance();
                    return new BinaryNode(BinaryOperation.Power, left, ParseUnary());
                }
                return left;
            }

            private Node ParsePrimary()
            {
                if (_current.Kind == TokenKind.Number)
                {
                    double value = _current.Number;
                    Advance();
                    return new NumberNode(value);
                }

                if (_current.Kind == TokenKind.Identifier)
                {
                    string name = _current.Text;
                    int position = _current.Position;
                    Advance();

                    if (string.Equals(name, "pi", StringComparison.OrdinalIgnoreCase))
                        return new NumberNode(Math.PI);
                    if (string.Equals(name, "e", StringComparison.OrdinalIgnoreCase))
                        return new NumberNode(Math.E);
                    if (name == "x")
                        return new VariableNode();

                    if (IsFunction(name))
                    {
                        if (_current.Kind != TokenKind.LeftParen)
                            throw new ExpressionException("Expected '(' after " + name, _current.Position);
                        Advance();
                        List<Node> arguments = new List<Node>();
                        if (_current.Kind != TokenKind.RightParen)
                        {
                            arguments.Add(ParseExpression());
                            while (_current.Kind == TokenKind.Comma)
                            {
                                Advance();
                                arguments.Add(ParseExpression());
                            }
                        }
                        if (_current.Kind != TokenKind.RightParen)
                            throw new ExpressionException("Expected ')' after function arguments", _current.Position);
                        Advance();
                        ValidateArgumentCount(name, arguments.Count, position);
                        return new FunctionNode(name.ToLowerInvariant(), arguments);
                    }

                    if (name.Length == 1 && char.IsLetter(name[0]) && name[0] >= 'a' && name[0] <= 'k')
                    {
                        Parameters.Add(name);
                        return new ParameterNode(name);
                    }

                    throw new ExpressionException("Unknown name: " + name, position);
                }

                if (_current.Kind == TokenKind.LeftParen)
                {
                    Advance();
                    Node inner = ParseExpression();
                    if (_current.Kind != TokenKind.RightParen)
                        throw new ExpressionException("Expected ')'", _current.Position);
                    Advance();
                    return inner;
                }

                throw new ExpressionException("Expected a number, variable or '('", _current.Position);
            }

            private void ValidateArgumentCount(string name, int count, int position)
            {
                string lower = name.ToLowerInvariant();
                if (lower == "log")
                {
                    if (count != 1 && count != 2)
                        throw new ExpressionException("log expects 1 or 2 arguments", position);
                    return;
                }
                if (count != 1)
                    throw new ExpressionException(name + " expects 1 argument", position);
            }

            private static bool IsFunction(string name)
            {
                switch (name.ToLowerInvariant())
                {
                    case "sin":
                    case "cos":
                    case "tan":
                    case "asin":
                    case "acos":
                    case "atan":
                    case "sqrt":
                    case "abs":
                    case "ln":
                    case "log":
                    case "log10":
                    case "lg":
                    case "exp":
                    case "floor":
                    case "ceil":
                        return true;
                    default:
                        return false;
                }
            }

            private void Advance()
            {
                _current = _lexer.Next();
            }
        }
    }

    internal enum BinaryOperation
    {
        Add,
        Subtract,
        Multiply,
        Divide,
        Power
    }

    internal abstract class Node
    {
        public abstract double Evaluate(double x, Func<string, double> parameterLookup);
    }

    internal sealed class NumberNode : Node
    {
        private readonly double _value;
        public NumberNode(double value) { _value = value; }
        public override double Evaluate(double x, Func<string, double> parameterLookup) { return _value; }
    }

    internal sealed class VariableNode : Node
    {
        public override double Evaluate(double x, Func<string, double> parameterLookup) { return x; }
    }

    internal sealed class ParameterNode : Node
    {
        private readonly string _name;
        public ParameterNode(string name) { _name = name; }
        public override double Evaluate(double x, Func<string, double> parameterLookup)
        {
            if (parameterLookup == null)
                throw new EvaluationException("Parameter is not defined: " + _name);
            return parameterLookup(_name);
        }
    }

    internal sealed class UnaryNode : Node
    {
        private readonly Node _inner;
        public UnaryNode(Node inner) { _inner = inner; }
        public override double Evaluate(double x, Func<string, double> parameterLookup)
        {
            return -_inner.Evaluate(x, parameterLookup);
        }
    }

    internal sealed class BinaryNode : Node
    {
        private readonly BinaryOperation _operation;
        private readonly Node _left;
        private readonly Node _right;

        public BinaryNode(BinaryOperation operation, Node left, Node right)
        {
            _operation = operation;
            _left = left;
            _right = right;
        }

        public override double Evaluate(double x, Func<string, double> parameterLookup)
        {
            double left = _left.Evaluate(x, parameterLookup);
            double right = _right.Evaluate(x, parameterLookup);
            switch (_operation)
            {
                case BinaryOperation.Add: return left + right;
                case BinaryOperation.Subtract: return left - right;
                case BinaryOperation.Multiply: return left * right;
                case BinaryOperation.Divide: return left / right;
                case BinaryOperation.Power:
                    if (left < 0.0 && Math.Abs(right - Math.Round(right)) > 1e-12)
                        return double.NaN;
                    return Math.Pow(left, right);
                default: throw new EvaluationException("Unknown operation");
            }
        }
    }

    internal sealed class FunctionNode : Node
    {
        private readonly string _name;
        private readonly List<Node> _arguments;

        public FunctionNode(string name, List<Node> arguments)
        {
            _name = name;
            _arguments = arguments;
        }

        public override double Evaluate(double x, Func<string, double> parameterLookup)
        {
            double first = _arguments[0].Evaluate(x, parameterLookup);
            switch (_name)
            {
                case "sin": return Math.Sin(first);
                case "cos": return Math.Cos(first);
                case "tan": return Math.Tan(first);
                case "asin": return Math.Asin(first);
                case "acos": return Math.Acos(first);
                case "atan": return Math.Atan(first);
                case "sqrt": return first < 0.0 ? double.NaN : Math.Sqrt(first);
                case "abs": return Math.Abs(first);
                case "ln": return first <= 0.0 ? double.NaN : Math.Log(first);
                case "log10":
                case "lg": return first <= 0.0 ? double.NaN : Math.Log10(first);
                case "log":
                    if (_arguments.Count == 1)
                        return first <= 0.0 ? double.NaN : Math.Log10(first);
                    double baseValue = _arguments[1].Evaluate(x, parameterLookup);
                    return first <= 0.0 || baseValue <= 0.0 || Math.Abs(baseValue - 1.0) < 1e-12
                        ? double.NaN
                        : Math.Log(first, baseValue);
                case "exp": return Math.Exp(first);
                case "floor": return Math.Floor(first);
                case "ceil": return Math.Ceiling(first);
                default: throw new EvaluationException("Unknown function: " + _name);
            }
        }
    }

    public sealed class EvaluationException : Exception
    {
        public EvaluationException(string message) : base(message) { }
    }
}
