
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

        public static Variable Create(string shorthand, string name = "Variable")
        {
            return new Variable(shorthand, name);
        }

        protected Variable(string shorthand, string name = "Variable")
        {
            this.shorthand = shorthand ?? string.Empty;
            this.name = name ?? string.Empty;
        }
    }

}
