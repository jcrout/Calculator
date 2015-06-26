namespace Calculator
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;
    using System.ComponentModel.Composition;
    using System.ComponentModel.Composition.Hosting;
    using JonUtility;

    public interface ICalculatorExtender
    {
        object[] LoadInstances();

        Type[] LoadTypes();
    }

    public static class CalculatorSettings
    {
        private static Dictionary<Type, List<EquationMember>> dict;
        internal static TraceListener traceListener;
        internal static TraceSource Log = new TraceSource("CalculatorSource", SourceLevels.Information);
        internal static List<Operator> Operators;
        internal static List<Function> Functions;
        internal static List<SubExpressionDelimiter> SubExpressionDelimiters;
        internal static EquationMemberGroup DefaultMemberGroup;

        static CalculatorSettings()
        {
            Trace.AutoFlush = true;
            traceListener = new TextWriterTraceListener("Trace.txt", "listener");
            Log.Listeners.Clear();
            Log.Listeners.Add(traceListener);

            Operators = new List<Operator>(Operator.DefaultList);
            Functions = new List<Function>(Function.DefaultList);
            SubExpressionDelimiters = new List<SubExpressionDelimiter>(SubExpressionDelimiter.DefaultList);

            var combinedList = ((IEnumerable<EquationMember>)Operators).Concat(
                               ((IEnumerable<EquationMember>)Functions)).Concat(
                               ((IEnumerable<EquationMember>)SubExpressionDelimiters));

            DefaultMemberGroup = new EquationMemberGroup("Default", combinedList);
        }

        public static void AddMember(EquationMember member)
        {

        }

        public static void AddOperator(Operator op)
        {
            Operator duplicate = Operators.Find(o => o.Shorthand.ToUpper() == op.Shorthand.ToUpper());
            if (duplicate != null)
            {
                throw new Exception("Plugin attempted to add a duplicate operator."); ////
            }

            Operators.Add(op);
        }

        public static void AddFunction(Function function)
        {
            Function duplicate = Functions.Find(f => f.Shorthand.ToUpper() == function.Shorthand.ToUpper());
            if (duplicate != null)
            {
                throw new Exception("Plugin attempted to add a duplicate function."); ////
            }

            Functions.Add(function);
        }

        public static void LoadExtension(string filePath)
        {
            Assembly extensionAssembly = null;
            Type[] extensionTypes = null;
            try
            {
                extensionAssembly = Assembly.LoadFile(filePath);
                extensionTypes = extensionAssembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                Log.TraceError(ex);
                throw;
            }


            Type extensionManagerType = extensionTypes.FirstOrDefault(type => type.IsClass && typeof(ICalculatorExtender).IsAssignableFrom(type));
            if (extensionManagerType == null)
            {
                return;
            }
            
            ICalculatorExtender extension = (ICalculatorExtender)Activator.CreateInstance(extensionManagerType);
            object[] instances = TryGetInstances(extension, filePath);
          
            foreach (object instance in instances)
            {
                Type instanceType = instance.GetType();
                if (instanceType.IsSubclassOf(typeof(Operator)))
                {
                    AddOperator((Operator)instance);
                }
                else if (instanceType == typeof(Function))
                {
                    AddFunction((Function)instance);
                }
            }
        }

        private static object[] TryGetInstances(ICalculatorExtender extension, string filePath)
        {
            try
            {
                return extension.LoadInstances();
            }                
            catch (Exception)
            {
                FileInfo fileInfo = new FileInfo(filePath);
                throw new Exception("Cannot load extension " + fileInfo.Name + ": contains one or more errors.");
            }
        }

        [Conditional("TRACE")]
        public static void SetListener(TraceListener listener)
        {
            traceListener = listener;
        }

    }
}
