namespace Loom.DataAnnotations
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
    using FluentAssertions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Moq;

    [TestClass]
    public class ObjectValidator_specs
    {
        public class RootObject
        {
            [Range(1, 10)]
            public int Int32Property { get; set; } = 1;

            public StemObject StemObjectProperty { get; set; } = new StemObject();

            public IEnumerable CollectionProperty { get; set; } = Array.Empty<object>();
        }

        public class BaseObject
        {
            [Range(1, 10)]
            public int BaseInt32Property { get; set; } = 1;
        }

        public class StemObject : BaseObject
        {
            [StringLength(10)]
            public string StringProperty { get; set; } = "foo";
        }

        public class ElementObject
        {
            [Required]
            public object ObjectProperty { get; set; } = new object();
        }

        public class WriteOnlyObject
        {
            public int WriteOnlyInt32Property
            {
                set
                {
                }
            }
        }

        public interface ICollector
        {
            void Collect(ValidationResult validationResult);
        }

        [TestMethod]
        public void collecting_TryValidate_has_guard_clause_against_null_validationResultCollector()
        {
            object instance = "foo";
            Action action = () => ObjectValidator.TryValidate(instance, validationResultCollector: null);
            action.Should().Throw<ArgumentNullException>().Which.ParamName.Should().Be("validationResultCollector");
        }

        [TestMethod]
        public void given_null_argument_then_Validate_succeeds()
        {
            Action action = () => ObjectValidator.Validate(instance: null);
            action.Should().NotThrow();
        }

        [TestMethod]
        public void given_null_argument_then_TryValidate_succeeds()
        {
            object instance = null;

            bool successful = ObjectValidator.TryValidate(instance, out ValidationResult validationResult);

            successful.Should().BeTrue();
            validationResult.Should().Be(ValidationResult.Success);
        }

        [TestMethod]
        public void given_null_argument_then_collecting_TryValidate_succeeds()
        {
            object instance = null;
            ICollector collector = Mock.Of<ICollector>();

            bool successful = ObjectValidator.TryValidate(instance, collector.Collect);

            successful.Should().BeTrue();
            Mock.Get(collector)
                .Verify(x => x.Collect(It.IsAny<ValidationResult>()), Times.Never());
        }

        [TestMethod]
        public void given_valid_object_then_Validate_succeeds()
        {
            var instance = new RootObject();
            Action action = () => ObjectValidator.Validate(instance);
            action.Should().NotThrow();
        }

        [TestMethod]
        public void given_valid_object_then_TryValidate_succeeds()
        {
            var instance = new RootObject();

            bool successful = ObjectValidator.TryValidate(instance, out ValidationResult validationResult);

            successful.Should().BeTrue();
            validationResult.Should().Be(ValidationResult.Success);
        }

        [TestMethod]
        public void given_valid_object_then_collecting_TryValidate_succeeds()
        {
            var instance = new RootObject();
            ICollector collector = Mock.Of<ICollector>();

            bool successful = ObjectValidator.TryValidate(instance, collector.Collect);

            successful.Should().BeTrue();
            Mock.Get(collector)
                .Verify(x => x.Collect(It.IsAny<ValidationResult>()), Times.Never());
        }

        [TestMethod]
        public void given_root_has_invalid_property_then_Validate_throws_ValidationException()
        {
            var instance = new RootObject
            {
                Int32Property = -1,
            };

            Action action = () => ObjectValidator.Validate(instance);

            action.Should().Throw<ValidationException>()
                .Where(x => x.ValidationAttribute is RangeAttribute)
                .Which.ValidationResult.MemberNames.Should()
                .BeEquivalentTo("Int32Property");
        }

        [TestMethod]
        public void given_root_has_invalid_property_then_TryValidate_fails()
        {
            var instance = new RootObject
            {
                Int32Property = -1,
            };

            bool successful = ObjectValidator.TryValidate(instance, out ValidationResult validationResult);

            successful.Should().BeFalse();
            validationResult.Should().NotBe(ValidationResult.Success);
            validationResult.MemberNames.Should().BeEquivalentTo("Int32Property");
        }

        [TestMethod]
        public void given_root_has_invalid_property_then_collecting_TryValidate_fails()
        {
            var instance = new RootObject
            {
                Int32Property = -1,
            };
            var validationResults = new List<ValidationResult>();

            bool successful = ObjectValidator.TryValidate(instance, validationResults.Add);

            successful.Should().BeFalse();
            validationResults.Should().ContainSingle().Which.MemberNames.Should().BeEquivalentTo("Int32Property");
        }

        [TestMethod]
        public void given_stem_has_invalid_property_then_Validate_throws_ValidationException()
        {
            var instance = new RootObject
            {
                StemObjectProperty =
                {
                    StringProperty = "f to the o to the o",
                },
            };

            Action action = () => ObjectValidator.Validate(instance);

            action.Should().Throw<ValidationException>()
                .Where(x => x.ValidationAttribute is StringLengthAttribute)
                .Which.ValidationResult.MemberNames.Should()
                .BeEquivalentTo("StemObjectProperty.StringProperty");
        }

        [TestMethod]
        public void given_null_stem_then_Validate_succeeds()
        {
            var instance = new RootObject { StemObjectProperty = null };
            Action action = () => ObjectValidator.Validate(instance);
            action.Should().NotThrow();
        }

        [TestMethod]
        public void given_invalid_collection_element_then_Validate_throws_ValidationException()
        {
            var instance = new RootObject
            {
                CollectionProperty = new[]
                {
                    new ElementObject(),
                    new ElementObject { ObjectProperty = default },
                },
            };

            Action action = () => ObjectValidator.Validate(instance);

            action.Should().Throw<ValidationException>()
                .Where(x => x.ValidationAttribute is RequiredAttribute)
                .Which.ValidationResult.MemberNames.Should()
                .BeEquivalentTo("CollectionProperty[1].ObjectProperty");
        }

        [TestMethod]
        public void given_null_element_then_Validate_succeeds()
        {
            var instance = new RootObject
            {
                CollectionProperty = new[]
                {
                    new ElementObject(),
                    null,
                },
            };

            Action action = () => ObjectValidator.Validate(instance);

            action.Should().NotThrow();
        }

        [TestMethod]
        [Timeout(100)]
        public void given_circular_reference_then_Validate_succeeds()
        {
            var instance = new RootObject();
            instance.CollectionProperty = new[] { instance };

            Action action = () => ObjectValidator.Validate(instance);

            action.Should().NotThrow();
        }

        [TestMethod]
        public void given_stem_has_invalid_inherited_property_then_Validate_throws_ValidationException()
        {
            var instance = new RootObject
            {
                StemObjectProperty =
                {
                    BaseInt32Property = -1,
                },
            };

            Action action = () => ObjectValidator.Validate(instance);

            action.Should().Throw<ValidationException>()
                .Where(x => x.ValidationAttribute is RangeAttribute)
                .Which.ValidationResult.MemberNames.Should()
                .BeEquivalentTo("StemObjectProperty.BaseInt32Property");
        }

        public static IEnumerable<object[]> FIXTURE_given_multiple_errors_then_TryValidate_returns_the_first_error
        {
            get
            {
                yield return new object[]
                {
                    new RootObject
                    {
                        Int32Property = -1,
                        StemObjectProperty = { BaseInt32Property = -1 },
                    },
                    "Int32Property",
                };

                yield return new object[]
                {
                    new RootObject
                    {
                        Int32Property = 1,
                        CollectionProperty = new[]
                        {
                            new ElementObject { ObjectProperty = null },
                            new ElementObject { ObjectProperty = null },
                        },
                    },
                    "CollectionProperty[0].ObjectProperty",
                };
            }
        }

        [TestMethod]
        [DynamicData(nameof(FIXTURE_given_multiple_errors_then_TryValidate_returns_the_first_error))]
        public void given_multiple_errors_then_TryValidate_returns_the_first_error(
            object instance, string expectedMemberName)
        {
            // Act
            bool successful = ObjectValidator.TryValidate(instance, out ValidationResult validationResult);

            // Assert
            successful.Should().BeFalse();
            validationResult.Should().NotBe(ValidationResult.Success);
            validationResult.MemberNames.Should().BeEquivalentTo(expectedMemberName);
        }

        public static IEnumerable<object[]> FIXTURE_given_multiple_errors_then_collecting_TryValidate_invokes_collector_function_for_all_errors
        {
            get
            {
                yield return new object[]
                {
                    new RootObject
                    {
                        Int32Property = -1,
                        StemObjectProperty = { BaseInt32Property = -1 },
                    },
                    new[]
                    {
                        "Int32Property",
                        "StemObjectProperty.BaseInt32Property",
                    },
                };

                yield return new object[]
                {
                    new RootObject
                    {
                        Int32Property = 1,
                        CollectionProperty = new[]
                        {
                            new ElementObject { ObjectProperty = null },
                            new ElementObject { ObjectProperty = null },
                        },
                    },
                    new[]
                    {
                        "CollectionProperty[0].ObjectProperty",
                        "CollectionProperty[1].ObjectProperty",
                    },
                };
            }
        }

        [TestMethod]
        [DynamicData(nameof(FIXTURE_given_multiple_errors_then_collecting_TryValidate_invokes_collector_function_for_all_errors))]
        public void given_multiple_errors_then_collecting_TryValidate_invokes_collector_function_for_all_errors(
            object instance, string[] expectedMemberNames)
        {
            var validationResults = new List<ValidationResult>();

            bool successful = ObjectValidator.TryValidate(instance, validationResults.Add);

            successful.Should().BeFalse();
            validationResults.Should().HaveSameCount(expectedMemberNames);
            validationResults.SelectMany(r => r.MemberNames).Should().BeEquivalentTo(expectedMemberNames);
        }

        [TestMethod]
        public void given_write_only_property_then_Validate_succeeds()
        {
            var instance = new RootObject
            {
                CollectionProperty = new[]
                {
                    new WriteOnlyObject(),
                },
            };

            Action action = () => ObjectValidator.Validate(instance);

            action.Should().NotThrow();
        }

        [TestMethod]
        [Timeout(10)]
        public void given_DateTime_element_then_Validate_does_not_enter_infinite_recursion()
        {
            var instance = new RootObject
            {
                CollectionProperty = new[]
                {
                    DateTime.Now,
                },
            };

            Action action = () => ObjectValidator.Validate(instance);

            action.Should().NotThrow();
        }

        [TestMethod]
        [Timeout(10)]
        public void given_DateTimeOffset_element_then_Validate_does_not_enter_infinite_recursion()
        {
            var instance = new RootObject
            {
                CollectionProperty = new[]
                {
                    DateTimeOffset.Now,
                },
            };

            Action action = () => ObjectValidator.Validate(instance);

            action.Should().NotThrow();
        }

        public interface IHasInt32Value
        {
            int Int32Value { get; }
        }

        public class HasNotSupportedExplicitProperty : IHasInt32Value
        {
            int IHasInt32Value.Int32Value => throw new NotSupportedException();
        }

        [TestMethod]
        public void Validate_ignores_explicit_properties()
        {
            var instance = new RootObject
            {
                CollectionProperty = new[]
                {
                    new HasNotSupportedExplicitProperty(),
                },
            };

            Action action = () => ObjectValidator.Validate(instance);

            action.Should().NotThrow();
        }
    }
}
