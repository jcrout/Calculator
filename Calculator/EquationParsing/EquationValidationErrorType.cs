
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
    
    [Serializable]
    public enum EquationValidationErrorType : int
    {
        Unknown,
        InvalidSpacing,
        MultipleSequentialOperators,
        OperatorMissingValue,
        NumberTooLarge,
        TooManyDecimalPlaces,
        EmptySubExpression,
        MissingSubExpressionDelimiter,
        LeadingOperator,
        TrailingOperator,
        DecimalBetweenNonNumbers,
        TrailingDecimal,
        NonMatchingSubExpressionDelimiters,
        FunctionWithoutSubExpression,
        InvalidText
    }

}
