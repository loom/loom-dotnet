namespace Loom.Testing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using AutoFixture;
    using AutoFixture.AutoMoq;
    using AutoFixture.Kernel;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public sealed class AutoDataAttribute : Attribute, ITestDataSource
    {
        public IEnumerable<object[]> GetData(MethodInfo methodInfo)
        {
            IFixture generator = CreateGenerator();

            var arguments = new List<object>();
            foreach (ParameterInfo parameter in methodInfo.GetParameters())
            {
                CustomizeFixture(generator, parameter);
                arguments.Add(Resolve(generator, parameter));
            }

            yield return arguments.ToArray();
        }

        private static IFixture CreateGenerator()
        {
            return new Fixture { Behaviors = { new OmitOnRecursionBehavior() } }.Customize(new DomainCustomization());
        }

        private static void CustomizeFixture(IFixture generator, ParameterInfo parameter)
        {
            foreach (Attribute attribute in parameter.GetCustomAttributes())
            {
                switch (attribute)
                {
                    case IParameterCustomizationSource source:
                        generator.Customize(source.GetCustomization(parameter));
                        break;
                }
            }
        }

        private static object Resolve(IFixture generator, ParameterInfo parameter)
        {
            var context = new SpecimenContext(generator);
            return context.Resolve(parameter);
        }

        public string GetDisplayName(MethodInfo methodInfo, object[] data)
        {
            ParameterInfo[] parameters = methodInfo.GetParameters();
            IEnumerable<string> values =
                from t in parameters.Zip(data, (p, a) => (parameter: p, argument: a))
                select DumpArgument(t.parameter, t.argument);
            return string.Join(", ", values);
        }

        private static string DumpArgument(ParameterInfo parameter, object argument)
        {
            return $"{parameter.Name}: {argument?.ToString() ?? "null"}";
        }
    }
}
