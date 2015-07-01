
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

    public abstract class EquationParser
    {
        private static EquationParser defaultParser = new DefaultEquationParser();

        public static EquationParser Default
        {
            get { return defaultParser; }
        }

        public abstract void Validate(Equation equation, EquationMemberGroup members = null);

        public abstract Delegate Parse(Equation equation, EquationMemberGroup members = null);

        public static void ValidateEquation(Equation equation, EquationMemberGroup members = null)
        {
            defaultParser.Validate(equation, members ?? CS.DefaultMemberGroup);
        }

        public static Delegate ParseEquation(Equation equation, EquationMemberGroup members = null)
        {
            return defaultParser.Parse(equation, members ?? CS.DefaultMemberGroup);
        }

        private class DefaultEquationParser : EquationParser
        {
            private struct ReplacementMember
            {
                private int index;
                private EquationMember target;

                public int Index { get { return this.index; } }
                public EquationMember Target { get { return this.target; } }

                public ReplacementMember(int index, EquationMember target)
                {
                    this.index = index;
                    this.target = target;
                }

                public override string ToString()
                {
                    return this.index.ToString() + ": " + this.target.ToString();
                }
            }

            private class SubExpressionIndex
            {
                private bool isRightEndPresent;

                public int X { get; set; }
                public int Y { get; set; }
                public SubExpressionDelimiter Delimiter { get; set; }
                public string Left { get { return this.Delimiter.Left; } }
                public string Right { get { return this.Delimiter.Right; } }
                public bool IsRightEndPresent { get { return this.isRightEndPresent; } }

                public SubExpressionIndex(SubExpressionDelimiter delimiter, int x, int y)
                {
                    this.X = x;
                    this.Y = y;
                    this.Delimiter = delimiter;
                    if (y > -1)
                    {
                        this.isRightEndPresent = true;
                    }
                }

                public void SetUnfoundRightEnd(int newIndex)
                {
                    this.Y = newIndex;
                    this.isRightEndPresent = false;
                }

                public void SetFoundRightEnd(int newIndex)
                {
                    this.Y = newIndex;
                    this.isRightEndPresent = true;
                }

                public string GetSubExpressionText(string text)
                {
                    int leftIndex = this.X + this.Delimiter.Left.Length;
                    return text.Substring(leftIndex, this.Y - leftIndex);
                }

                public int GetTotalLength()
                {
                    int totalLength = this.Y - this.X +
                                      this.Delimiter.Left.Length +
                                      (this.isRightEndPresent ? this.Right.Length : 0);
                    return totalLength;
                }
            }

            private class OperatorIndexListing
            {
                public int Index { get; set; }
                public Operator Operator { get; set; }
            }

            private class ValidationData
            {
                private List<ReplacementMember> replacements;
                private List<List<SubExpressionIndex>> subExpressions;
                private EquationMemberGroup members;
                private object target;
                private string text;
                private bool needsEvaluated;

                public ValidationData(object target, bool needsEvaluated)
                {
                    this.target = target;
                    this.needsEvaluated = needsEvaluated;
                }

                public List<List<SubExpressionIndex>> SubExpressions
                {
                    get
                    {
                        return this.subExpressions;
                    }
                    set
                    {
                        this.subExpressions = value;
                    }
                }

                public object Target
                {
                    get
                    {
                        return this.target;
                    }
                }

                public EquationMemberGroup Members
                {
                    get
                    {
                        return this.members;
                    }
                    set
                    {
                        this.members = value;
                    }
                }

                public bool NeedsEvaluated
                {
                    get
                    {
                        return this.needsEvaluated;
                    }
                }

                public string Text
                {
                    get
                    {
                        return this.text;
                    }
                }

                public List<ReplacementMember> Replacements
                {
                    get
                    {
                        return this.replacements;
                    }
                }

                public void SetData(string text, List<ReplacementMember> replacements)
                {
                    this.text = text;
                    this.replacements = replacements;
                }
            }

            private class EquationTextFormatter
            {
                private ExpressionEvaluator evaluator;
                private ValidationData data;
                private List<MatchInfo> list;
                private object target;
                private string text;
                private string constantAndVariablePattern;

                public EquationTextFormatter(ExpressionEvaluator evaluator)
                {
                    this.evaluator = evaluator;
                    this.data = evaluator.Data;
                    this.constantAndVariablePattern = GetConstantAndVariablePattern();
                }

                private string GetConstantAndVariablePattern()
                {
                    string variablePattern = this.evaluator.Equation.Variables.GetRegexPattern();
                    string constantPattern = this.evaluator.Equation.Constants.GetRegexPattern();

                    string pattern = variablePattern + (constantPattern != string.Empty ? '|' + constantPattern : string.Empty);
                    return pattern;
                }

                private string GetOperatorPattern(bool excludeSubtraction = false)
                {
                    StringBuilder sb = new StringBuilder();
                    var Operators = this.data.Members.Operators;
                    if (Operators.Count() > 0)
                    {
                        foreach (var c in Operators)
                        {
                            if (excludeSubtraction && ReferenceEquals(c, Operator.Subtraction))
                            {
                                continue;
                            }

                            sb.Append(Regex.Escape(c.Shorthand));
                            sb.Append('|');
                        }

                        if (sb.Length > 0)
                        {
                            sb.Remove(sb.Length - 1, 1);
                        }
                    }

                    return sb.ToString();
                }

                //private bool CheckIfInsideFunction(int index)
                //{
                //    if (index == 0 || index == this.text.Length - 1) return false;

                //    if (text[index + 1] == ExpressionEvaluator.FunctionDelimiter)
                //    {
                //        if (index >= ExpressionEvaluator.FunctionLength + 2)
                //        {
                //            if (text[index - ExpressionEvaluator.FunctionLength + 2] == ExpressionEvaluator.FunctionDelimiter)
                //            {
                //                return true;
                //            }
                //        }
                //        return false;
                //    }

                //    if (text[index] == ExpressionEvaluator.FunctionDelimiter)
                //    {
                //        if (index + ExpressionEvaluator.FunctionLength - 1 < this.text.Length)
                //        {
                //            if (text[index + ExpressionEvaluator.FunctionLength - 1] == ExpressionEvaluator.FunctionDelimiter)
                //            {
                //                return true;
                //            }
                //        }
                //        return false;
                //    }
                //    return false;
                //}

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
                    string pattern = @"\d(" + this.constantAndVariablePattern + '|' +
                                      this.data.Members.SubExpressionDelimiters.GetRegexPattern(true) + '|' +
                                      Regex.Escape(ExpressionEvaluator.FunctionDelimiter.ToString()) + ")";
                    MultiplyNumbers_WithPattern(pattern);

                    // same thing, but with the number on the right side
                    pattern = "(" + this.constantAndVariablePattern + '|' +
                                    this.data.Members.SubExpressionDelimiters.GetRegexPattern(false) + @")\d";
                    MultiplyNumbers_WithPattern(pattern);
                }

                private void MultiplyNumbers_WithPattern(string pattern)
                {
                    var matches = Regex.Matches(text, pattern, RegexOptions.IgnoreCase);
                    foreach (Match match in matches)
                    {
                        list.Add(new MatchInfo(match.Index + 1, 0, Operator.Multiplication.Shorthand));
                    }
                }

                private void MultiplyVariables()
                {
                    string leftPattern = "(" + this.constantAndVariablePattern + '|' +
                                                  this.data.Members.SubExpressionDelimiters.GetRegexPattern(false) + ')';
                    string rightPattern = @"\A(" + this.constantAndVariablePattern + '|' +
                                                Regex.Escape(ExpressionEvaluator.FunctionDelimiter.ToString()) + '|' +
                                                this.data.Members.SubExpressionDelimiters.GetRegexPattern(true) + ')';

                    var matches = Regex.Matches(this.text, leftPattern, RegexOptions.IgnoreCase);
                    foreach (Match match in matches)
                    {
                        int rightIndex = match.Index + match.Length;
                        if (rightIndex == this.text.Length)
                        {
                            break;
                        }

                        bool isMatch = Regex.IsMatch(this.text.Substring(rightIndex), rightPattern, RegexOptions.IgnoreCase);
                        if (isMatch)
                        {
                            list.Add(new MatchInfo(rightIndex, 0, Operator.Multiplication.Shorthand));
                        }
                    }


                    //foreach (var v in this.evaluator.Equation.Variables)
                    //{
                    //    string pattern = v.Shorthand + leftPattern;
                    //    var matches = Regex.Matches(text, pattern, RegexOptions.IgnoreCase);
                    //    foreach (Match match in matches)
                    //    {
                    //        list.Add(new MatchInfo(match.Index + v.Shorthand.Length, 0, Operator.Multiplication.Shorthand));
                    //    }
                    //}

                    //string rightDelimiterPattern = "(" + Regex.Escape(ExpressionEvaluator.FunctionDelimiter.ToString()) + "|" +
                    //                               this.data.Members.SubExpressionDelimiters.GetRegexPattern(false) + ")";
                    //foreach (var v in this.evaluator.Equation.Variables)
                    //{
                    //    string pattern = rightDelimiterPattern + v.Shorthand;
                    //    var matches = Regex.Matches(text, pattern, RegexOptions.IgnoreCase);
                    //    foreach (Match match in matches)
                    //    {
                    //        list.Add(new MatchInfo(match.Index + match.Length - v.Shorthand.Length, 0, Operator.Multiplication.Shorthand));
                    //    }
                    //}
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
                    string yoyo = this.GetOperatorPattern(true);
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

                public string FormatText(ValidationData data, string text)
                {
                    this.data = data;
                    this.target = data.Target;
                    this.text = text;
                    this.list = new List<MatchInfo>();

                    ReplaceIncompleteDecimalNumbers();
                    ReplaceDoubleNegatives();
                    ApplyChanges();
                    this.list.Clear();

                    MultiplyNumbers();
                    MultiplyVariables();
                    //MultiplyAdjacentSubExpressionDelimiters();
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

            // validate argument count, self-referencing constant, referencing later constants
            private class EquationTextValidator
            {
                private Equation equation;
                private EquationMemberGroup members;
                private List<EquationValidationErrorInfo> errors;
                private string text;
                private ITextContainer target;
                private List<Point> spaceIndexes;
                private List<ReplacementMember> replacements;

                public EquationTextValidator(Equation equation, EquationMemberGroup members)
                {
                    this.equation = equation;
                    this.members = members;
                }

                public ValidationData Validate(ITextContainer target)
                {
                    this.text = target.Text;

                    double d;
                    if (Double.TryParse(this.text.Trim(), out d))
                    {
                        return new ValidationData(target, false);
                    }

                    this.target = target;
                    this.errors = new List<EquationValidationErrorInfo>();
                    this.replacements = new List<ReplacementMember>();

                    ValidationData validationData = new ValidationData(target, true);
                    validationData.Members = this.members;

                    ValidateAndReplaceSpaces();
                    FindAndReplaceFunctions();
                    ValidateRemainingText();

                    var subExpressions = this.GetSubExpressions();
                    ValidateSubExpressions(subExpressions);

                    validationData.SubExpressions = this.GetSubExpressions();

                    if (!IsEmptySubExpression(this.text, 0))
                    {
                        CheckForLeadingOperators(this.text, 0);
                        CheckForTrailingOperators(this.text, 0);
                        CheckForInvalidTrailingDecimalPoints(this.text, 0);

                        ValidateOperators();
                        CheckForNumbersThatAreTooLarge();

                        CheckForSuccessiveDecimalPoints();
                        CheckForDecimalsBetweenNonNumbers();
                        CheckForNumbersWithMultipleDecimalPlaces();
                    }

                    if (this.errors.Count > 0)
                    {
                        var error = new EquationValidationException();
                        errors.Sort((e1, e2) => e1.Index > e2.Index ? 1 : e1.Index < e2.Index ? -1 : 0);
                        error.Data.Add("Errors", errors);
                        throw error;
                    }
                    else
                    {
                        validationData.SetData(this.text, this.replacements);
                        return validationData;
                    }
                }

                private void AddError(EquationValidationErrorType errorType, int index, string value)
                {
                    int position = GetIndex(index);

                    // keep one error per character index
                    if (errors.FirstOrDefault(e => e.Index == position) == null)
                    {
                        errors.Add(new EquationValidationErrorInfo(
                              errorType,
                              this.target,
                              position,
                              value));
                    }
                }

                private int GetIndex(int index)
                {
                    int position = index;

                    for (int i = replacements.Count - 1; i > -1; i--)
                    {
                        var replacement = replacements[i];
                        if (position >= replacement.Index)
                        {
                            position += replacement.Target.Shorthand.Length - 1;
                        }
                        else
                        {
                            break;
                        }
                    }

                    foreach (var point in spaceIndexes)
                    {
                        if (position >= point.X)
                        {
                            position += point.Y;
                        }
                        else
                        {
                            break;
                        }
                    }
                    return position;
                }

                private void FindAndReplaceFunctions()
                {
                    var functions = this.members.Functions.OrderByDescending(f => f.Shorthand.Length);
                    foreach (var function in functions)
                    {
                        var indexes = this.text.AllIndexesOf(function.Shorthand, true);
                        int count = indexes.Count;
                        if (count > 0)
                        {
                            for (int i = count - 1; i > -1; i--)
                            {
                                int index = indexes[i];
                                int rightIndex = index + function.Shorthand.Length;
                                string rightOfFunction = (rightIndex != this.text.Length) ? this.text.Substring(rightIndex) : string.Empty;
                                if (ContainsDelimiter(rightOfFunction, true, true, false))
                                {
                                    replacements.Add(new ReplacementMember(index, function));
                                    this.text = this.text.InsertReplace(index, function.Shorthand.Length, ExpressionEvaluator.FunctionDelimiter.ToString());
                                }
                                else
                                {
                                    AddError(EquationValidationErrorType.FunctionWithoutSubExpression, index, function.Shorthand);
                                }
                            }
                        }
                    }
                }

                private string GetConstantPattern()
                {
                    if (this.target is Equation)
                    {
                        return this.equation.Constants.GetRegexPattern();
                    }

                    int index = 0;
                    int count = this.equation.Constants.Count();
                    var builder = new StringBuilder();
                    for (int i = 0; i < count; i++)
                    {
                        var constant = this.equation.Constants.ElementAt(i);
                        if (ReferenceEquals(this.equation.Constants.ElementAt(i), this.target))
                        {
                            if (builder.Length > 0)
                            {
                                builder.Remove(builder.Length - 1, 1);
                            }
                            break;
                        }
                        else
                        {
                            builder.Append(constant.Shorthand);
                            builder.Append('|');
                        }
                    }

                    return builder.ToString();
                }

                private void ValidateRemainingText()
                {
                    string pattern = @"([A-Z]|[a-z])+";
                    string constantPattern = GetConstantPattern();
                    string acceptableTextPattern = @"\A(" + this.equation.Variables.GetRegexPattern() +
                        (constantPattern != string.Empty ? "|" + constantPattern + ")" : ")") + @"+\z";
                    var matches = Regex.Matches(this.text, pattern, RegexOptions.IgnoreCase);
                    foreach (Match match in matches)
                    {
                        if (!Regex.IsMatch(match.Value, acceptableTextPattern, RegexOptions.IgnoreCase))
                        {
                            AddError(
                                EquationValidationErrorType.InvalidText,
                                match.Index,
                                match.Value);
                        }
                    }
                }

                private List<List<SubExpressionIndex>> GetSubExpressions()
                {
                    var indexes = new List<Tuple<int, int, SubExpressionDelimiter>>();
                    foreach (var delimiter in this.members.SubExpressionDelimiters)
                    {
                        for (int i = 0; i < 2; i++)
                        {
                            int previousIndex = 0;
                            for (; ; )
                            {
                                int index = this.text.IndexOf(delimiter[i], previousIndex);
                                if (index == -1) break;
                                indexes.Add(new Tuple<int, int, SubExpressionDelimiter>(index, i, delimiter));
                                previousIndex = index + delimiter[i].Length;
                            }
                        }
                    }

                    indexes.Sort((t1, t2) => t1.Item1 > t2.Item1 ? 1 : t1.Item1 < t2.Item1 ? -1 : 0);
                    int depth = 0;
                    var list = new List<List<SubExpressionIndex>>();

                    foreach (var index in indexes)
                    {
                        if (index.Item2 == 0)
                        {
                            if (list.Count <= depth)
                            {
                                list.Add(new List<SubExpressionIndex>());
                            }
                            list[depth].Add(new SubExpressionIndex(index.Item3, index.Item1, -1));
                            depth++;
                        }
                        else
                        {
                            depth--;
                            if (depth == -1)
                            {
                                AddError(
                                     EquationValidationErrorType.MissingSubExpressionDelimiter,
                                     index.Item1,
                                     index.Item3.Right);
                                depth++;
                                continue;
                            }

                            var currentList = list[depth];
                            var lastIndex = currentList[currentList.Count - 1];
                            if (!ReferenceEquals(lastIndex.Delimiter, index.Item3))
                            {
                                AddError(
                                     EquationValidationErrorType.NonMatchingSubExpressionDelimiters,
                                     lastIndex.X,
                                     this.text.Substring(lastIndex.X, index.Item1 + index.Item3.Left.Length - lastIndex.X));
                            }

                            lastIndex.SetFoundRightEnd(index.Item1);
                        }
                    }

                    foreach (var depthList in list)
                    {
                        foreach (var index in depthList)
                        {
                            if (index.Y == -1)
                            {
                                index.SetUnfoundRightEnd(this.text.Length);
                            }
                        }
                    }

                    return list;
                }

                private void ValidateSubExpressions(List<List<SubExpressionIndex>> subExpressions)
                {
                    foreach (var depthList in subExpressions)
                    {
                        foreach (var subExpression in depthList)
                        {

                            int leftIndex = subExpression.X + subExpression.Left.Length;
                            string subExpressionText = subExpression.GetSubExpressionText(this.text);
                            if (IsEmptySubExpression(subExpressionText, leftIndex))
                            {
                                continue;
                            }

                            this.CheckForLeadingOperators(subExpressionText, leftIndex);
                            this.CheckForTrailingOperators(subExpressionText, leftIndex);
                            this.CheckForInvalidTrailingDecimalPoints(subExpressionText, leftIndex);
                        }
                    }
                }

                private bool IsEmptySubExpression(string subExpression, int index)
                {
                    if (subExpression == string.Empty) // trimmedText == ".") // include the point for cases like "(." at the end of strings
                    {
                        AddError(
                            EquationValidationErrorType.EmptySubExpression,
                            index,
                            subExpression);
                        return true;
                    }
                    return false;
                }

                private void CheckForLeadingOperators(string subExpression, int index)
                {
                    string pattern = @"\A(" + members.Operators.GetRegexPattern() + ")";
                    var matches = Regex.Matches(subExpression, pattern);
                    foreach (Match match in matches)
                    {
                        if (!match.Value.StartsWith(Operator.Subtraction.Shorthand) || CheckForLeadingOperators_CheckForLoneNegativeSign(subExpression, match))
                        {
                            AddError(
                                EquationValidationErrorType.LeadingOperator,
                                index + match.Index,
                                match.Value);
                        }
                    }
                }

                private bool CheckForLeadingOperators_CheckForLoneNegativeSign(string subExpression, Match match)
                {
                    if (match.Index + match.Length < subExpression.Length - 1)
                    {
                        char nextChar = subExpression[match.Index + match.Length];
                        if (!char.IsDigit(nextChar) && nextChar != '.')
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }

                private void CheckForTrailingOperators(string subExpression, int index)
                {
                    string pattern = "(" + members.Operators.GetRegexPattern() + @")\z";
                    var matches = Regex.Matches(subExpression, pattern);
                    foreach (Match match in matches)
                    {
                        AddError(
                            EquationValidationErrorType.TrailingOperator,
                            index + match.Index,
                            match.Value);
                    }
                }

                private void CheckForInvalidTrailingDecimalPoints(string subExpression, int index)
                {
                    string pattern = @"\D\.\z";
                    var matches = Regex.Matches(subExpression, pattern);
                    foreach (Match match in matches)
                    {
                        int decimalIndex = match.Value.IndexOf(@".");
                        AddError(
                            EquationValidationErrorType.TrailingDecimal,
                            index + match.Index + decimalIndex,
                            match.Value.Substring(decimalIndex));
                    }
                }

                private void CheckForSuccessiveDecimalPoints()
                {
                    string duplicateDecimalPattern = @"\.{2,}";
                    var matches = Regex.Matches(text, duplicateDecimalPattern);
                    foreach (Match match in matches)
                    {
                        AddError(
                            EquationValidationErrorType.TooManyDecimalPlaces,
                            match.Index,
                            match.Value);
                    }
                }

                private void CheckForDecimalsBetweenNonNumbers()
                {
                    string decimalPattern = @"\D\.\D";
                    var matches = Regex.Matches(text, decimalPattern);
                    foreach (Match match in matches)
                    {
                        AddError(
                            EquationValidationErrorType.DecimalBetweenNonNumbers,
                            match.Index,
                            match.Value);
                    }
                }

                private void CheckForNumbersWithMultipleDecimalPlaces()
                {
                    string numberPattern = @"\d*(\.+\d+\.+)+(\d+\.*)*";
                    var matches = Regex.Matches(text, numberPattern);
                    foreach (Match match in matches)
                    {
                        AddError(
                            EquationValidationErrorType.TooManyDecimalPlaces,
                            match.Index,
                            match.Value);
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
                            AddError(
                                EquationValidationErrorType.NumberTooLarge,
                                match.Index,
                                match.Value);
                        }
                    }
                }

                private void ValidateOperators()
                {
                    string operatorPattern = @"((" + members.Operators.GetRegexPattern() + @")){2,}";
                    //string operatorPattern = @"(?=" + members.Operators.GetRegexPattern() + @"){2,}";
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
                        AddError(
                            EquationValidationErrorType.MultipleSequentialOperators,
                            match.Index,
                            match.Value);
                    }
                }

                private string GetSubExpressionPattern()
                {
                    StringBuilder builder = new StringBuilder();
                    foreach (var delimiter in this.members.SubExpressionDelimiters)
                    {
                        builder.Append(delimiter.Left);
                        builder.Append('|');
                        builder.Append(delimiter.Right);
                    }
                    return builder.ToString();
                }

                private void ValidateAndReplaceSpaces()
                {
                    string pattern = @"\S\s+";
                    var matches = Regex.Matches(this.text, pattern);
                    this.spaceIndexes = new List<Point>();
                    int lastIndex = 0;
                    foreach (Match match in matches)
                    {
                        string left = this.text.Substring(lastIndex, match.Index + 1 - lastIndex);
                        lastIndex = match.Index + match.Length;
                        this.spaceIndexes.Add(new Point(match.Index + 1, match.Length - 1));

                        if (lastIndex == this.text.Length)
                        {
                            continue;
                        }

                        string right = this.text.Substring(lastIndex);
                        if (!ValidateSpace(left, right))
                        {
                            errors.Add(new EquationValidationErrorInfo(
                                EquationValidationErrorType.InvalidSpacing,
                                this.target,
                                match.Index + 1,
                                match.Value.Substring(1)));
                        }
                    }
                    for (int i = 0; i < this.text.Length; i++)
                    {
                        if (this.text[i] != ' ')
                        {
                            if (i > 0)
                            {
                                this.spaceIndexes.Insert(0, new Point(0, i));
                            }

                            break;
                        }
                    }

                    this.text = this.text.Replace(" ", string.Empty);
                }

                private bool ValidateSpace(string left, string right)
                {
                    // if either string is an operator, this is valid as far as spacing goes
                    if (ContainsOperator(left, right)) return true;

                    // if either string is a function argument seperator, the other value is valid (situations like "(5 +  , 55)" will be accounted for in validating operators)
                    if (ContainsArgumentDelimiter(left, false)) return true;
                    if (ContainsArgumentDelimiter(right, true)) return true;

                    // if left delimiter is in left spot, any value is valid after
                    if (ContainsDelimiter(left, false, true, false)) return true;

                    // if right edleimiter is in right spot, any value is valid after
                    if (ContainsDelimiter(right, true, false, false)) return true;



                    // if left delimiter is in right spot, any value other than an operator is invalid - but operators were already checked prior to this
                    if (ContainsDelimiter(right, true, true, false)) return false;

                    char lastLeft = left[left.Length - 1];

                    // if char is number like "5  " or ends with a period after a number, like "5.   " which will be turned into "5.0" later
                    //if (char.IsNumber(lastLeft) || (lastLeft == '.' && left.Length > 1 && char.IsNumber(left[left.Length - 2])))
                    //{
                    //    if (ContainsDelimiter(right, true, true, false)) return false;

                    //    if (ContainsDelimiter(right, true, false, true)) return true;
                    //}

                    return false;
                }

                private bool ContainsOperator(string left, string right)
                {
                    foreach (var op in this.members.Operators)
                    {
                        if (left.EndsWith(op.Shorthand)) return true;
                        if (right.StartsWith(op.Shorthand)) return true;
                    }
                    return false;
                }

                private bool ContainsArgumentDelimiter(string text, bool startsWith)
                {
                    bool hasArgumentSeperator = startsWith ?
                         text.StartsWith(ExpressionEvaluator.FunctionArgumentDelimiter.ToString()) :
                         text.EndsWith(ExpressionEvaluator.FunctionArgumentDelimiter.ToString());
                    return hasArgumentSeperator;
                }

                private bool ContainsDelimiter(string text, bool startsWith, bool leftDelimiter, bool includeArgumentDelimiter)
                {
                    if (includeArgumentDelimiter)
                    {
                        bool hasArgumentSeperator = ContainsArgumentDelimiter(text, startsWith);
                        if (hasArgumentSeperator) return true;
                    }

                    foreach (var delimiter in this.members.SubExpressionDelimiters)
                    {
                        bool hasDelimiter = startsWith ? text.StartsWith(delimiter[leftDelimiter]) : text.EndsWith(delimiter[leftDelimiter]);
                        if (hasDelimiter) return true;
                    }
                    return false;
                }

                private void ValidateSpaces()
                {
                    string pattern = @"\S\s+\S";
                    string operatorPattern = members.Operators.GetRegexPattern();
                    string leftPattern = @"\A(" + this.members.SubExpressionDelimiters.GetRegexPattern(true) + "|" + Regex.Escape(ExpressionEvaluator.FunctionArgumentDelimiter.ToString()) + ")";
                    string rightPattern = @"(" + this.members.SubExpressionDelimiters.GetRegexPattern(false) + "|" + Regex.Escape(ExpressionEvaluator.FunctionArgumentDelimiter.ToString()) + @")\Z";

                    var matches = Regex.Matches(this.text, pattern, RegexOptions.IgnoreCase);
                    foreach (Match match in matches)
                    {
                        bool containsOperator = Regex.IsMatch(match.Value, operatorPattern);
                        if (!containsOperator)
                        {
                            if (!Regex.IsMatch(match.Value, leftPattern, RegexOptions.IgnoreCase) &&
                                 !Regex.IsMatch(match.Value, rightPattern, RegexOptions.IgnoreCase))
                            {
                                errors.Add(new EquationValidationErrorInfo(EquationValidationErrorType.InvalidSpacing, this.target, match.Index, match.Value));
                            }
                        }
                    }
                }
            }

            private class ExpressionEvaluator
            {
                public const char FunctionDelimiter = '$';
                public const char FunctionArgumentDelimiter = ',';
                public static int FunctionLength
                {
                    get
                    {
                        return 5;
                    }
                }

                private ValidationData data;
                private Equation equation;
                private EquationMemberGroup members;
                private List<Operator>[] operators;
                private ParameterExpression[] variables;
                private Dictionary<string, Function> usedFunctions;
                private Dictionary<Constant, string> constantExpressions;
                private Dictionary<string, Expression> evaluatedExpressions;
                private EquationTextFormatter formatter;
                private List<List<SubExpressionIndex>> subExpressions;
                private int currentExpression = -1;
                private string text;

                public ExpressionEvaluator(Equation equation, EquationMemberGroup members)
                {
                    this.equation = equation;
                    this.members = members;
                    //this.functions = (from f in this.members.OfType<Function>()
                    //                  orderby f.Shorthand.Length descending
                    //                  select f).ToArray();
                    this.operators = (from o in this.members.OfType<Operator>()
                                      group o by o.Order into oGroup
                                      orderby oGroup.Key descending
                                      select new List<Operator>(from g in oGroup select g)).ToArray();
                    this.variables = equation.Variables.Select(v => Expression.Parameter(typeof(double), v.Shorthand)).ToArray();
                    this.evaluatedExpressions = new Dictionary<string, Expression>();
                    this.usedFunctions = new Dictionary<string, Function>();
                    this.constantExpressions = new Dictionary<Constant, string>();
                    this.formatter = new EquationTextFormatter(this);
                }

                public string CurrentExpression
                {
                    get
                    {
                        return FunctionDelimiter + currentExpression.ToString("000") + FunctionDelimiter;
                    }
                }

                public ValidationData Data
                {
                    get
                    {
                        return this.data;
                    }
                }

                public Equation Equation
                {
                    get
                    {
                        return this.equation;
                    }
                }

                public ParameterExpression[] Variables
                {
                    get
                    {
                        return this.variables;
                    }
                }

                public Expression EvaluateTextExpression(ValidationData data)
                {
                    this.text = data.Text;
                    this.data = data;

                    this.text = this.formatter.FormatText(data, this.text);
                    this.ProcessReplacements();
                    this.FixConstants();

                    this.FindSubExpressions();
                    if (this.subExpressions.Count > 0)
                    {
                        for (int depth = this.subExpressions.Count - 1; depth > -1; depth--)
                        {
                            var subExpressionList = this.subExpressions[depth];
                            subExpressionList.Sort((s1, s2) => s1.X < s2.X ? 1 : s1.X > s2.X ? -1 : 0);
                            foreach (var subExpression in subExpressionList)
                            {
                                this.EvaluateSubExpression(subExpression, depth);
                            }
                        }
                    }

                    var expression = this.ParseExpression(this.text);

                    if (this.data.Target is Constant)
                    {
                        string name = this.RegisterExpression(expression);
                        this.constantExpressions.Add((Constant)data.Target, name);
                    }

                    return expression;
                }

                private void FindSubExpressions()
                {
                    var indexes = new List<Tuple<int, int, SubExpressionDelimiter>>();
                    foreach (var delimiter in this.members.SubExpressionDelimiters)
                    {
                        for (int i = 0; i < 2; i++)
                        {
                            int previousIndex = 0;
                            for (; ; )
                            {
                                int index = this.text.IndexOf(delimiter[i], previousIndex);
                                if (index == -1) break;
                                indexes.Add(new Tuple<int, int, SubExpressionDelimiter>(index, i, delimiter));
                                previousIndex = index + delimiter[i].Length;
                            }
                        }
                    }

                    indexes.Sort((t1, t2) => t1.Item1 > t2.Item1 ? 1 : t1.Item1 < t2.Item1 ? -1 : 0);
                    int depth = 0;
                    var list = new List<List<SubExpressionIndex>>();

                    foreach (var index in indexes)
                    {
                        if (index.Item2 == 0)
                        {
                            if (list.Count <= depth)
                            {
                                list.Add(new List<SubExpressionIndex>());
                            }
                            list[depth].Add(new SubExpressionIndex(index.Item3, index.Item1, -1));
                            depth++;
                        }
                        else
                        {
                            depth--;
                            var currentList = list[depth];
                            var lastIndex = currentList[currentList.Count - 1];
                            lastIndex.SetFoundRightEnd(index.Item1);
                        }
                    }

                    foreach (var depthList in list)
                    {
                        foreach (var index in depthList)
                        {
                            if (index.Y == -1)
                            {
                                index.SetUnfoundRightEnd(this.text.Length);
                            }
                        }
                    }

                    this.subExpressions = list;
                }

                private void UpdateSubExpressions()
                {
                    var listo = this.data.SubExpressions.SelectMany(list => from index in list
                                                                            select index);

                    foreach (var depthList in this.data.SubExpressions)
                    {
                        foreach (var index in depthList)
                        {

                        }
                    }
                }

                private void ProcessReplacements()
                {
                    var replacements = this.data.Replacements;
                    if (replacements.Count == 0)
                    {
                        return;
                    }

                    //replacements.TempEvaluate(this.text);
                    replacements.Sort((r1, r2) => r1.Index > r2.Index ? 1 : r1.Index < r2.Index ? -1 : 0);

                    int previousIndex = 0;
                    List<int> indexes = new List<int>(replacements.Count);
                    foreach (var replacement in replacements)
                    {
                        int index = this.text.IndexOf(ExpressionEvaluator.FunctionDelimiter, previousIndex);
                        indexes.Add(index);
                        previousIndex = index + 1;
                    }

                    for (int i = indexes.Count - 1; i > -1; i--)
                    {
                        var replacement = replacements[i];
                        if (replacement.Target is Function)
                        {
                            this.RegisterFunction((Function)replacement.Target, indexes[i]);
                        }
                    }
                }

                private string GetNextReplacementName()
                {
                    this.currentExpression++;
                    return this.CurrentExpression;
                }

                private void RegisterFunction(Function function, int startIndex)
                {
                    string name = GetNextReplacementName();
                    this.text = this.text.InsertReplace(startIndex, 1, name);
                    this.usedFunctions.Add(name, function);
                }

                public string RegisterExpression(Expression exp)
                {
                    string name = GetNextReplacementName();
                    this.evaluatedExpressions.Add(name, exp);
                    return name;
                }

                private string GetConstantText(Constant constant)
                {
                    if (this.constantExpressions.ContainsKey(constant))
                    {
                        return this.constantExpressions[constant];
                    }
                    else
                    {
                        return ((ITextContainer)constant).Text;
                    }
                }

                private void FixConstants()
                {
                    foreach (Constant c in this.equation.Constants)
                    {
                        int previousIndex = 0;
                        var indexes = new List<int>();
                        for (; ; )
                        {
                            int index = this.text.IndexOf(c.Shorthand, previousIndex);
                            if (index == -1) break;
                            indexes.Add(index);
                            previousIndex = index + c.Shorthand.Length;
                        }

                        // only evaluate the constant's value if that constant is actually contained in the equation
                        if (previousIndex > 0)
                        {
                            indexes.Sort((x, y) => x < y ? 1 : x > y ? -1 : 0);

                            string text = this.GetConstantText(c);
                            int length = c.Shorthand.Length;
                            foreach (var index in indexes)
                            {
                                this.text = this.text.InsertReplace(index, length, text.ToString());
                                //int newIndex = index;
                                //StringBuilder replacementString = new StringBuilder();
                                //if (index > 0)
                                //{
                                //    char charLeft = this.text[index - 1];
                                //    if (char.IsLetterOrDigit(charLeft) || charLeft == ExpressionEvaluator.FunctionDelimiter)
                                //    {
                                //        newIndex -= Operator.Multiplication.Shorthand.Length;
                                //        replacementString.Append(Operator.Multiplication.Shorthand);
                                //    }
                                //}
                                //replacementString.Append(text);
                                //int rightIndex = index + length;
                                //if (rightIndex < this.text.Length)
                                //{
                                //    char charRight = this.text[rightIndex];
                                //    if (char.IsLetterOrDigit(charRight) || charRight == ExpressionEvaluator.FunctionDelimiter || charRight == '.' || IsValueADelimiter(this.text, rightIndex, true))
                                //    {
                                //        replacementString.Append(Operator.Multiplication.Shorthand);
                                //    }
                                //}

                                //this.text = this.text.InsertReplace(index, length, replacementString.ToString());
                            }
                        }
                    }
                }

                private bool IsValueADelimiter(string text, int index, bool checkLeftDelimiter = true)
                {
                    string subText = (index == 0) ? text : text.Substring(index);
                    foreach (var delimiter in data.Members.SubExpressionDelimiters)
                    {
                        if (subText.StartsWith(delimiter[checkLeftDelimiter])) return true;
                    }
                    return false;
                }

                private void EvaluateSubExpression(SubExpressionIndex subExpression, int depth)
                {
                    int leftIndex = subExpression.X;
                    int rightIndex = subExpression.Y;
                    int leftLength = subExpression.Left.Length;
                    int rightLength = subExpression.Right.Length;
                    int difference = subExpression.Y - subExpression.X - leftLength;
                    string lolzsf = subExpression.GetSubExpressionText(this.text);
                    string equationFragment = this.text.Substring(subExpression.X + leftLength,
                                                                    difference);
                    var function = this.GetAttachedFunction(subExpression.X);
                    Expression expr = null;

                    if (function != null)
                    {
                        string[] argumentFragments = equationFragment.Split(new char[] { ',' });
                        if (argumentFragments.Count() != function.ArgumentCount) throw new Exception(""); ////

                        Expression[] functionArguments = new Expression[argumentFragments.Count()];
                        for (int argIndex = 0; argIndex < argumentFragments.Count(); argIndex++)
                            functionArguments[argIndex] = this.ParseExpression(argumentFragments[argIndex]);
                        expr = Expression.Call(function.Method.Method, functionArguments);

                        leftIndex -= FunctionLength;
                        difference += FunctionLength;
                    }
                    else
                    {
                        expr = this.ParseExpression(equationFragment);
                    }

                    string name = this.RegisterExpression(expr);
                    difference += leftLength + (subExpression.IsRightEndPresent ? rightLength : 0);
                    this.text = this.text.InsertReplace(leftIndex, difference, name);
                    UpdateList(leftIndex, difference - name.Length, depth);
                }

                private Function GetAttachedFunction(int index)
                {
                    if (index >= FunctionLength)
                    {
                        int leftIndex = index - FunctionLength;
                        string value = this.text.Substring(leftIndex, FunctionLength);
                        if (value[0] == FunctionDelimiter && value[FunctionLength - 1] == FunctionDelimiter)
                        {
                            return this.usedFunctions[value];
                        }
                    }
                    return null;
                }

                private void UpdateList(int leftIndex, int length, int depth)
                {
                    for (int i = depth - 1; i > -1; i--)
                    {
                        var lowerList = this.subExpressions[i];
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

                private string FixString(string equationFragment)
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

                private Expression ParseExpression(string equationFragment)
                {
                    equationFragment = FixString(equationFragment);

                    List<Expression> expressions = new List<Expression>();
                    string pattern = @"\d+(?:\.{1}\d+)?"; // +data.Equation.Variable.Shorthand + "?";
                    string variablePattern = "(" + this.equation.Variables.GetRegexPattern() + "){1}";
                    string negPattern = @"-1\*";

                    var matches = Regex.Matches(equationFragment, pattern);
                    var matches2 = Regex.Matches(equationFragment, variablePattern, RegexOptions.IgnoreCase);
                    var matches3 = Regex.Matches(equationFragment, negPattern);
                    var matchInfoList = new List<MatchInfo>(matches.Count + matches2.Count);

                    foreach (Match m in matches)
                    {
                        if (m.Index > 0 && equationFragment[m.Index - 1] == ExpressionEvaluator.FunctionDelimiter)
                            matchInfoList.Add(new MatchInfo(m.Index - 1, m.Length + 2, ExpressionEvaluator.FunctionDelimiter + m.Value + ExpressionEvaluator.FunctionDelimiter));
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
                        return this.GetExpressionFromMatch(matchInfoList[0]);

                    matchInfoList.Sort((m1, m2) => m1.Index > m2.Index ? 1 : m1.Index < m2.Index ? -1 : 0);

                    foreach (var opList in this.operators)
                    {
                        var indexes = this.GetIndexesOfOperators(equationFragment, opList);
                        for (int i = 0; i < indexes.Count; i++)
                        {
                            var opIndex = indexes[i];
                            Operator op = opIndex.Operator;
                            int index = opIndex.Index;
                            int length = op.Shorthand.Length;
                            var surroundingMatches = this.GetSurroundingMatches(matchInfoList, index);
                            if (surroundingMatches == null) throw new Exception(""); /////

                            var arguments = new Expression[2];
                            arguments[0] = this.GetExpressionFromMatch(surroundingMatches.Item1);
                            arguments[1] = this.GetExpressionFromMatch(surroundingMatches.Item2);
                            Expression expr = op.GetExpression(arguments[0], arguments[1]);

                            string val = this.RegisterExpression(expr);
                            int leftIndex = surroundingMatches.Item1.Index;
                            int rightIndex = surroundingMatches.Item2.Index + surroundingMatches.Item2.Length;
                            int difference = rightIndex - leftIndex;
                            equationFragment = equationFragment.Remove(leftIndex, difference);
                            equationFragment = equationFragment.Insert(leftIndex, val);

                            this.UpdateMatches(matchInfoList, surroundingMatches, leftIndex, val, difference - val.Length);
                            for (int i2 = i + 1; i2 < indexes.Count; i2++)
                            {
                                indexes[i2].Index -= difference - val.Length;
                            }

                        }
                    }
                    if (equationFragment != this.CurrentExpression)
                    {
                        throw new Exception(""); ////
                    }

                    Expression resultantExpression = this.evaluatedExpressions[equationFragment];
                    //Delegate del = Expression.Lambda(resultantExpression, data.Variables).Compile();
                    //object result = del.DynamicInvoke(new object[] { 5.5D });
                    return resultantExpression;
                }

                private List<OperatorIndexListing> GetIndexesOfOperators(string text, IEnumerable<Operator> operators)
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
                    return indexes;
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

                private Expression GetExpressionFromMatch(MatchInfo match)
                {
                    double d;
                    if (Double.TryParse(match.Value, out d))
                    {
                        return Expression.Constant(d);
                    }

                    if (match.Value[0] == FunctionDelimiter)
                    {
                        return this.evaluatedExpressions[match.Value];
                    }

                    string matchText = match.Value.ToUpper();
                    foreach (var v in this.variables)
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

            }

            public override Delegate Parse(Equation equation, EquationMemberGroup members = null)
            {
                if (members == null)
                {
                    members = CS.DefaultMemberGroup;
                }

                var validationResults = ValidateEquation(equation, members);

                CS.Log.TraceInformation("Parsing equation " + equation.Text.ToString());
                //_InternalData data = new _InternalData(equation, members);

                int constantExpressionCount = validationResults.Item2.Count;
                var parameters = new ParameterExpression[constantExpressionCount];
                var expressions = new Expression[constantExpressionCount + 1];
                var evaluator = new ExpressionEvaluator(equation, members);

                for (int i = 0; i < constantExpressionCount; i++)
                {
                    var constantData = validationResults.Item2[i];
                    Constant constant = (Constant)constantData.Target;
                    var expression = evaluator.EvaluateTextExpression(constantData);
                    var variableExpression = Expression.Variable(
                        typeof(double),
                        constant.Shorthand);
                    var assignmentExpression = Expression.Assign(variableExpression, expression);
                    parameters[i] = variableExpression;
                    expressions[i] = assignmentExpression;
                }

                var equationExpression = evaluator.EvaluateTextExpression(validationResults.Item1);

                LambdaExpression lambdaExpression = null;
                if (constantExpressionCount > 0)
                {
                    expressions[constantExpressionCount] = equationExpression;
                    BlockExpression be = BlockExpression.Block(parameters, expressions);
                    lambdaExpression = Expression.Lambda(be, evaluator.Variables);
                }
                else
                {
                    lambdaExpression = Expression.Lambda(equationExpression, evaluator.Variables);
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

            private Tuple<ValidationData, List<ValidationData>> ValidateEquation(Equation equation, EquationMemberGroup members)
            {
                CS.Log.TraceInformation("Validating equation " + equation.Text.ToString());

                List<ValidationData> constantValidationData = new List<ValidationData>();
                var validator = new EquationTextValidator(equation, members);

                if (equation.Constants.Count() > 0)
                {
                    foreach (var constant in equation.Constants)
                    {
                        if (!constant.IsNumber)
                        {
                            constantValidationData.Add(validator.Validate(constant));
                        }
                    }
                }

                var equationValidationData = validator.Validate(equation);

                return new Tuple<ValidationData, List<ValidationData>>(equationValidationData, constantValidationData);
            }

        }
    }

}
