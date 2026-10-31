using System;
using System.Collections.Generic;
using System.Linq;

namespace FunctionClassroom
{
    public sealed class FunctionExpression
    {
        private readonly Node _root;
        private readonly string[] _parameterNames;

        internal FunctionExpression(Node root, IEnumerable<string> parameterNames)
        {
            _root = root;
            _parameterNames = parameterNames.OrderBy(name => name, StringComparer.Ordinal).ToArray();
        }

        public IList<string> ParameterNames
        {
            get { return Array.AsReadOnly(_parameterNames); }
        }

        public double Evaluate(double x, Func<string, double> parameterLookup)
        {
            try
            {
                return _root.Evaluate(x, parameterLookup);
            }
            catch (OverflowException)
            {
                return double.NaN;
            }
        }
    }
}
