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
    public sealed class InlineAutoDataAttribute : Attribute, ITestDataSource
    {
        private readonly object[] _arguments;

        public InlineAutoDataAttribute(params object[] arguments)
        {
            _arguments = arguments;
        }

        public IEnumerable<object[]> GetData(MethodInfo methodInfo)
        {
            object[] controlledValues = _arguments;
            ParameterInfo[] parameters = methodInfo.GetParameters();
            IEnumerable<object> generatedValues = Generate(parameters.Skip(controlledValues.Length));
            yield return controlledValues.Concat(generatedValues).ToArray();
        }

        private IEnumerable<object> Generate(IEnumerable<ParameterInfo> parameters)
        {
            IFixture generator = CreateGenerator();

            var arguments = new List<object>();
            foreach (ParameterInfo parameter in parameters)
            {
                CustomizeFixture(generator, parameter);
                arguments.Add(Resolve(generator, parameter));
            }

            return arguments;
        }

        private static IFixture CreateGenerator()
        {
            ICustomization customization = new CompositeCustomization(
                new AutoMoqCustomization(),
                new DateTimeCustomization());

            return new Fixture { Behaviors = { new OmitOnRecursionBehavior() } }.Customize(customization);
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
                from t in parameters.Zip(data, (parameter, argument) => (parameter, argument))
                select DumpArgument(t.parameter, t.argument);
            return string.Join(", ", values);
        }

        private static string DumpArgument(ParameterInfo parameter, object argument)
        {
            return $"{parameter.Name}: {argument?.ToString() ?? "null"}";
        }
    }
}
