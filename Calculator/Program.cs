using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Calculator
{
    static class Program
    {
        internal static TraceSource Log;
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Log = new TraceSource("CalculatorSource", SourceLevels.Information); 
            //Application.EnableVisualStyles();
            //Application.SetCompatibleTextRenderingDefault(false);
            //string eqText = @"log10(X) - -7max(3B(B),ABA) + l^2 - 3X(5(X))";
            //Constant[] constants = new Constant[3];
            //constants[0] = new Constant("A", "55.7");
            //constants[1] = new Constant("B", "X - 1");
            //constants[2] = new Constant("l", "33.21");

            //Function[] functions = new Function[1];
            //functions[0] = new Function("Square Root", "sqrt", new Func<double, double>(d => Math.Sqrt(d)));

            //equation = Equation.Create(eqText, constants, new Variable[] { Variable.XVariable }); //, Variable.YVariable, new Variable("TE") });
            //Stopwatch sw = Stopwatch.StartNew();
            //sw.Stop();
            //delegates = new Delegate[10000];
            //JonUtility.Diagnostics.BenchmarkMethod(methodo, 100, false, s => Console.WriteLine(s));
            //JonUtility.Diagnostics.BenchmarkMethod(methodo, 100, false, s => Console.WriteLine(s));
            //someFunc = (Func<double, double>)delegates[0];
            //JonUtility.Diagnostics.BenchmarkMethod(methodo2, 100, false, s => Console.WriteLine(s));
         
            Application.Run(new CalculatorForm());
        }

        private static Equation equation;
        private static Delegate[] delegates;
        private static Func<double, double> someFunc;
        private static int delCounter = 0;
        private static double someResultz = 0;
        private static void methodo()
        {
            Delegate del = EquationParser.ParseEquation(equation, EquationMemberGroup.Default);
            delegates[delCounter] = del;
            delCounter++;
            //object result = del.DynamicInvoke(new object[] { 5.7D });
            //Console.WriteLine(result.ToString());
        }

        private static void methodo2()
        {
            someResultz = someFunc(5.5D);
        }
    }
}
