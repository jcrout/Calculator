
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

}
