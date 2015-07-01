using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Calculator;

namespace CalculatorProgram
{
    static class Program
    {
        internal const string ExtensionFolderName = "Extensions";
        static string extensionFolder;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            LoadExtensions();
            //TestMethod();

            Application.Run(new CalculatorForm());
        }

        private static DirectoryInfo GetExtensionsFolder()
        {
            try
            {
                var di = new DirectoryInfo(Application.StartupPath);
                while (true)
                {
                    var folder = di.GetDirectories().FirstOrDefault(d => d.Name == ExtensionFolderName);
                    if (folder != null)
                    {
                        extensionFolder = folder.FullName;
                        return folder;
                    }
                    else
                    {
                        // no plugin folder found
                        if (di.Parent == null)
                        {
                            return null;
                        }
                        else
                        {
                            di = di.Parent;
                        }
                    }
                }
            }
            catch (UnauthorizedAccessException ex)
            {
                return null;
            }
        }

        private static void LoadExtensions()
        {
            DirectoryInfo extensionFolder = GetExtensionsFolder();
            if (extensionFolder == null)
            {
                return;
            }

            var Files = extensionFolder.GetFiles(@"*.dll");
            if (Files.Length == 0)
            {
                return;
            }

            foreach (var file in Files)
            {
                Calculator.CalculatorSettings.LoadExtension(file.FullName);
            }
        }

        private static Equation[] equations;
        private static Delegate[] delegates;
        private static int delegateIndex = 0;
        private static void TestMethod()
        {
            Constant[] constants = new Constant[3];
            constants[0] = new Constant("A", "X + 1");
            constants[1] = new Constant("B", "A5.0"); //  x- 1(xmax(  5..  , Alog10xwhyX5[x-] *  XB [3max(5,6)) ) "); // 22.5 55+x   55x 33  322.3x x
            constants[2] = new Constant("C", "A/B");

            equations = new Equation[2];
            equations[0] = Equation.Create(@"X / A * B / (C / x", constants);
            equations[1] = Equation.Create(@"X / B * A / (C / x", constants);
            delegates = new Delegate[10000];
            JonUtility.Diagnostics.BenchmarkMethod(_TestMethod, 200, false, s => Console.WriteLine(s));
            JonUtility.Diagnostics.BenchmarkMethod(_TestMethod2, 200, false, s => Console.WriteLine(s));
        }

        private static void _TestMethod()
        {
            delegates[delegateIndex] = EquationParser.ParseEquation(equations[0]);
            delegateIndex++;
        }

        private static void _TestMethod2()
        {
            delegates[delegateIndex] = EquationParser.ParseEquation(equations[1]);
            delegateIndex++;
        }
    }
}
