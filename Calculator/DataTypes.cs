using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Calculator
{

    // 
    public abstract class EquationBoxBase
    {
        // Include a 'Constants' textbox locally for each EBox and one globally (cannot have repeat values for local)
        // CheckBox to enable/disable an EB for graphing

        protected RichTextBox rtbEquation;

        public virtual string Variable { get { return "X"; } }

        public virtual string Text { get { return rtbEquation.Text; } set { rtbEquation.Text = value; } }

        public virtual bool HasEquation { get { return !string.IsNullOrEmpty(rtbEquation.Text); } }
    }

    // Holds one or more EquationBox instances, with a + button to allow adding more equations
    public class EquationPanel : Panel
    {

    }




    [DebuggerDisplay("{Text}")]
    public abstract class Equation
    {
        public abstract Variable Variable { get; set; }
        public abstract string Text { get; set; }
        public abstract IEnumerable<Constant> Constants { get; set; }

        public static Equation Create(string text, IEnumerable<Constant> constants = null, Variable variable = null)
        {
            return new DefaultEquation(text, constants ?? new Constant[0], variable ?? Variable.Default);
        }

        private class DefaultEquation : Equation
        {
            private string text;
            public override string Text
            {
                get
                {
                    return text;
                }
                set
                {
                    text = value;
                }
            }

            private Constant[] constants;
            public override IEnumerable<Constant> Constants
            {
                get
                {
                    return constants;
                }
                set
                {
                    constants = (value is Constant[]) ? (Constant[])value : value.ToArray();
                }
            }

            private Variable variable;
            public override Variable Variable
            {
                get
                {
                    return variable;
                }
                set
                {
                    this.variable = value;
                }
            }

            public DefaultEquation(string text, IEnumerable<Constant> constants, Variable variable)
            {
                this.text = text;
                this.constants = constants.ToArray();
                this.variable = variable;
            }
        }
    }

    public abstract class EquationMember
    {
        public abstract string Name { get; }
        public abstract string Shorthand { get; }

        public virtual bool CheckIfEqual(string text)
        {
            return String.Equals(this.Shorthand, text);
        }
    }

    public abstract class EquationMethodMember : EquationMember
    {
        private string name;
        public override string Name { get { return name; } }
        private string shorthand;
        public override string Shorthand { get { return shorthand; } }

        private Delegate method;
        public Delegate Method { get { return method; } }

        protected EquationMethodMember(string name, string shorthand, Delegate method)
        {
            this.name = name;
            this.shorthand = shorthand;
            this.method = method;
        }
    }

    [DebuggerDisplay("{Name} ({Shorthand})")]
    public class Operator : EquationMethodMember
    {
        public int Order { get; set; }
        public Operator(string name, string shorthand, Delegate method, int order)
            : base(name: name, shorthand: shorthand, method: method)
        {
            this.Order = order;
        }

        private static Operator[] defaultList;
        public static Operator[] DefaultList { get { return defaultList; } }

        static Operator()
        {
            List<Operator> list = new List<Operator>();
            list.Add(new Operator("Addition", "+",
                new Func<double, double, double>((d1, d2) => d1 + d2), 0));
            list.Add(new Operator("Subtraction", "-",
                new Func<double, double, double>((d1, d2) => d1 - d2), 0));
            list.Add(new Operator("Multiplication", "*",
                new Func<double, double, double>((d1, d2) => d1 * d2), 1));
            list.Add(new Operator("Division", "/",
                new Func<double, double, double>((d1, d2) => d1 * d2), 1));
            list.Add(new Operator("Exponent", "^",
                new Func<double, double, double>((d1, d2) => Math.Pow(d1, d2)), 2));
            list.Add(new Operator("Modulus", "%",
                new Func<double, double, double>((d1, d2) => d1 % d2), 1));
            defaultList = list.ToArray();
        }
    }

    [DebuggerDisplay("{Name} ({Shorthand}): {argumentCount} arguments")]
    public class Function : EquationMethodMember
    {
        private int argumentCount;
        public int ArgumentCount { get { return argumentCount; } }

        public Function(string name, string shorthand, Delegate method)
            : base(name: name, shorthand: shorthand, method: method)
        {
            this.argumentCount = method.Method.GetParameters().Length;
        }

        private static Function[] defaultList;
        public static Function[] DefaultList { get { return defaultList; } }

        static Function()
        {
            List<Function> list = new List<Function>();
            list.Add(new Function("Square Root", "sqrt",
                new Func<double, double>(d => Math.Sqrt(d))));
            defaultList = list.ToArray();
        }
    }

    public class Variable : EquationMember
    {
        private static Variable _Default = new Variable("X", "Variable");
        public static Variable Default { get { return _Default; } }

        private string shorthand;
        public override string Shorthand { get { return shorthand; } }
        private string name;
        public override string Name { get { return name; } }

        public Variable(string shorthand, string name = "Variable")
        {
            this.shorthand = shorthand;
            this.name = name ?? "";
        }
    }

    // case sensitivity?
    [DebuggerDisplay("{Shorthand,nq}:  {Value,nq}")]
    public class Constant : Variable
    {
        public string Value { get; set; } // either a string double like "55.7", or a non-numeric string like "X - 5"

        public Constant(string shorthand, string value, string name = "")
            : base(shorthand, name ?? "Constant")
        {
            this.Value = value;
        }

        public bool IsConstantValue
        {
            get
            {
                double d;
                bool result = double.TryParse(Value, out d);
                return result;
            }
        }
    }

    public class SubExpressionDelimiter : EquationMember
    {

        public override string Name
        {
            get { throw new NotImplementedException(); }
        }

        public override string Shorthand
        {
            get { throw new NotImplementedException(); }
        }
    }

    public class EquationMemberGroup
    {
        private string name;
        public string Name { get { return name; } }

        private IEnumerable<EquationMember> members;
        public IEnumerable<EquationMember> Members { get { return members; } }

        public EquationMemberGroup(string name, IEnumerable<EquationMember> members)
        {
            this.name = Name;
            this.members = members;
        }

        private static EquationMemberGroup _Default;
        public static EquationMemberGroup Default { get { return _Default; } }
        static EquationMemberGroup()
        {
            var operators = (IEnumerable<EquationMember>)Operator.DefaultList;
            var functions = (IEnumerable<EquationMember>)Function.DefaultList;
            var combinedLists = operators.Concat(functions);

            _Default = new EquationMemberGroup("Default", combinedLists);
        }
    }

    public abstract class EquationParser
    {
        private static EquationParser _Default = new DefaultEquationParser();
        public static EquationParser Default
        {
            get { return _Default; }
        }

        public abstract string[,] SubExpressionDelimiters { get; }

        public abstract Delegate Parse(Equation equation, EquationMemberGroup members);

        public static Delegate ParseEquation(Equation equation, EquationMemberGroup members)
        {
            return _Default.Parse(equation, members);
        }




        // resume work on parsing equation fragments tomorrow

        private class DefaultEquationParser : EquationParser
        {
            private static string[,] subExpressionDelimiters = new string[2, 2];

            static DefaultEquationParser()
            {
                subExpressionDelimiters[0, 0] = "(";
                subExpressionDelimiters[0, 1] = ")";
                subExpressionDelimiters[1, 0] = "[";
                subExpressionDelimiters[1, 1] = "]";
            }

            public override string[,] SubExpressionDelimiters
            {
                get { return subExpressionDelimiters; }
            }

            public override Delegate Parse(Equation equation, EquationMemberGroup members)
            {
                _InternalData data = new _InternalData(equation, members);
                GetSubExpressions(data);

                BlockExpression compositeBlock = _ParseInner(data, 0);
                LambdaExpression lambdaExpression = Expression.Lambda(compositeBlock, null);

                Delegate compiledDelegate = lambdaExpression.Compile();
                return compiledDelegate;
            }


            private class SubExpressionIndex
            {
                public int X { get; set; }
                public int Y { get; set; }
                public string Left { get; set; }
                public string Right { get; set; }

                public SubExpressionIndex(int x, int y, string left, string right)
                {
                    this.X = x;
                    this.Y = y;
                    this.Left = left;
                    this.Right = right;
                }
            }

            private void PopulateSubExpressionList(string equation, string left, string right, List<List<SubExpressionIndex>> list)
            {
                // if (list == null) list = new List<List<SubExpressionIndex>>();
                //List<List<Point>> points = new List<List<Point>>();
                int previousIndex = 0, depth = 0;

                for (; ; )
                {
                    int leftIndex = equation.IndexOf(left, previousIndex);
                    int rightIndex = equation.IndexOf(right, previousIndex);
                    if (leftIndex == -1 && rightIndex == -1) break;
                    int newIndex = 0;
                    Point p;
                    if (leftIndex < rightIndex && leftIndex != -1)
                    {
                        newIndex = leftIndex;
                        if (list.Count == depth) list.Add(new List<SubExpressionIndex>());

                        list[depth].Add(new SubExpressionIndex(newIndex, -1, left, right));
                        depth++;

                    }
                    else if (rightIndex < leftIndex || (rightIndex != -1 && leftIndex == -1))
                    {
                        newIndex = rightIndex;
                        depth--;
                        var pt = list[depth];
                        pt[pt.Count - 1].Y = newIndex;
                    }
                    else
                    {
                        break;
                    }

                    previousIndex = newIndex + 1;
                }
                // validate the points just in case later
            }

            private void GetSubExpressions(_InternalData data)
            {
                int count = subExpressionDelimiters.GetUpperBound(0) + 1;
                string newEquation = data.Text;
                var list = new List<List<SubExpressionIndex>>();
                for (int i = 0; i < count; i++)
                    PopulateSubExpressionList(newEquation, subExpressionDelimiters[i, 0], subExpressionDelimiters[i, 1], list);

                Dictionary<string, Expression> exprs = new Dictionary<string, Expression>();
                int currentExpression = 0;
                string expressionPrefix = "$var";
                for (int i = list.Count - 1; i > -1; i--)
                {
                    var depthList = list[i];
                    depthList.Sort((x, y) => x.X > y.X ? -1 : x.X < y.X ? 1 : 0);
                    foreach (var subExpression in depthList)
                    {
                        string name = expressionPrefix + currentExpression.ToString();
                        int leftIndex = subExpression.X - subExpression.Left.Length;
                        int rightIndex = subExpression.Y + subExpression.Right.Length;
                        int difference = subExpression.Y - subExpression.X;
                        string equationFragment = newEquation.Substring(subExpression.X, difference);

                        // parse the equation, store the resulting expression in a dictionary?

                        newEquation = newEquation.Remove(subExpression.X, difference);
                        newEquation = newEquation.Insert(subExpression.X, name);
                        currentExpression++;
                        UpdateList(list, leftIndex, difference - name.Length, i);

                    }
                }
                { }
            }

            private void UpdateList(List<List<SubExpressionIndex>> list, int leftIndex, int length, int depth)
            {
                for (int i = depth - 1; i > -1; i--)
                {
                    var lowerList = list[i];
                    foreach (var subExpression in lowerList)
                    {
                        if (subExpression.X > leftIndex)
                        {
                            subExpression.X -= length;
                            subExpression.Y -= length;
                        }
                    }
                }
            }
            private class _InternalData
            {
                public Equation Equation { get; set; }
                public EquationMember[] Members { get; set; }
                public ParameterExpression Variable { get; set; }
                public string Text { get; set; }
                public Dictionary<string, Expression> SubExpressions { get; set; }

                //public Dictionary<Constant, ParameterExpression> Constants { get; set; }

                public _InternalData(Equation equation, EquationMemberGroup members)
                {
                    this.Equation = equation;
                    this.Members = members.Members.ToArray();
                    this.Variable = Expression.Parameter(typeof(double), equation.Variable.Shorthand);
                    this.Text = equation.Text; //.Replace(" ", ""); //          -2x*2+5*3(55x-2x*-A+(10x-2)-B)^2+sqrt(x)
                    this.SubExpressions = new Dictionary<string, Expression>();

                    //var dict = new Dictionary<Constant, ParameterExpression>(equation.Constants.Count());
                    //var dict2 = equation.Constants.ToDictionary(c => c, c => Expression.Parameter(typeof( )
                    //this.Constants = dict;
                }

            }

            // Should be free of all parenthesis
            private BlockExpression _ParseInner(_InternalData data, string equationFragment)
            {

                List<Expression> expressions = new List<Expression>();

                return Expression.Block(expressions);
            }

            //         " -2x * 2 +5 * 3(55x - 2x *-A + (10x -2)-B)^2+sqrt(x- 1)"          |         -2x*2+5*3(55x-2x*-A+(10x-2)-B)^2+sqrt(x)


            // var1 = Subtract(Mult(10, x), 2)
            // var2 = Subtract(Mult(55, x), Subtract(Add(Mult(Mult(2, x), Mult(-1, A)), var1), B)
            // var3 = Subtract(x, 1)
            // Add(Add(Mult(Mult(-2, x), 2), Mult(5, Mult(3, Exp(var2, 2)))), sqrt(var3))
            //         "-2x * 2 + 5 * 3 * var1^2 + sqrt(var3)
            //              "55x -2x * -A + var2 - B"
            // var1-3 = compiled delegates taking in X? Func<double, double> ?

            private class indexio
            {
                public int StartIndex { get; set; }
                public string Text { get; set; }

                public indexio(int startIndex, string text)
                {
                    this.StartIndex = startIndex;
                    this.Text = text;
                }
            }

            private BlockExpression _ParseInner(_InternalData data, int startIndex)
            {
                List<Expression> expressions = new List<Expression>();
                string equationText = data.Text;

                StringBuilder sbTemp = new StringBuilder();
                string currentNumber = "";





                // find numbers
                for (int i = startIndex; i < equationText.Length; i++)
                {
                    char c = equationText[i];
                    if (char.IsNumber(c))
                    {
                        currentNumber += c;
                        continue;
                    }
                    if (c == ' ')
                    {

                    }
                    else if (c == '-')
                    {
                        if (i != equationText.Length - 1)
                        {

                        }
                    }

                }

                return Expression.Block(expressions);
            }

            //private bool GetCurrentNumber()
        }
    }


}
