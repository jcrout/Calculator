
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
    
    [DebuggerDisplay("{Shorthand,nq}:  {Text,nq}")]
    public class Constant : Variable, ITextContainer
    {
        private static Constant emptyConstant = new Constant(string.Empty, string.Empty, string.Empty);
        private string text;

        public static Constant Empty
        {
            get
            {
                return emptyConstant;
            }
        }

        public static new Constant Create(string shorthand, string value)
        {
            return new Constant(
                shorthand ?? string.Empty, 
                value ?? string.Empty);
        }

        private Constant(string shorthand, string value, string name = "")
            : base(shorthand, name ?? "Constant")
        {
            this.text = value;
        }
        
        public bool IsNumber
        {
            get
            {
                double d;
                bool result = double.TryParse(this.text, out d);
                return result;
            }
        }

        public bool IsValid()
        {
            return (string.IsNullOrWhiteSpace(this.text) || string.IsNullOrWhiteSpace(this.Shorthand));
        }

        string ITextContainer.Text
        {
            get { return this.text; }
        }
    }
}
