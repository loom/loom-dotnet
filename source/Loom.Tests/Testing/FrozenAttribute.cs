using System;
using System.Reflection;
using AutoFixture;

namespace Loom.Testing
{
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = true, Inherited = false)]
    public sealed class FrozenAttribute : Attribute, IParameterCustomizationSource
    {
        public ICustomization GetCustomization(ParameterInfo parameter)
        {
            return new FreezingCustomization(parameter.ParameterType);
        }
    }
}
