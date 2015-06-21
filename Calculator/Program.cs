using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Calculator
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            //string eqText = @" x * 2 +5 * 3(55x - 2x *-A + (10x -2)-B)^2+sqrt(x)";

            //string eqText = @" -2x * 2 +5 * 3(55x - 2x *-A + (10x -2)-B)^2+sqrt(x- 1)";
            //string eqText2 = @"x*2+5*3(55x-2x*-A+(10x-2)-B)^2+sqrt(x)";
            //string eqText = "-X*max(A,B)";        
            string eqText = "55";
            Constant[] constants = new Constant[2];
            constants[0] = new Constant("A", "55.7");
            constants[1] = new Constant("B", "X - 1");

            Function[] functions = new Function[1];
            functions[0] = new Function("Square Root", "sqrt", new Func<double, double>(d => Math.Sqrt(d)));

            Equation equation = Equation.Create(eqText, constants, Variable.Default);

            Delegate del = EquationParser.ParseEquation(equation, EquationMemberGroup.Default);
            { }
            //Application.Run(new Form1());
        }
    }
}
