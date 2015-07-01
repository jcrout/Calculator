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

    }
}
