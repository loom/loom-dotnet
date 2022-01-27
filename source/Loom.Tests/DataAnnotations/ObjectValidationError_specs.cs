using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Loom.DataAnnotations
{
    [TestClass]
    public class ObjectValidationError_specs
    {
        public class Component
        {
            [Range(1, 10)]
            public int Value { get; set; } = 1;

            public Component? Child { get; set; }
        }

        [TestMethod]
        public void MemberPaths_composes_path_correctly()
        {
            var instance = new Component
            {
                Child = new Component
                {
                    Child = new Component
                    {
                        Value = -1,
                    },
                },
            };

            ObjectValidator.TryValidate(
                instance,
                out IEnumerable<ObjectValidationError> errors);

            ObjectValidationError sut = errors.Single();
            sut.MemberPaths.Should().ContainSingle();
            sut.MemberPaths.Single().Should().Be("Child.Child.Value");
        }
    }
}
