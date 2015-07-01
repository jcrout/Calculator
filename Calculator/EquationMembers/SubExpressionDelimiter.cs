
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

}
