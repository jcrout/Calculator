
namespace Calculator
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Drawing;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using System.Windows.Forms;
    using JonUtility;
    using CS = CalculatorSettings;


    [DebuggerDisplay("{Name} ({Shorthand})")]
    public abstract class Operator : EquationMember
    {
        private static Operator[] defaultList;
        public static Operator[] DefaultList { get { return defaultList; } }

        public static Operator Addition { get { return defaultList[0]; } }
        public static Operator Subtraction { get { return defaultList[1]; } }
        public static Operator Multiplication { get { return defaultList[2]; } }
        public static Operator Division { get { return defaultList[3]; } }
        public static Operator Modulo { get { return defaultList[4]; } }
        public static Operator Exponent { get { return defaultList[5]; } }

        public abstract int Order { get; }
        public abstract Expression GetExpression(Expression arg1, Expression arg2);

        static Operator()
        {
            List<Operator> list = new List<Operator>();
            list.Add(new AdditionOperator());
            list.Add(new SubtractionOperator());
            list.Add(new MultiplicationOperator());
            list.Add(new DivisionOperator());
            list.Add(new ModuloOperator());
            list.Add(new ExponentOperator());
            defaultList = list.ToArray();
        }

        private class AdditionOperator : Operator
        {
            public override string Name { get { return "Addition"; } }
            public override string Shorthand { get { return "+"; } }
            public override int Order { get { return 0; } }
            public override Expression GetExpression(Expression arg1, Expression arg2)
            { return Expression.Add(arg1, arg2); }
        }

        private class SubtractionOperator : Operator
        {
            public override string Name { get { return "Subtraction"; } }
            public override string Shorthand { get { return "-"; } }
            public override int Order { get { return 0; } }
            public override Expression GetExpression(Expression arg1, Expression arg2)
            { return Expression.Subtract(arg1, arg2); }
        }

        private class MultiplicationOperator : Operator
        {
            public override string Name { get { return "Multiplication"; } }
            public override string Shorthand { get { return "*"; } }
            public override int Order { get { return 1; } }
            public override Expression GetExpression(Expression arg1, Expression arg2)
            { return Expression.Multiply(arg1, arg2); }
        }

        private class DivisionOperator : Operator
        {
            public override string Name { get { return "Division"; } }
            public override string Shorthand { get { return "/"; } }
            public override int Order { get { return 1; } }
            public override Expression GetExpression(Expression arg1, Expression arg2)
            { return Expression.Divide(arg1, arg2); }
        }

        private class ModuloOperator : Operator
        {
            public override string Name { get { return "Modulo"; } }

            public override string Shorthand { get { return "%"; } }

            public override int Order { get { return 1; } }

            public override Expression GetExpression(Expression arg1, Expression arg2)
            { return Expression.Modulo(arg1, arg2); }
        }

        private class ExponentOperator : Operator
        {
            public override string Name { get { return "Exponent"; } }
            public override string Shorthand { get { return "^"; } }
            public override int Order { get { return 2; } }
            public override Expression GetExpression(Expression arg1, Expression arg2)
            { return Expression.Power(arg1, arg2); }
        }

    }

}
