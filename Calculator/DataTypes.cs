using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:ElementsMustBeDocumented", Justification = "Reviewed.")]
[assembly: SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:FileMayOnlyContainASingleClass", Justification = "Reviewed.")]
[assembly: SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1634:FileHeaderMustShowCopyright", Justification = "Reviewed.")]

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

    [DebuggerDisplay("{Text}")]
    public abstract class Equation
    {
        public abstract string Text { get; set; }
        public abstract IEnumerable<Constant> Constants { get; set; }
        public abstract IEnumerable<Variable> Variables { get; set; }

        public static Equation Create(string text, IEnumerable<Constant> constants = null, IEnumerable<Variable> variables = null)
        {
            return new DefaultEquation(text,
                constants ?? new Constant[0],
                variables ?? new Variable[1] { Variable.XVariable });
        }

        public override string ToString()
        {
            return this.Text ?? string.Empty;
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

    [DebuggerDisplay("{Name}: {Shorthand}")]
    public abstract class EquationMember
    {
        public abstract string Name { get; }
        public abstract string Shorthand { get; }

        public virtual bool CheckIfEqual(string text)
        {
            return String.Equals(this.Shorthand, text);
        }

        internal Type GetHighestDerivedType()
        {
            Type t = this.GetType();
            if (t.BaseType == typeof(EquationMember))
                return t;

            for (; ; )
            {
                t = t.BaseType;
                if (t.BaseType == typeof(EquationMember))
                    return t;
            }
        }

        public override string ToString()
        {
            return Shorthand.ToString();
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
            list.Add(new Function("Round", "round",
                new Func<double, double, double>((d1, d2) => Math.Round(d1, (int)d2, MidpointRounding.AwayFromZero))));
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
        public Constant(string shorthand, string value, string name = "")
            : base(shorthand, name ?? "Constant")
        {
            this.Value = value;
        }

        public string Value { get; set; } // either a string double like "55.7", or a non-numeric string like "X - 5"

        public bool IsNumber
        {
            get
            {
                double d;
                bool result = double.TryParse(this.Value, out d);
                return result;
            }
        }
    }

    public class SubExpressionDelimiter : EquationMember
    {
        private readonly string left;
        private readonly string right;
        private string shorthand;

        public SubExpressionDelimiter(string shorthand)
        {
            this.shorthand = shorthand;
            int index = shorthand.IndexOf(',');
            if (index == -1)
                throw new ArgumentException("shorthand must contain a single comma delimiter between the left and right values");
            this.left = shorthand.Substring(0, index);
            this.right = shorthand.Substring(index + 1);
        }

        public override string Name
        {
            get { return "Delimiter"; }
        }

        public override string Shorthand
        {
            get { return shorthand; }
        }

        public string this[int index]
        {
            get
            {
                return index <= 0 ? this.left : this.right;
            }
        }

        public string this[bool leftDelimiter]
        {
            get
            {
                return leftDelimiter ? this.left : this.right;
            }
        }

        public string Left
        {
            get
            {
                return this.left;
            }
        }

        public string Right
        {
            get
            {
                return this.right;
            }
        }

        private static SubExpressionDelimiter[] defaultList;
        public static SubExpressionDelimiter[] DefaultList { get { return defaultList; } }

        static SubExpressionDelimiter()
        {
            defaultList = new SubExpressionDelimiter[]
            {
                new SubExpressionDelimiter("(,)"),
                new SubExpressionDelimiter("[,]")
            };
        }
    }

    public class EquationMemberGroup
        : IEnumerable<EquationMember>
    {
        private string name;
        private Dictionary<Type, List<EquationMember>> dictionary;
        private IEnumerable<EquationMember> members;

        public EquationMemberGroup(string name, IEnumerable<EquationMember> members)
        {
            this.name = Name;
            this.members = members;
            this.dictionary = members.GroupBy(
                member => member.GetHighestDerivedType())
                .ToDictionary(
                    g => g.Key, g => new List<EquationMember>(
                        from m in g
                        select m));
        }

        public string Name { get { return name; } }

        public IEnumerable<EquationMember> this[Type memberType]
        {
            get
            {
                var list = dictionary[memberType];
                foreach (var member in list)
                {
                    yield return member;
                }
            }
        }

        private IEnumerable<T> GetList<T>() where T : EquationMember
        {
            var list = dictionary[typeof(T)];
            foreach (T member in list)
            {
                yield return member;
            }
        }

        public IEnumerable<EquationMember> Members
        {
            get
            {
                return this.members;
            }
        }

        public IEnumerator<EquationMember> GetEnumerator()
        {
            foreach (var member in members)
            {
                yield return member;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public IEnumerable<Operator> Operators
        {
            get
            {
                return GetList<Operator>();
            }
        }

        public IEnumerable<Function> Functions
        {
            get
            {
                return GetList<Function>();
            }
        }

        public IEnumerable<SubExpressionDelimiter> SubExpressionDelimiters
        {
            get
            {
                return GetList<SubExpressionDelimiter>();
            }
        }
    }

    public abstract class EquationValidator
    {
        private static EquationValidator defaultEquationValidator = new DefaultEquationValidator();

        public static EquationValidator Default
        {
            get
            {
                return defaultEquationValidator;
            }
        }

        public abstract void Validate(Equation equation);

        private class DefaultEquationValidator : EquationValidator
        {
            public DefaultEquationValidator()
            {
            }

            public override void Validate(Equation equation)
            {
                List<Exception> exceptions = new List<Exception>();
                this.ValidateSpaces(equation, exceptions);

                if (exceptions.Count > 0)
                {
                    throw new AggregateException(exceptions);
                }
            }

            private void ValidateSpaces(Equation equation, List<Exception> exceptions)
            {
                //string pattern = @"\d\s+\d";
                string pattern = @"\S\s+\S";
                string operatorPattern = "";// equation.Text.GetOperatorPattern(false);
                var matches = Regex.Matches(equation.Text, pattern, RegexOptions.IgnoreCase);
                foreach (Match match in matches)
                {
                    bool containsOperator = Regex.IsMatch(match.Value, operatorPattern);
                    if (!containsOperator)
                    {

                    }

                    // var  operatorMatches = Regex.Matches(match.Value, operatorPattern)
                }
            }
        }
    }

    [Serializable]
    public class ParsingException : Exception
    {
        public ParsingException() { }
        public ParsingException(string message) : base(message) { }
        public ParsingException(string message, Exception inner) : base(message, inner) { }
        protected ParsingException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }

        public string ParsingErrorMessage
        {
            get
            {
                if (this.Data.Count == 0)
                {
                    return string.Empty;
                }

                foreach (DictionaryEntry entry in this.Data)
                {
                    var matchInfo = (MatchInfo)entry.Value;
                    return entry.Key.ToString() + ": \"" + matchInfo.Value + "\" at index " + matchInfo.Index.ToString();
                }
                return "";
            }
        }
    }

    [Serializable]
    public class EquationValidationException : Exception
    {
        public EquationValidationException() { }
        public EquationValidationException(string message) : base(message) { }
        public EquationValidationException(string message, Exception inner) : base(message, inner) { }
        protected EquationValidationException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context)
            : base(info, context) { }
    }

    [Serializable, DebuggerDisplay(@"{errorType}: {value} at index {index} in {target}")]
    public class EquationValidationErrorInfo
    {
        private EquationValidationErrorType errorType;
        private object target;
        private int index;
        private string value;

        public EquationValidationErrorInfo(EquationValidationErrorType errorType, object target, int index, string value)
        {
            this.errorType = errorType;
            this.target = target;
            this.index = index;
            this.value = value;
        }

        public EquationValidationErrorType ErrorType
        {
            get
            {
                return this.errorType;
            }
        }
    }

    [Serializable]
    public enum EquationValidationErrorType : int
    {
        Unknown,
        InvalidSpacing,
        MultipleSequentialOperators,
        OperatorMissingValue,
        NumberTooLarge,
        TooManyDecimalPlaces,
        EmptySubExpression
    }

    public abstract class EquationParser
    {
        private static EquationParser _Default = new DefaultEquationParser();

        public static EquationParser Default
        {
            get { return _Default; }
        }

        public abstract void Validate(Equation equation, EquationMemberGroup members = null);

        public abstract Delegate Parse(Equation equation, EquationMemberGroup members = null);

        public static Delegate ParseEquation(Equation equation, EquationMemberGroup members)
        {
            return _Default.Parse(equation, members ?? CS.DefaultMemberGroup);
        }

        private class DefaultEquationParser : EquationParser
        {
            private class _InternalData
            {
                private int currentExpression = -1;

                public const char FunctionDelimiter = '$';
                public int FunctionLength { get { return 5; } }
                public int ValueLength { get { return 5; } }
                public string CurrentExpression { get { return FunctionDelimiter + currentExpression.ToString("000") + FunctionDelimiter; } }
                public string Text { get; set; }
                public Equation Equation { get; set; }
                public EquationMemberGroup Members { get; set; }
                public ParameterExpression[] Variables { get; set; }
                public List<Operator>[] Operators { get; set; }
                public Constant[] Constants { get; set; }
                public Function[] Functions { get; set; }
                public Dictionary<string, Expression> SubExpressions { get; set; }
                public Dictionary<string, Function> UsedFunctions { get; set; }
                public EquationTextFormatter Formatter { get; set; }
                public List<Tuple<ParameterExpression, Expression>> ConstantExpressions { get; set; }

                public _InternalData(Equation equation, EquationMemberGroup members)
                {
                    this.Equation = equation;
                    this.Members = members;
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

                public void RegisterFunction(Function function, int startIndex)
                {
                    currentExpression++;
                    string name = FunctionDelimiter + currentExpression.ToString("000") + FunctionDelimiter;
                    this.Text = this.Text.Remove(startIndex, function.Shorthand.Length);
                    this.Text = this.Text.Insert(startIndex, name);
                    UsedFunctions.Add(name, function);
                }

                public string RegisterExpression(Expression exp)
                {
                    currentExpression++;
                    string name = CurrentExpression;
                    SubExpressions.Add(name, exp);
                    return name;
                }

                private string operatorPattern;
                public string GetOperatorPattern(bool excludeSubtraction = false)
                {
                    if (operatorPattern != null && !excludeSubtraction)
                    {
                        return operatorPattern;
                    }

                    StringBuilder sb = new StringBuilder();
                    if (Operators.Count() > 0)
                    {
                        foreach (var cList in Operators)
                        {
                            foreach (var c in cList)
                            {
                                if (excludeSubtraction && ReferenceEquals(c, Operator.Subtraction))
                                {
                                    continue;
                                }

                                sb.Append(Regex.Escape(c.Shorthand));
                                sb.Append('|');
                            }
                        }

                        if (sb.Length > 0)
                        {
                            sb.Remove(sb.Length - 1, 1);
                        }

                    }

                    operatorPattern = sb.ToString();
                    return operatorPattern;
                }

            }
   
            private class EquationTextFormatter
            {
                private _InternalData data;
                private List<MatchInfo> list;
                private object target;
                private string text;

                public EquationTextFormatter(_InternalData data)
                {
                    this.data = data;
                }
                      
                private bool CheckIfInsideFunction(int index)
                {
                    if (index == 0 || index == this.text.Length - 1) return false;

                    if (text[index + 1] == _InternalData.FunctionDelimiter)
                    {
                        if (index >= data.FunctionLength + 2)
                        {
                            if (text[index - data.FunctionLength + 2] == _InternalData.FunctionDelimiter)
                            {
                                return true;
                            }
                        }
                        return false;
                    }

                    if (text[index] == _InternalData.FunctionDelimiter)
                    {
                        if (index + data.FunctionLength - 1 < this.text.Length)
                        {
                            if (text[index + data.FunctionLength - 1] == _InternalData.FunctionDelimiter)
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

                    pattern = @"\d+\.{1}\D";
                    matches = Regex.Matches(text, pattern);
                    foreach (Match match in matches)
                    {
                        list.Add(new MatchInfo(match.Index + match.Length - 2, 1, string.Empty));
                    }
                }

                private void MultiplyNumbers()
                {
                    // check for any case where a number is next to a non-number/operator and multiply them, such as 5(3X) => 5*(3*X)
                    string pattern = @"\d(" + this.data.Equation.Variables.GetRegexPattern() +
                                      "|" + this.data.Members.SubExpressionDelimiters.GetRegexPattern(true) + "|" +
                                      Regex.Escape(_InternalData.FunctionDelimiter.ToString()) + ")";
                    var matches = Regex.Matches(text, pattern, RegexOptions.IgnoreCase);
                    foreach (Match match in matches)
                    {
                        if (CheckIfInsideFunction(match.Index)) continue;
                        list.Add(new MatchInfo(match.Index + 1, 0, Operator.Multiplication.Shorthand));
                    }
  
                    // same thing, but with the number on the right side
                    pattern = "(" + this.data.Equation.Variables.GetRegexPattern() +
                                      "|" + this.data.Members.SubExpressionDelimiters.GetRegexPattern(false) + "|" +
                                      Regex.Escape(_InternalData.FunctionDelimiter.ToString()) + @")\d";
                    matches = Regex.Matches(text, pattern, RegexOptions.IgnoreCase);
                    foreach (Match match in matches)
                    {
                        if (CheckIfInsideFunction(match.Index)) continue;
                        list.Add(new MatchInfo(match.Index + 1, 0, Operator.Multiplication.Shorthand));
                    }
                }

                private void MultiplyVariables()
                {
                    string leftDelimiterPattern = "(" + Regex.Escape(_InternalData.FunctionDelimiter.ToString()) + "|" + 
                                                  this.data.Members.SubExpressionDelimiters.GetRegexPattern(true) + "|" +
                                                  this.data.Equation.Variables.GetRegexPattern() + ')';
                    foreach (var v in data.Equation.Variables)
                    {
                        string pattern = v.Shorthand + leftDelimiterPattern;
                        var matches = Regex.Matches(text, pattern, RegexOptions.IgnoreCase);
                        foreach (Match match in matches)
                        {
                            list.Add(new MatchInfo(match.Index + v.Shorthand.Length, 0, Operator.Multiplication.Shorthand));
                        }
                    }

                    string rightDelimiterPattern = "(" + Regex.Escape(_InternalData.FunctionDelimiter.ToString()) + "|" +
                                                   this.data.Members.SubExpressionDelimiters.GetRegexPattern(false) + ")"; 
                    foreach (var v in data.Equation.Variables)
                    {
                        string pattern = rightDelimiterPattern + v.Shorthand;
                        var matches = Regex.Matches(text, pattern, RegexOptions.IgnoreCase);
                        foreach (Match match in matches)
                        {
                            list.Add(new MatchInfo(match.Index + match.Length - v.Shorthand.Length, 0, Operator.Multiplication.Shorthand));
                        }
                    }
                }

                private void MultiplyAdjacentSubExpressionDelimiters()
                {
                    foreach (var delimiter in this.data.Members.SubExpressionDelimiters)
                    {
                        string pattern = Regex.Escape(delimiter.Right) + Regex.Escape(delimiter.Left);
                        var matches = Regex.Matches(text, pattern, RegexOptions.IgnoreCase);
                        foreach (Match match in matches)
                        {
                            list.Add(new MatchInfo(match.Index + delimiter.Right.Length, 0, Operator.Multiplication.Shorthand));
                        }
                    }
                }

                private void ReplaceDoubleNegatives()
                {
                    string doubleNegativePattern = @"-{2}";
                    var matches = Regex.Matches(text, doubleNegativePattern);
                    foreach (Match match in matches)
                    {
                        list.Add(new MatchInfo(match.Index, match.Length, Operator.Addition.Shorthand));
                    }
                }

                private void ReplaceNegativeOneWithNegativeOneTimesValue()
                {
                    string yoyo = data.GetOperatorPattern(true);
                    string negPattern = @"(" + yoyo + @")\s*" + Operator.Subtraction.Shorthand + @"{1}\s*";
                    var matches = Regex.Matches(this.text, negPattern);
                    int count = matches.Count - 1;
                    for (int i = count; i > -1; i--)
                    {
                        Match match = matches[i];
                        string replacementText = match.Value.Substring(0, 1) + "-1*";
                        this.list.Add(new MatchInfo(match.Index, match.Length, replacementText));
                    }
                }

                private void ZeroMinus()
                {
                    string negPattern213 = @"^\s*-";
                    // equationFragment = Regex.Replace(equationFragment, negPattern213, "0-");

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

                public string FormatText(object target, string text)
                {
                    this.target = target;
                    this.text = text;
                    this.list = new List<MatchInfo>();

                    ReplaceIncompleteDecimalNumbers();
                    ReplaceDoubleNegatives();
                    ApplyChanges();
                    this.list.Clear();

                    MultiplyNumbers();
                    MultiplyVariables();
                    MultiplyAdjacentSubExpressionDelimiters();
                    ApplyChanges();
                    this.list.Clear();

                    ReplaceNegativeOneWithNegativeOneTimesValue();
                    ApplyChanges();

                    return this.text;
                }

                //interface IFormatAction
                //{
                //    string Name { get; }

                //    string FormatString(string text);                    
                //}


                //private class ReplaceIncompleteDecimalNumbers : IFormatAction
                //{
                //    public string Name
                //    {
                //        get { return "Replace Incomplete Decimal Numbers"; }
                //    }

                //    public string FormatString(string text)
                //    {
                //        throw new NotImplementedException();
                //    }
                //}
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

            private class OperatorIndexListing
            {
                public int Index { get; set; }
                public Operator Operator { get; set; }
            }

            private enum ParsingExceptionType : int
            {
                Unknown = 0,
                InvalidSpacing = 1,
                MultipleOperators = 2
            }

            // check for operators at beginning and end of text without other values around it
            // check for blank parens like () or []

            private class EquationTextValidator
            {
                private EquationMemberGroup members;
                private List<EquationValidationErrorInfo> errors;
                private string text;
                private object target;

                public EquationTextValidator(EquationMemberGroup members)
                {
                    this.members = members;
                }

                public void Validate(object target)
                {
                    if (target is Constant)
                    {
                        var constant = (Constant)target;
                        if (constant.IsNumber) return;
                        this.text = constant.Value;
                    }
                    else if (target is Equation)
                    {
                        this.text = ((Equation)target).Text;
                    }

                    this.target = target;
                    this.errors = new List<EquationValidationErrorInfo>();

                    ValidateSpaces();
                    ValidateOperators();
                    CheckForSuccessiveDecimalPoints();
                    CheckForNumbersWithMultipleDecimalPlaces();
                    CheckForNumbersThatAreTooLarge();
                    CheckForEmptySubExpressions();
                    if (this.errors.Count > 0)
                    {
                        var error = new EquationValidationException();
                        error.Data.Add("Errors", errors);
                        throw error;
                    }
                }

                private void CheckForEmptySubExpressions()
                {
                    var delimiters = this.members.SubExpressionDelimiters;
                    foreach (var delimiter in delimiters)
                    {
                        string delimiterPattern = Regex.Escape(delimiter.Left) + @"\s*\.*\s*" + Regex.Escape(delimiter.Right);
                        var matches = Regex.Matches(text, delimiterPattern);
                        foreach (Match match in matches)
                        {
                            errors.Add(new EquationValidationErrorInfo(EquationValidationErrorType.EmptySubExpression, this.target, match.Index, match.Value));
                        }
                    } 
                }

                private void CheckForSuccessiveDecimalPoints()
                {
                    string duplicateDecimalPattern = @"\.+\s*\.+";
                    var matches = Regex.Matches(text, duplicateDecimalPattern);
                    foreach (Match match in matches)
                    {
                        errors.Add(new EquationValidationErrorInfo(EquationValidationErrorType.TooManyDecimalPlaces, this.target, match.Index, match.Value));
                    }
                }

                private void CheckForNumbersWithMultipleDecimalPlaces()
                {
                    string numberPattern = @"\d*(\.+\d+\.+)+(\d+\.*)*";
                    var matches = Regex.Matches(text, numberPattern);
                    foreach (Match match in matches)
                    {
                        errors.Add(new EquationValidationErrorInfo(EquationValidationErrorType.TooManyDecimalPlaces, this.target, match.Index, match.Value));
                    }
                }

                private void CheckForNumbersThatAreTooLarge()
                {
                    string numberPattern = @"\d+(?:\.{1}\d+)?";
                    var matches = Regex.Matches(text, numberPattern);
                    foreach (Match match in matches)
                    {
                        double d;
                        if (!Double.TryParse(match.Value, out d))
                        {
                            errors.Add(new EquationValidationErrorInfo(EquationValidationErrorType.NumberTooLarge, this.target, match.Index, match.Value));
                        }
                    }
                }

                private void ValidateOperators()
                {
                    string operatorPattern = @"(\s*(" + members.Operators.GetRegexPattern() + @")\s*){2,}";
                    //string operatorPattern = @"\s(?=" + members.Operators.GetRegexPattern() + @"){2,}";
                    string doubleNegative = Operator.Subtraction.Shorthand + Operator.Subtraction.Shorthand;
                    string plusMinus = Operator.Addition.Shorthand + Operator.Subtraction.Shorthand;
                    var matches = Regex.Matches(text, operatorPattern);
                    foreach (Match match in matches)
                    {
                        string matchTrimmed = match.Value.Replace(" ", string.Empty);
                        int length = Operator.Subtraction.Shorthand.Length;
                        if (matchTrimmed.Length > length && matchTrimmed.EndsWith(Operator.Subtraction.Shorthand))
                        {
                            string remainderString = matchTrimmed.Substring(0, matchTrimmed.Length - length);
                            bool isSingleOperator = this.members.Operators.FirstOrDefault(op => string.Equals(op.Shorthand, remainderString)) != null;
                            if (isSingleOperator)
                            {
                                continue;
                            }
                        }
                        errors.Add(new EquationValidationErrorInfo(EquationValidationErrorType.MultipleSequentialOperators, this.target, match.Index, match.Value));
                    }
                }

                private void ValidateSpaces()
                {
                    string pattern = @"\S\s+\S";
                    string operatorPattern = members.Operators.GetRegexPattern();
                    var matches = Regex.Matches(text, pattern, RegexOptions.IgnoreCase);
                    foreach (Match match in matches)
                    {
                        bool containsOperator = Regex.IsMatch(match.Value, operatorPattern);
                        if (!containsOperator)
                        {
                            errors.Add(new EquationValidationErrorInfo(EquationValidationErrorType.InvalidSpacing, this.target, match.Index, match.Value));
                        }
                    }
                }
            }

            public override Delegate Parse(Equation equation, EquationMemberGroup members = null)
            {
                if (members == null)
                {
                    members = CS.DefaultMemberGroup;
                }

                CS.Log.TraceInformation("Validating equation " + equation.Text.ToString());
                ValidateEquation(equation, members);

                CS.Log.TraceInformation("Parsing equation " + equation.Text.ToString());
                _InternalData data = new _InternalData(equation, members);

                data.Text = data.Text.Replace(" ", string.Empty);
                GetFunctionList(data);
                FixConstants(data);
                data.Text = data.Formatter.FormatText(data.Equation, data.Text);

                EvaluateSubExpressions(data);

                var finalExpression = ParseSubExpression(data, data.Text);
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

            public override void Validate(Equation equation, EquationMemberGroup members = null)
            {
                if (members == null)
                {
                    members = CS.DefaultMemberGroup;
                }

                ValidateEquation(equation, members);
            }

            private void ValidateEquation(Equation equation, EquationMemberGroup members)
            {
                var validator = new EquationTextValidator(members);
                validator.Validate(equation);
                if (equation.Constants.Count() > 0)
                {
                    foreach (var constant in equation.Constants)
                    {
                        validator.Validate(constant);
                    }
                }
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

            private bool IsValueADelimiter(_InternalData data, string text, int index, bool checkLeftDelimiter = true)
            {
                string subText = (index == 0) ? text : text.Substring(index);
                foreach (var delimiter in data.Members.SubExpressionDelimiters)
                {
                    if (subText.StartsWith(delimiter[checkLeftDelimiter])) return true;
                }
                return false;
            }

            private void PopulateSubExpressionList(string equation, SubExpressionDelimiter delimiter, List<List<SubExpressionIndex>> list)
            {
                int previousIndex = 0, depth = 0;
                for (; ; )
                {
                    int leftIndex = equation.IndexOf(delimiter.Left, previousIndex);
                    int rightIndex = equation.IndexOf(delimiter.Right, previousIndex);
                    if (leftIndex == -1 && rightIndex == -1) break;
                    int newIndex = 0;
                    if (leftIndex < rightIndex && leftIndex != -1)
                    {
                        newIndex = leftIndex;
                        if (list.Count == depth) list.Add(new List<SubExpressionIndex>());

                        list[depth].Add(new SubExpressionIndex(newIndex, -1, delimiter.Left, delimiter.Right));
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
                string name = "";
                var list = new List<List<SubExpressionIndex>>();
                foreach (var delimiter in data.Members.SubExpressionDelimiters)
                    PopulateSubExpressionList(data.Text, delimiter, list);


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
                        data.Text = data.Text.InsertReplace(leftIndex, difference, name);
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
                if (constant.IsNumber)
                {
                    return constant.Value;
                }
                else
                {
                    string formattedValue = data.Formatter.FormatText(constant, constant.Value);
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

            private void FixConstants(_InternalData data)
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
                                if (char.IsLetterOrDigit(charRight) || charRight == _InternalData.FunctionDelimiter || charRight == '.' || IsValueADelimiter(data, data.Text, rightIndex, true))
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

    [DebuggerDisplay("{Index}, {Length}:  {Value}"), Serializable]
    internal class MatchInfo
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


    internal static class CalculatorExtensions
    {
        internal static string GetRegexPattern(this IEnumerable<SubExpressionDelimiter> @this, bool leftDelimiter)
        {
            if (@this == null) return string.Empty;

            var builder = new StringBuilder();

            foreach (var member in @this)
            {
                builder.Append(Regex.Escape(member[leftDelimiter]));
                builder.Append('|');
            }

            int builderLength = builder.Length;
            if (builderLength > 0)
                builder.Remove(builderLength - 1, 1);

            return builder.ToString();
        }

        internal static string GetRegexPattern(this IEnumerable<EquationMember> @this)
        {
            if (@this == null) return string.Empty;

            var builder = new StringBuilder();

            foreach (var member in @this)
            {
                builder.Append(Regex.Escape(member.Shorthand));
                builder.Append('|');
            }

            int builderLength = builder.Length;
            if (builderLength > 0)
                builder.Remove(builderLength - 1, 1);

            return builder.ToString();
        }
    }
}
