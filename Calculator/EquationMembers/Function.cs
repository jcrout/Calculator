
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

}
