using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Text.RegularExpressions;
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
            list.Add(new Function("Max", "max",
                new Func<double, double, double>((d, d2) => Math.Max(d, d2))));
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

            private string GetDelimiterStrings()
            {
                int count = subExpressionDelimiters.GetUpperBound(0) + 1;
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < count; i++)
                    sb.Append(subExpressionDelimiters[i, 0]);
                return sb.ToString();
            }

            private void FixShorthand_MultiplyParen(_InternalData data)
            {
                string pattern = "";
                MatchCollection matches = null;
                int count = subExpressionDelimiters.GetUpperBound(0) + 1;
                for (int i = 0; i < count; i++)
                {
                    pattern = @"\d" + Regex.Escape(subExpressionDelimiters[i, 0]);
                    matches = Regex.Matches(data.Text, pattern);
                    for (int i2 = matches.Count - 1; i2 > -1; i2--)
                    {
                        Match m = matches[i2];
                        data.UpdateEquationText(m.Index + 1, 1, "*" + subExpressionDelimiters[i, 0]);
                    }
                }
            }
            private void FixShorthand_MultiplyVariable(_InternalData data)
            {
                string variableText = data.Equation.Variable.Shorthand.ToUpper();
                string pattern = @"\d" + Regex.Escape(variableText);
                var matches = Regex.Matches(data.Text, pattern, RegexOptions.IgnoreCase);
                int count = matches.Count - 1;
                for (int i = count; i > -1; i--)
                {
                    Match match = matches[i];
                    data.Text = data.UpdateEquationText(match.Index + 1, 1, "*" + data.Text[match.Index + 1]);
                }
            }

            private void RemoveShorthandNotations(_InternalData data)
            {
                FixShorthand_MultiplyParen(data);
                FixShorthand_MultiplyVariable(data);
            }

            public override Delegate Parse(Equation equation, EquationMemberGroup members)
            {
                _InternalData data = new _InternalData(equation, members);
                data.UsedFunctions = GetFunctionList(data);

                // must occur after obtaining used function list, because those functions might have numbers in them at the end
                RemoveShorthandNotations(data);

                data.Text = FixConstants(data);
                EvaluateSubExpressions(data);
                Expression finalExpression = ParseSubExpression(data, data.Text);
                LambdaExpression lambdaExpression = Expression.Lambda(finalExpression, data.Variable);

                Delegate compiledDelegate = lambdaExpression.Compile();
                object result = compiledDelegate.DynamicInvoke(new object[] { 5.5D });

                return compiledDelegate;
            }


            private List<MatchInfo> GetFunctionList(_InternalData data)
            {
                List<MatchInfo> functionList = new List<MatchInfo>();
                string equationText = data.Text;
                foreach (Function f in data.Functions)
                {
                    string pattern = f.Shorthand;
                    var matches = Regex.Matches(equationText, pattern);
                    foreach (Match m in matches)
                    {
                        int rightIndex = m.Index + m.Length;
                        if (rightIndex == equationText.Length) continue;
                        if (!IsNextValueADelimiter(equationText, rightIndex)) continue;
                        if (!ContainedInNumber(functionList, m.Index, m.Length))
                        {
                            functionList.Add(MatchInfo.FromMatch(m));
                        }
                    }
                }
                return functionList;
            }

            private bool IsNextValueADelimiter(string text, int index)
            {
                int count = subExpressionDelimiters.GetUpperBound(0) + 1;
                string subText = text.Substring(index);
                for (int i = 0; i < count; i++)
                {
                    if (subText.StartsWith(subExpressionDelimiters[i, 0])) return true;
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
                        int difference = subExpression.Y - subExpression.X;
                        string equationFragment = data.Text.Substring(subExpression.X + leftLength,
                                                                        difference - leftLength);
                        var expr = ParseSubExpression(data, equationFragment);

                        var Function = GetAttachedFunction(data, subExpression.X);
                        if (Function != null)
                        {
                            data.UsedFunctions.Remove(Function);
                            var functionMethod = data.Functions.FirstOrDefault(f => String.Equals(Function.Value.ToUpper(), f.Shorthand.ToUpper()));
                            var functionExpression = Expression.Call(functionMethod.Method.Method, expr);
                            //Delegate del = Expression.Lambda(functionExpression, data.Variable).Compile();
                            //object result = del.DynamicInvoke(new object[] { -85.3 });
                            { }
                            name = data.RegisterExpression(functionExpression);
                            leftIndex = subExpression.X - functionMethod.Shorthand.Length;
                            difference = subExpression.Y + subExpression.Right.Length - leftIndex;
                            data.Text = data.UpdateEquationText(leftIndex, difference, name); // subExpression.X - functionMethod.Shorthand.Length, difference + 1 + functionMethod.Shorthand.Length, name);
                            UpdateList(list, leftIndex, difference - name.Length, i);
                        }
                        else
                        {
                            name = data.RegisterExpression(expr);
                            data.Text = data.UpdateEquationText(subExpression.X, difference + 1, name);
                            UpdateList(list, leftIndex, difference - name.Length + 1, i);
                        }


                    }
                }
                { }
            }

            private MatchInfo GetAttachedFunction(_InternalData data, int index)
            {
                foreach (var function in data.UsedFunctions)
                {
                    if (function.Index + function.Length == index) return function;
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
                public string CurrentExpression { get { return ExpressionPrefix + currentExpression.ToString(); } }
                public Equation Equation { get; set; }
                public EquationMember[] Members { get; set; }
                public ParameterExpression Variable { get; set; }
                public List<Operator>[] Operators { get; set; }
                public Constant[] Constants { get; set; }
                public Function[] Functions { get; set; }
                public List<MatchInfo> UsedFunctions { get; set; }
                public string Text { get; set; }
                public Dictionary<string, Expression> SubExpressions { get; set; }

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
                    this.Variable = Expression.Parameter(typeof(double), equation.Variable.Shorthand);
                    this.Text = equation.Text; //.Replace(" ", ""); //          -2x*2+5*3(55x-2x*-A+(10x-2)-B)^2+sqrt(x)
                    this.SubExpressions = new Dictionary<string, Expression>();
                }

                private int currentExpression = 0;
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
                    foreach (MatchInfo matchInfo in this.UsedFunctions)
                        if (matchInfo.Index >= insertionIndex)
                            matchInfo.Index -= deletionLength - text.Length;

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


            // tomrrow: replace -55.7 with -1*55.7 ? getting rid of the - sign as a negative indicator, and keeping only the subtr operator text

            private Expression ParseSubExpression(_InternalData data, string equationFragment)
            {
                //equationFragment = "10x - -3.55^2 +33.0 - -35.0*5";
                // equationFragment = "x -2";

                string doubleNegativePattern = @"-{1}\s+-{1}"; // var matcheslol = Regex.Matches(equationFragment, doubleNegativePattern);                
                equationFragment = Regex.Replace(equationFragment, doubleNegativePattern, "+");

                string negPattern = @"^\s*-";
                equationFragment = Regex.Replace(equationFragment, negPattern, "0-");

                List<Expression> expressions = new List<Expression>();
                string pattern = @"\d+(?:\.{1}\d+)?"; // +data.Equation.Variable.Shorthand + "?";
                string variableText = data.Equation.Variable.Shorthand.ToUpper();
                string variablePattern = variableText + "{1}";
                var matches = Regex.Matches(equationFragment, pattern);
                var matches2 = Regex.Matches(equationFragment, variablePattern, RegexOptions.IgnoreCase);
                var matchInfoList = new List<MatchInfo>(matches.Count + matches2.Count);
                foreach (Match m in matches) matchInfoList.Add(MatchInfo.FromMatch(m));
                foreach (Match m in matches2) matchInfoList.Add(MatchInfo.FromMatch(m));
                matchInfoList.Sort((m1, m2) => m1.Index > m2.Index ? 1 : m1.Index < m2.Index ? -1 : 0);

                //pattern = @"\d" + Regex.Escape(variableText);
                //matches = Regex.Matches(equationFragment, pattern, RegexOptions.IgnoreCase);
                // for (int i = matches.cou)
                //equationFragment = FixShorthandVariableNotation(data, equationFragment, matchInfoList, matches2);

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
                            // arguments[0] = surroundingMatches.Item1.Value.ToUpper() == variableText ? data.Variable : Expression.Constant()
                            Expression expr = Expression.Call(op.Method.Method, arguments);
                            string val = data.RegisterExpression(expr);
                            int leftIndex = surroundingMatches.Item1.Index;
                            int rightIndex = surroundingMatches.Item2.Index + surroundingMatches.Item2.Length;
                            int difference = rightIndex - leftIndex;
                            equationFragment = equationFragment.Remove(leftIndex, difference);
                            equationFragment = equationFragment.Insert(leftIndex, val);

                            Delegate del2 = Expression.Lambda(expr, data.Variable).Compile();
                            object result2 = del2.DynamicInvoke(new object[] { 5.5D });

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
                Delegate del = Expression.Lambda(resultantExpression, data.Variable).Compile();
                object result = del.DynamicInvoke(new object[] { 5.5D });
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
                string variableText = data.Equation.Variable.Shorthand.ToUpper();
                double d;
                if (match.Value.ToUpper() == variableText)
                    return data.Variable;
                else if (Double.TryParse(match.Value, out d))
                    return Expression.Constant(d);
                else if (match.Value.StartsWith("$"))
                    return data.SubExpressions[match.Value];

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

            private string FixConstants(_InternalData data)
            {
                string newEquation = data.Text;
                foreach (Constant c in data.Constants)
                {
                    int previousIndex = 0;
                    for (; ; )
                    {
                        int index = newEquation.IndexOf(c.Shorthand, previousIndex);
                        if (index == -1) break;
                        if (ContainedInNumber(data.UsedFunctions, index, c.Shorthand.Length)) continue;

                        //newEquation = newEquation.Remove(index, c.Shorthand.Length);
                        string val = "";
                        if (c.IsConstantValue)
                        {
                            val = c.Value;
                        }
                        else
                        {
                            var expr = ParseSubExpression(data, c.Value);
                            val = data.RegisterExpression(expr);
                        }
                        //newEquation = newEquation.Insert(index, val);
                        newEquation = data.UpdateEquationText(index, c.Shorthand.Length, val);
                        previousIndex = index + val.Length;
                    }
                }
                return newEquation;
            }

        }
    }


}
