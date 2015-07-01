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

    [DebuggerDisplay("{x}, {y}")]
    public struct PointD
    {
        private double x;
        private double y;

        public PointD(double x, double y)
        {
            this.x = x;
            this.y = y;
        }

        public double X
        {
            get
            {
                return this.x;
            }
        }

        public double Y
        {
            get
            {
                return this.y;
            }
        }

        public override string ToString()
        {
            return this.x.ToString() + "," + this.y.ToString();
        }
    }

    public interface ITextContainer
    {
        string Text { get; }
    }

    [DebuggerDisplay("{Text}")]
    public abstract class Equation : ITextContainer
    {
        private static Equation empty;

        public abstract string Text { get; }
        public abstract IEnumerable<Constant> Constants { get; set; }
        public abstract IEnumerable<Variable> Variables { get; set; }

        /// <summary>
        /// Represents an empty equation containing no text, an empty Constant array, and a Variable array with the standard X variable.
        /// </summary>
        public static Equation Empty
        {
            get
            {
                return empty;
            }
        }

        static Equation()
        {
            empty = Create(string.Empty, null, null);
        }

        public static Equation Create(string text, IEnumerable<Constant> constants = null, IEnumerable<Variable> variables = null)
        {
            return new DefaultEquation(text ?? string.Empty,
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

    public class EquationMemberGroup
        : IEnumerable<EquationMember>
    {
        private string name;
        private Dictionary<Type, List<EquationMember>> dictionary;       

        public EquationMemberGroup(string name, IEnumerable<EquationMember> members)
        {
            this.name = Name;        
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
                if (this.dictionary.ContainsKey(memberType))
                {
                    var list = dictionary[memberType];
                    foreach (var member in list)
                    {
                        yield return member;
                    }
                }
                else
                {
                    yield break;
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

        public IEnumerator<EquationMember> GetEnumerator()
        {
            foreach (var entry in this.dictionary)
            {
                foreach (var member in entry.Value)
                {
                    yield return member;
                }           
            }
        }

        public void Add(EquationMember member)
        {
            var memberType = member.GetHighestDerivedType();
            if (this.dictionary.ContainsKey(memberType))
            {
                this.dictionary[memberType].Add(member);
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
    
    [Serializable, DebuggerDisplay(@"{errorType}: {value} at index {index} in {target}")]
    public class EquationValidationErrorInfo
    {
        private EquationValidationErrorType errorType;
        private ITextContainer target;
        private int index;
        private string value;

        public EquationValidationErrorInfo(EquationValidationErrorType errorType, ITextContainer target, int index, string value)
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

        public int Index
        {
            get
            {
                return this.index;
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

    public class ValidationEventArgs<T> : EventArgs
    {
        private T value;
        private T replacementValue;
        private bool success;

        public ValidationEventArgs(T valueToValidate)
        {
            this.value = valueToValidate;
            this.replacementValue = valueToValidate;
            this.success = true;
        }

        public T Value
        {
            get
            {
                return this.value;
            }
        }

        public bool Success
        {
            get
            {
                return this.success;
            }
        }

        public T ReplacementValue
        {
            get
            {
                return this.replacementValue;
            }
        }

        public void SetResults(bool success, T replacementValue)
        {
            this.success = success;
            if (!success)
            {
                this.replacementValue = replacementValue;
            }
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

        internal static void TempEvaluate<T>(this IEnumerable<T> @this, string text)
        {
            if (@this.Count() == 0)
            {
                return;
            }

            Type type = typeof(T);
            var poopo = type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance);
            var lolz = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Where(fi => fi.FieldType == typeof(int));

            if (lolz.Count() == 0)
            {
                return;
            }
            Console.WriteLine("Evaluating List({0}) on \"{1}\"", lolz.Count(), text);
            try
            {
                var builder = new StringBuilder();

                foreach (T obj in @this)
                {
                    string substrings = string.Join("  |  ", from lol in lolz
                                                             let value = (int)lol.GetValue(obj)
                                                             select value.ToString() + ": " + text.Substring(value));
                    Console.WriteLine(substrings);
                }
            }
            catch
            {
                Console.WriteLine("Error");
            }
        }

        internal static List<int> AllIndexesOf(this string @this, string value, bool ignoreCase)
        {
            var indexes = new List<int>(4);
            if (ignoreCase)
            {
                @this = @this.ToUpper();
                value = value.ToUpper();
            }
            int previousIndex = 0;
            for (; ; )
            {
                int index = @this.IndexOf(value, previousIndex);
                if (index == -1) break;
                indexes.Add(index);
                previousIndex = index + value.Length;
            }
            return indexes;
        }
    }
}
