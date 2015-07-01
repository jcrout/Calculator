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
}
