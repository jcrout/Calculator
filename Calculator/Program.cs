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

        private static EquationMemberGroup defaultMemberGroup;
        internal static EquationMemberGroup DefaultMemberGroup
        {
            get
            {
                return defaultMemberGroup;
            }
        }

        static CalculatorSettings()
        {
            Trace.AutoFlush = true;
            traceListener = new TextWriterTraceListener("Trace.txt", "listener");
            Log.Listeners.Clear();
            Log.Listeners.Add(traceListener);

            var operators = new List<Operator>(Operator.DefaultList);
            var functions = new List<Function>(Function.DefaultList);
            var subExpressionDelimiters = new List<SubExpressionDelimiter>(SubExpressionDelimiter.DefaultList);

            var combinedList = ((IEnumerable<EquationMember>)operators).Concat(
                               ((IEnumerable<EquationMember>)functions)).Concat(
                               ((IEnumerable<EquationMember>)subExpressionDelimiters));

            defaultMemberGroup = new EquationMemberGroup("Default", combinedList);            
        }

        public static void AddMember(EquationMember member)
        {
            var memberType = member.GetHighestDerivedType();
            var duplicate = defaultMemberGroup[memberType].FirstOrDefault(em => string.Equals(em.Shorthand, member.Shorthand, StringComparison.OrdinalIgnoreCase));
            if (duplicate == null)
            {
                defaultMemberGroup.Add(member);
            }
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
                    AddMember((Operator)instance);
                }
                else if (instanceType == typeof(Function))
                {
                    AddMember((Function)instance);
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
