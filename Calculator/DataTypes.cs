using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:ElementsMustBeDocumented", Justification = "Reviewed.")]
[assembly: SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:FileMayOnlyContainASingleClass", Justification = "Reviewed.")]
[assembly: SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1634:FileHeaderMustShowCopyright", Justification = "Reviewed.")]

namespace Calculator
{
    using System;
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
    using System.Collections;

    [DebuggerDisplay("{Text}")]
    public abstract class Equation
    {
        public abstract IEnumerable<Variable> Variables { get; set; }
        public abstract string Text { get; set; }
        public abstract IEnumerable<Constant> Constants { get; set; }

        public static Equation Create(string text, IEnumerable<Constant> constants = null, IEnumerable<Variable> variables = null)
        {
            return new DefaultEquation(text,
                constants ?? new Constant[0],
                variables ?? new Variable[1] { Variable.XVariable });
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

            private Variable[] variables;
            public override IEnumerable<Variable> Variables
            {
                get
                {
                    return variables;
                }
                set
                {
                    this.variables = value.ToArray();
                }
            }

            public DefaultEquation(string text, IEnumerable<Constant> constants, IEnumerable<Variable> variables)
            {
                this.text = text;
                this.constants = constants.ToArray();
                this.variables = variables.ToArray();
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
    public abstract class OperatorBase : EquationMember
    {
        private string name;
        public override string Name { get { return name; } }
        private string shorthand;
        public override string Shorthand { get { return shorthand; } }
        public int Order { get; set; }

        protected OperatorBase(string name, string shorthand, int order)
        {
            this.name = name;
            this.shorthand = shorthand;
            this.Order = order;
        }
    }

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

    [DebuggerDisplay("{Name} ({Shorthand}): {argumentCount} arguments")]
    public class Function : EquationMember
    {
        private string name;
        private string shorthand;
        private int argumentCount;
        private Delegate method;
        private static Function[] defaultList;

        static Function()
        {
            List<Function> list = new List<Function>();
            list.Add(new Function("Square Root", "sqrt",
                new Func<double, double>(d => Math.Sqrt(d))));
            list.Add(new Function("Max", "max",
                new Func<double, double, double>((d, d2) => Math.Max(d, d2))));
            list.Add(new Function("Log10", "log10",
                new Func<double, double>(d => Math.Log10(d))));
            defaultList = list.ToArray();
        }

        public Function(string name, string shorthand, Delegate method)
        {
            this.name = name;
            this.shorthand = shorthand;
            this.method = method;
            this.argumentCount = method.Method.GetParameters().Length;
        }

        public static Function[] DefaultList { get { return defaultList; } }

        public override string Name { get { return name; } }

        public override string Shorthand { get { return shorthand; } }

        public Delegate Method { get { return method; } }

        public int ArgumentCount { get { return argumentCount; } }

    }

    public class Variable : EquationMember
    {
        private static Variable variableX = new Variable("X", "Variable");
        public static Variable XVariable { get { return variableX; } }
        private static Variable variableY = new Variable("Y", "Variable");
        public static Variable YVariable { get { return variableY; } }

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

        public abstract Delegate Parse(Equation equation, object parseData = null);

        public static Delegate ParseEquation(Equation equation, EquationMemberGroup members)
        {
            return _Default.Parse(equation, members ?? EquationMemberGroup.Default);
        }

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

            private class EquationTextFormatter
            {
                private _InternalData data;
                private List<MatchInfo> list;
                private string text;
                private string leftDelimiterPattern;
                private string rightDelimiterPattern;
                private string variablePattern;

                public EquationTextFormatter(_InternalData data)
                {
                    this.data = data;
                    this.leftDelimiterPattern = GetDelimiterStrings(0);
                    this.rightDelimiterPattern = GetDelimiterStrings(1);
                    this.variablePattern = GetVariablePattern();
                }

                private string GetDelimiterStrings(int side = 0)
                {
                    int count = subExpressionDelimiters.GetUpperBound(0) + 1;
                    StringBuilder sb = new StringBuilder();
                    sb.Append(Regex.Escape(subExpressionDelimiters[0, side]));
                    for (int i = 1; i < count; i++)
                    {
                        sb.Append("|");
                        sb.Append(Regex.Escape(subExpressionDelimiters[i, side]));
                    }
                    return sb.ToString();
                }

                private string GetVariablePattern()
                {
                    StringBuilder sb = new StringBuilder();
                    if (data.Equation.Variables.Count() > 0)
                    {
                        foreach (var v in data.Equation.Variables)
                        {
                            sb.Append(Regex.Escape(v.Shorthand));
                            sb.Append('|');
                        }
                        sb.Remove(sb.Length - 1, 1);
                    }
                    return sb.ToString();
                }

                private bool CheckIfInsideFunction(int index)
                {
                    if (index == 0) return false;
                    if (text[index + 1] == _InternalData.FunctionDelimiter)
                    {
                        if (index > 1)
                        {
                            if (text[index - data.FunctionLength + 2] == _InternalData.FunctionDelimiter)
                            {
                                return true;
                            }
                        }
                        return false;
                    }
                    return false;
                }

                private void ReplaceIncompleteDecimalNumbers()
                {
                    string pattern = @"\D\.{1}\d+";
                    var matches = Regex.Matches(text, pattern);
                    foreach (Match match in matches)
                    {                   
                        list.Add(new MatchInfo(match.Index + 1, 0, "0"));
                    }

                    pattern = @"\A\.{1}\d+";
                    matches = Regex.Matches(text, pattern);
                    foreach (Match match in matches)
                    {
                        list.Add(new MatchInfo(match.Index, 0, "0"));
                    }
                }

                private void AppendFormatChanges_MultiplyNumbers()
                {
                    string pattern = @"\d[" + variablePattern + "|" + leftDelimiterPattern + "|" + Regex.Escape(_InternalData.FunctionDelimiter.ToString()) + "]";
                    var matches = Regex.Matches(text, pattern, RegexOptions.IgnoreCase);
                    foreach (Match match in matches)
                    {
                        if (CheckIfInsideFunction(match.Index)) continue;
                        list.Add(new MatchInfo(match.Index + 1, 0, Operator.Multiplication.Shorthand));
                    }
                }

                private void AppendFormatChanges_MultiplyVariable()
                {
                    string leftDelimiterPattern = "(" + this.leftDelimiterPattern + ")";
                    foreach (var v in data.Variables)
                    {
                        string pattern = v.Name.ToUpper() + leftDelimiterPattern;
                        var matches = Regex.Matches(text, pattern, RegexOptions.IgnoreCase);
                        foreach (Match match in matches)
                        {
                            list.Add(new MatchInfo(match.Index + 1, 0, Operator.Multiplication.Shorthand));
                        }
                    }
                }

                private void AppendFormatChanges_MultiplySubExpressionDelimiters()
                {
                    int count = subExpressionDelimiters.GetUpperBound(0) + 1;
                    string leftDelimiters = "(" + this.leftDelimiterPattern + ")";
                    for (int i = 0; i < count; i++)
                    {
                        string pattern = Regex.Escape(subExpressionDelimiters[i, 1]) + leftDelimiters;
                        var matches = Regex.Matches(text, pattern, RegexOptions.IgnoreCase);
                        foreach (Match match in matches)
                        {
                            list.Add(new MatchInfo(match.Index + subExpressionDelimiters[i, 0].Length, 0, Operator.Multiplication.Shorthand));
                        }
                    }
                }

                private void AppendFormatChanges_ReplaceDoubleNegatives()
                {
                    string doubleNegativePattern = @"-{1}\s+-{1}";
                    var matches = Regex.Matches(text, doubleNegativePattern);
                    foreach (Match match in matches)
                    {
                        list.Add(new MatchInfo(match.Index, match.Length, Operator.Addition.Shorthand));
                    }
                }

                private void ApplyChanges()
                {
                    if (list.Count == 0) return;
                    list.Sort((m1, m2) => m1.Index > m2.Index ? 1 : m1.Index < m2.Index ? -1 : 0);
                    int newLength = text.Length +
                        list.Sum(mi => mi.Value.Length - mi.Length);
                    int previousIndex = 0;
                    int currentIndex = 0;
                    char[] newString = new char[newLength];
                    foreach (var m in list)
                    {
                        for (int i = previousIndex; i < m.Index; i++)
                        {
                            newString[currentIndex] = text[i];
                            currentIndex++;
                        }

                        string lolztemp = new string(newString);
                        previousIndex = m.Index + m.Length;
                        for (int i = 0; i < m.Value.Length; i++)
                        {
                            newString[currentIndex] = m.Value[i];
                            currentIndex++;
                        }
                    }

                    for (int i = previousIndex; i < text.Length; i++)
                    {
                        newString[currentIndex] = text[i];
                        currentIndex++;
                    }

                    this.text = new String(newString);
                }

                public string FormatText(string text)
                {
                    this.text = text;
                    this.list = new List<MatchInfo>();
                    ReplaceIncompleteDecimalNumbers();
                    AppendFormatChanges_MultiplyNumbers();
                    AppendFormatChanges_MultiplyVariable();
                    AppendFormatChanges_MultiplySubExpressionDelimiters();
                    AppendFormatChanges_ReplaceDoubleNegatives();
                    ApplyChanges();
                    return this.text;
                }
            }

            public override Delegate Parse(Equation equation, object parseData)
            {
                EquationMemberGroup members = parseData is EquationMemberGroup ? (EquationMemberGroup)parseData : EquationMemberGroup.Default;
                _InternalData data = new _InternalData(equation, members);
                Program.Log.TraceInformation("Parsing equation " + equation.Text.ToString());
                GetFunctionList(data);
                FixConstants2(data);

                // must occur after obtaining used function list, because those functions might have numbers in them at the end
                data.Text = data.Formatter.FormatText(data.Text);

                EvaluateSubExpressions(data);

                Expression finalExpression = ParseSubExpression(data, data.Text);
                LambdaExpression lambdaExpression = null;

                // Adds about .04 ms to add variable declarations in
                int evaluatedConstantCount = data.ConstantExpressions.Count;
                if (evaluatedConstantCount > 0)
                {
                    var parameters = new ParameterExpression[evaluatedConstantCount];
                    var expressions = new Expression[evaluatedConstantCount + 1];
                    for (int i = 0; i < evaluatedConstantCount; i++)
                    {
                        var t = data.ConstantExpressions[i];
                        parameters[i] = t.Item1;
                        expressions[i] = t.Item2;
                    }
                    expressions[evaluatedConstantCount] = finalExpression;
                    BlockExpression be = BlockExpression.Block(parameters, expressions);
                    lambdaExpression = Expression.Lambda(be, data.Variables);
                }
                else
                {
                    lambdaExpression = Expression.Lambda(finalExpression, data.Variables);
                }

                Delegate compiledDelegate = lambdaExpression.Compile();
                //object result = compiledDelegate.DynamicInvoke(new object[] { 5.5D });

                return compiledDelegate;
            }

            private void GetFunctionList(_InternalData data)
            {
                foreach (Function f in data.Functions)
                {
                    string pattern = f.Shorthand;
                    var matches = Regex.Matches(data.Text, pattern, RegexOptions.IgnoreCase);
                    for (int i = matches.Count - 1; i > -1; i--)
                    {
                        data.RegisterFunction(f, matches[i].Index);
                    }
                }
            }

            private bool IsValueADelimiter(string text, int index, int delimiterType = 0)
            {
                int count = subExpressionDelimiters.GetUpperBound(0) + 1;
                string subText = (index == 0) ? text : text.Substring(index);
                for (int i = 0; i < count; i++)
                {
                    if (subText.StartsWith(subExpressionDelimiters[i, delimiterType])) return true;
                }
                return false;
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
                int previousIndex = 0, depth = 0;
                for (; ; )
                {
                    int leftIndex = equation.IndexOf(left, previousIndex);
                    int rightIndex = equation.IndexOf(right, previousIndex);
                    if (leftIndex == -1 && rightIndex == -1) break;
                    int newIndex = 0;
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

            private void EvaluateSubExpressions(_InternalData data)
            {
                int count = subExpressionDelimiters.GetUpperBound(0) + 1;
                string name = "";
                var list = new List<List<SubExpressionIndex>>();
                for (int i = 0; i < count; i++)
                    PopulateSubExpressionList(data.Text, subExpressionDelimiters[i, 0], subExpressionDelimiters[i, 1], list);


                for (int i = list.Count - 1; i > -1; i--)
                {
                    var depthList = list[i];
                    depthList.Sort((x, y) => x.X > y.X ? -1 : x.X < y.X ? 1 : 0);
                    foreach (var subExpression in depthList)
                    {
                        int leftIndex = subExpression.X;
                        int rightIndex = subExpression.Y;
                        int leftLength = subExpression.Left.Length;
                        int rightLength = subExpression.Right.Length;
                        int difference = subExpression.Y - subExpression.X - leftLength;
                        string equationFragment = data.Text.Substring(subExpression.X + leftLength,
                                                                        difference);
                        var function = GetAttachedFunction(data, subExpression.X);

                        Expression expr = null;


                        if (function != null)
                        {
                            string[] argumentFragments = equationFragment.Split(new char[] { ',' });

                            if (argumentFragments.Count() != function.ArgumentCount) throw new Exception(""); ////

                            Expression[] functionArguments = new Expression[argumentFragments.Count()];
                            for (int argIndex = 0; argIndex < argumentFragments.Count(); argIndex++)
                                functionArguments[argIndex] = ParseSubExpression(data, argumentFragments[argIndex]);
                            expr = Expression.Call(function.Method.Method, functionArguments);

                            int length = data.FunctionLength;
                            leftIndex -= length;
                            difference += length;
                        }
                        else
                        {
                            expr = ParseSubExpression(data, equationFragment);
                        }
                        name = data.RegisterExpression(expr);
                        difference += leftLength + rightLength;
                        data.Text = data.UpdateEquationText(leftIndex, difference, name);
                        UpdateList(list, leftIndex, difference - name.Length, i);
                    }
                }
            }

            private Function GetAttachedFunction(_InternalData data, int index)
            {
                int length = data.FunctionLength;
                if (index >= length)
                {
                    string text = data.Text.Substring(index - length, length);
                    Function function = null;
                    data.UsedFunctions.TryGetValue(text, out function);
                    return function;
                }
                return null;
            }

            private void UpdateList(List<List<SubExpressionIndex>> list, int leftIndex, int length, int depth)
            {
                for (int i = depth - 1; i > -1; i--)
                {
                    var lowerList = list[i];
                    foreach (var subExpression in lowerList)
                    {
                        if (subExpression.Y > leftIndex)
                        {
                            if (subExpression.X > leftIndex) subExpression.X -= length;
                            subExpression.Y -= length;
                        }
                        else if (subExpression.X > leftIndex)
                        {
                            subExpression.X -= length;
                            subExpression.Y -= length;
                        }
                    }
                }
            }

            private class _InternalData
            {
                public const string ExpressionPrefix = "$val";
                public const char FunctionDelimiter = '$';
                public int FunctionLength { get { return 5; } }
                public int ValueLength { get { return 5; } }
                public string CurrentExpression { get { return FunctionDelimiter + currentExpression.ToString("000") + FunctionDelimiter; } }
                public Equation Equation { get; set; }
                public EquationMember[] Members { get; set; }
                public ParameterExpression[] Variables { get; set; }
                public List<Operator>[] Operators { get; set; }
                public Constant[] Constants { get; set; }
                public Function[] Functions { get; set; }
                public string Text { get; set; }
                public Dictionary<string, Expression> SubExpressions { get; set; }
                public Dictionary<string, Function> UsedFunctions { get; set; }
                public EquationTextFormatter Formatter { get; set; }
                public List<Tuple<ParameterExpression, Expression>> ConstantExpressions { get; set; }

                public void RegisterFunction(Function function, int startIndex)
                {
                    currentExpression++;
                    string name = FunctionDelimiter + currentExpression.ToString("000") + FunctionDelimiter;
                    this.Text = this.Text.Remove(startIndex, function.Shorthand.Length);
                    this.Text = this.Text.Insert(startIndex, name);
                    UsedFunctions.Add(name, function);
                }

                public _InternalData(Equation equation, EquationMemberGroup members)
                {
                    this.Equation = equation;
                    this.Members = members.Members.ToArray();
                    this.Functions = (from f in members.Members.OfType<Function>()
                                      orderby f.Shorthand.Length descending
                                      select f).ToArray();
                    this.Operators = (from o in this.Members.OfType<Operator>()
                                      group o by o.Order into oGroup
                                      orderby oGroup.Key descending
                                      select new List<Operator>(from g in oGroup select g)).ToArray();
                    this.Constants = equation.Constants.OrderByDescending(x => x.Shorthand.Length).ToArray();
                    this.Variables = equation.Variables.Select(v => Expression.Parameter(typeof(double), v.Shorthand)).ToArray();
                    this.Text = equation.Text;
                    this.SubExpressions = new Dictionary<string, Expression>();
                    this.UsedFunctions = new Dictionary<string, Function>();
                    this.ConstantExpressions = new List<Tuple<ParameterExpression, Expression>>();
                    this.Formatter = new EquationTextFormatter(this);
                }

                private int currentExpression = -1;
                public string RegisterExpression(Expression exp)
                {
                    currentExpression++;
                    string name = CurrentExpression;
                    SubExpressions.Add(name, exp);
                    return name;
                }

                public string UpdateEquationText(int insertionIndex, int deletionLength, string text)
                {
                    this.Text = this.Text.Remove(insertionIndex, deletionLength);
                    this.Text = this.Text.Insert(insertionIndex, text);

                    return this.Text;
                }
            }

            [DebuggerDisplay("{Index}, {Length}:  {Value}")]
            private class MatchInfo
            {
                public int Index { get; set; }
                public int Length { get; set; }
                public string Value { get; set; }

                public static MatchInfo FromMatch(Match match)
                {
                    MatchInfo matchInfo = new MatchInfo()
                    {
                        Index = match.Index,
                        Length = match.Length,
                        Value = match.Value
                    };
                    return matchInfo;
                }

                public MatchInfo()
                {
                }

                public MatchInfo(int index, int length, string value)
                {
                    this.Index = index;
                    this.Length = length;
                    this.Value = value;
                }
            }

            private class OperatorIndexListing
            {
                public int Index { get; set; }
                public Operator Operator { get; set; }
            }

            private List<OperatorIndexListing> GetIndexesOfOperators(string text, List<Operator> operators)
            {
                List<OperatorIndexListing> indexes = new List<OperatorIndexListing>();
                foreach (Operator op in operators)
                {
                    int previousIndex = 0;
                    int opTextLength = op.Shorthand.Length;
                    for (; ; )
                    {
                        int index = text.IndexOf(op.Shorthand, previousIndex);
                        if (index == -1) break;
                        indexes.Add(new OperatorIndexListing() { Index = index, Operator = op });
                        previousIndex = index + opTextLength;
                    }
                }
                indexes.Sort((i1, i2) => i1.Index > i2.Index ? 1 : i1.Index < i2.Index ? -1 : 0);
                //indexes.Reverse();
                return indexes;
            }


            private string FixString(_InternalData data, string equationFragment)
            {
                string negPattern = @"[" + Regex.Escape(@"*+\") + @"]\s*-{1}\s*";
                var matches = Regex.Matches(equationFragment, negPattern);
                int count = matches.Count - 1;
                for (int i = count; i > -1; i--)
                {
                    Match match = matches[i];
                    string newstr = match.Value.Substring(0, 1) + "-1*";
                    equationFragment = equationFragment.Remove(match.Index, match.Length);
                    equationFragment = equationFragment.Insert(match.Index, newstr);
                }

                string negPattern213 = @"^\s*-";
                equationFragment = Regex.Replace(equationFragment, negPattern213, "0-");
                return equationFragment;
            }

            private double Temp_EvaluateExpr(_InternalData data, string str)
            {
                Expression expr = data.SubExpressions[str];
                object result = Expression.Lambda(expr, data.Variables).Compile().DynamicInvoke(new object[] { 5.5D });
                Console.WriteLine(str + ": " + result.ToString());
                return (double)result;
            }

            private string GetVariablePattern(_InternalData data)
            {
                StringBuilder sb = new StringBuilder();
                if (data.Equation.Variables.Count() > 0)
                {
                    foreach (var v in data.Equation.Variables)
                    {
                        sb.Append(Regex.Escape(v.Shorthand.ToUpper()));
                        sb.Append('|');
                    }
                    sb.Remove(sb.Length - 1, 1);
                }
                return sb.ToString();
            }

            private Expression ParseSubExpression(_InternalData data, string equationFragment)
            {
                equationFragment = FixString(data, equationFragment);

                List<Expression> expressions = new List<Expression>();
                string pattern = @"\d+(?:\.{1}\d+)?"; // +data.Equation.Variable.Shorthand + "?";
                string variablePattern = GetVariablePattern(data) + "{1}";
                string negPattern = @"-1\*";

                var matches = Regex.Matches(equationFragment, pattern);
                var matches2 = Regex.Matches(equationFragment, variablePattern, RegexOptions.IgnoreCase);
                var matches3 = Regex.Matches(equationFragment, negPattern);
                var matchInfoList = new List<MatchInfo>(matches.Count + matches2.Count);

                foreach (Match m in matches)
                {
                    if (m.Index > 0 && equationFragment[m.Index - 1] == _InternalData.FunctionDelimiter)
                        matchInfoList.Add(new MatchInfo(m.Index - 1, m.Length + 2, _InternalData.FunctionDelimiter + m.Value + _InternalData.FunctionDelimiter));
                    else
                        matchInfoList.Add(MatchInfo.FromMatch(m));
                }
                foreach (Match m in matches2) matchInfoList.Add(MatchInfo.FromMatch(m));
                foreach (Match m in matches3)
                {
                    MatchInfo matchInfo = matchInfoList.First(minfo => minfo.Index == m.Index + 1);
                    matchInfo.Index -= 1;
                    matchInfo.Value = "-1";
                    matchInfo.Length = 2;
                }

                if (matchInfoList.Count == 0) throw new Exception("");
                if (matchInfoList.Count == 1)
                    return GetExpressionFromMatch(data, matchInfoList[0]);

                matchInfoList.Sort((m1, m2) => m1.Index > m2.Index ? 1 : m1.Index < m2.Index ? -1 : 0);

                foreach (var opList in data.Operators)
                {
                    var indexes = GetIndexesOfOperators(equationFragment, opList);
                    for (int i = 0; i < indexes.Count; i++)
                    {
                        var opIndex = indexes[i];
                        Operator op = opIndex.Operator;
                        int index = opIndex.Index;
                        int length = op.Shorthand.Length;

                        if (!ContainedInNumber(matchInfoList, index, length))
                        {
                            var surroundingMatches = GetSurroundingMatches(matchInfoList, index);
                            if (surroundingMatches == null) throw new Exception(""); /////
                            var arguments = new Expression[2];
                            arguments[0] = GetExpressionFromMatch(data, surroundingMatches.Item1);
                            arguments[1] = GetExpressionFromMatch(data, surroundingMatches.Item2);
                            Expression expr = op.GetExpression(arguments[0], arguments[1]);

                            string val = data.RegisterExpression(expr);
                            int leftIndex = surroundingMatches.Item1.Index;
                            int rightIndex = surroundingMatches.Item2.Index + surroundingMatches.Item2.Length;
                            int difference = rightIndex - leftIndex;
                            equationFragment = equationFragment.Remove(leftIndex, difference);
                            equationFragment = equationFragment.Insert(leftIndex, val);

                            //Delegate del2 = Expression.Lambda(expr, data.Variables).Compile();
                            //object result2 = del2.DynamicInvoke(new object[] { 5.5D });

                            UpdateMatches(matchInfoList, surroundingMatches, leftIndex, val, difference - val.Length);
                            for (int i2 = i + 1; i2 < indexes.Count; i2++) indexes[i2].Index -= difference - val.Length;
                            { }
                        }
                    }
                }
                if (equationFragment != data.CurrentExpression)
                {
                    throw new Exception(""); ////
                }

                Expression resultantExpression = data.SubExpressions[equationFragment];
                //Delegate del = Expression.Lambda(resultantExpression, data.Variables).Compile();
                //object result = del.DynamicInvoke(new object[] { 5.5D });
                return resultantExpression;
            }

            private void UpdateMatches(List<MatchInfo> matches, Tuple<MatchInfo, MatchInfo> surroundingMatches, int newIndex, string newValue, int difference)
            {
                int index = matches.IndexOf(surroundingMatches.Item1);
                matches.RemoveAt(index + 1);
                matches[index] = new MatchInfo() { Index = newIndex, Length = newValue.Length, Value = newValue };
                int count = matches.Count;
                for (int i = index + 1; i < count; i++)
                {
                    matches[i].Index -= difference;
                }
            }

            private Expression GetExpressionFromMatch(_InternalData data, MatchInfo match)
            {
                double d;
                if (Double.TryParse(match.Value, out d))
                {
                    return Expression.Constant(d);
                }
                if (match.Value.StartsWith("$"))
                {
                    return data.SubExpressions[match.Value];
                }
                string matchText = match.Value.ToUpper();
                foreach (var v in data.Variables)
                {
                    if (string.Equals(matchText, v.Name.ToUpper()))
                    {
                        return v;
                    }
                }
                return null;
            }

            private Tuple<MatchInfo, MatchInfo> GetSurroundingMatches(List<MatchInfo> matches, int index)
            {
                int count = matches.Count;
                for (int i = 0; i < count; i++)
                {
                    MatchInfo match = matches[i];
                    if (match.Index >= index)
                    {
                        if (i == 0)
                            throw new Exception(""); ////
                        return new Tuple<MatchInfo, MatchInfo>(matches[i - 1], match);
                    }
                }
                return null;
            }

            private bool ContainedInNumber(List<MatchInfo> matches, int index, int length)
            {
                int indexRange = index + length;
                foreach (MatchInfo match in matches)
                {
                    int matchRange = match.Index + match.Length;
                    if (index == match.Index) return true;
                    if (indexRange == matchRange) return true;
                    if (index > match.Index && indexRange <= matchRange) return true;
                }
                return false;
            }

            private string EvaluateConstantValue(_InternalData data, Constant constant)
            {
                if (constant.IsConstantValue)
                {
                    return constant.Value;
                }
                else
                {
                    string formattedValue = data.Formatter.FormatText(constant.Value);
                    var expression = ParseSubExpression(data, formattedValue);
                    var variableExpression = Expression.Variable(typeof(double), constant.Shorthand);
                    var assignmentExpression = Expression.Assign(variableExpression, expression);
                    //var blockExpression = Expression.Block(new ParameterExpression[] { variableExpression}, assignmentExpression);
                    //var lol = Expression.Lambda(blockExpression, data.Variables);
                    //object lolzo = lol.Compile().DynamicInvoke(new object[] { 5.5D });
                    data.ConstantExpressions.Add(new Tuple<ParameterExpression, Expression>(variableExpression, assignmentExpression));
                    return data.RegisterExpression(variableExpression);
                }
            }

            private void FixConstants2(_InternalData data)
            {
                foreach (Constant c in data.Constants)
                {
                    int previousIndex = 0;
                    var indexes = new List<int>();
                    for (; ; )
                    {
                        int index = data.Text.IndexOf(c.Shorthand, previousIndex);
                        if (index == -1) break;
                        indexes.Add(index);
                        previousIndex = index + c.Shorthand.Length;
                    }

                    indexes.Sort((x, y) => x < y ? 1 : x > y ? -1 : 0);

                    // only evaluate the constant's value if that constant is actually contained in the equation
                    if (previousIndex > 0)
                    {
                        string text = EvaluateConstantValue(data, c);
                        int length = c.Shorthand.Length;
                        foreach (var index in indexes)
                        {
                            int newIndex = index;
                            StringBuilder replacementString = new StringBuilder();
                            if (index > 0)
                            {
                                char charLeft = data.Text[index - 1];
                                if (char.IsLetterOrDigit(charLeft) || charLeft == _InternalData.FunctionDelimiter)
                                {
                                    newIndex -= Operator.Multiplication.Shorthand.Length;
                                    replacementString.Append(Operator.Multiplication.Shorthand);
                                }
                            }
                            replacementString.Append(text);
                            int rightIndex = index + length;
                            if (rightIndex < data.Text.Length)
                            {
                                char charRight = data.Text[rightIndex];
                                if (char.IsLetterOrDigit(charRight) || charRight == _InternalData.FunctionDelimiter || IsValueADelimiter(data.Text, rightIndex, 0))
                                {
                                    replacementString.Append(Operator.Multiplication.Shorthand);
                                }
                            }
                            //data.Text = data.Text.InsertReplace(index.Item1, index.Item2.Shorthand.Length, replacementString.ToString());

                            data.Text = data.Text.InsertReplace(index, length, replacementString.ToString());
                        }
                    }
                }
            }

        }
    }


}
