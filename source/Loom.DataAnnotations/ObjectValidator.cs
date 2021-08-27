namespace Loom.DataAnnotations
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
    using System.Reflection;

    /// <summary>
    /// Provides a helper class that can be used to validate objects. Unlike <see cref="Validator"/>, <see cref="ObjectValidator"/> traverses an object graph and provides full paths of properties as member names for validation failures.
    /// </summary>
    public static class ObjectValidator
    {
        private interface IVisitorStrategy
        {
            bool ShouldVisitProperty(object instance, PropertyInfo property);
        }

        /// <summary>
        /// Determines whether the specified object is valid. If not valid a <see cref="ValidationException"/> is thrown.
        /// </summary>
        /// <param name="instance">The object to validate.</param>
        /// <exception cref="ValidationException">
        /// The object is not valid.
        /// </exception>
        [Obsolete("Use TryValidate(object, out IEnumerable<ObjectValidationError>) method instead.")]
        public static void Validate(object instance)
        {
            Visitor.Visit(
                instance,
                breakOnFirstError: true,
                onError: error => throw new ValidationError(error).ToException());
        }

        /// <summary>
        /// Determine whether the specified object is valid. If the object is valid <paramref name="validationResult"/> is set to <see cref="ValidationResult.Success"/>; otherwise, a <see cref="ValidationResult"/> that contains error data.
        /// </summary>
        /// <param name="instance">The object to validate.</param>
        /// <param name="validationResult">A <see cref="ValidationResult"/> object.</param>
        /// <returns><c>true</c> if the object is valid; otherwise, <c>false</c>.</returns>
        [Obsolete("Use TryValidate(object, out IEnumerable<ObjectValidationError>) method instead.")]
        public static bool TryValidate(
            object instance,
            out ValidationResult? validationResult)
        {
            ValidationResult? captured = ValidationResult.Success;

            Visitor.Visit(
                instance,
                breakOnFirstError: true,
                onError: error => captured = new ValidationError(error).ValidationResult);

            validationResult = captured;
            return validationResult == ValidationResult.Success;
        }

        /// <summary>
        /// Determine whether the specified object is valid using a validation results collector.
        /// </summary>
        /// <param name="instance">The object to validate.</param>
        /// <param name="validationResultCollector">A callback function called for each <see cref="ValidationResult"/> object indicating validation error.</param>
        /// <returns><c>true</c> if the object is valid; otherwise, <c>false</c>.</returns>
        [Obsolete("Use TryValidate(object, out IEnumerable<ObjectValidationError>) method instead.")]
        public static bool TryValidate(
            object instance,
            Action<ValidationResult> validationResultCollector)
        {
            if (validationResultCollector == null)
            {
                throw new ArgumentNullException(nameof(validationResultCollector));
            }

            bool hasError = false;

            Visitor.Visit(instance, breakOnFirstError: false, onError: error =>
            {
                hasError = true;
                validationResultCollector.Invoke(new ValidationError(error).ValidationResult);
            });

            return hasError == false;
        }

        /// <summary>
        /// Determine whether the specified object is valid.
        /// </summary>
        /// <param name="instance">The object to validate.</param>
        /// <param name="errors">An empty collection if <paramref name="instance"/> is valid <paramref name="errors"/>; otherwise, a list of <see cref="ObjectValidationError"/> that contains validation error data.</param>
        /// <returns><c>true</c> if <paramref name="instance"/> is valid; otherwise, <c>false</c>.</returns>
        public static bool TryValidate(
            object? instance,
            out IEnumerable<ObjectValidationError> errors)
        {
            bool hasError = false;
            List<ObjectValidationError>? validationErrors = default;

            Visitor.Visit(instance, breakOnFirstError: false, onError: error =>
            {
                hasError = true;
                validationErrors ??= new List<ObjectValidationError>();
                validationErrors.Add(error);
            });

            errors = validationErrors?.AsReadOnly() ?? Enumerable.Empty<ObjectValidationError>();
            return hasError == false;
        }

        [Obsolete]
        private struct ValidationError
        {
            public ValidationError(ObjectValidationError source)
            {
                ValidationAttribute = source.ValidationAttribute;
                ValidationResult = new ValidationResult(
                    source.ValidationResult.ErrorMessage,
                    memberNames: source.MemberPaths.ToList().AsReadOnly());
                Value = source.Value;
            }

            public ValidationAttribute ValidationAttribute { get; }

            public ValidationResult ValidationResult { get; }

            public object? Value { get; }

            public ValidationException ToException()
                => new ValidationException(ValidationResult, ValidationAttribute, Value);
        }

        private class CompositeVisitorStrategy : IVisitorStrategy
        {
            private readonly IVisitorStrategy[] _strategies;

            public CompositeVisitorStrategy(params IVisitorStrategy[] strategies)
            {
                _strategies = strategies;
            }

            public bool ShouldVisitProperty(object instance, PropertyInfo property)
            {
                foreach (IVisitorStrategy strategy in _strategies)
                {
                    if (strategy.ShouldVisitProperty(instance, property) == false)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        private class IndexerStrategy : IVisitorStrategy
        {
            public bool ShouldVisitProperty(object instance, PropertyInfo property)
                => IsIndexer(property) == false;

            private static bool IsIndexer(PropertyInfo property)
                => property.GetIndexParameters().Any();
        }

        private class GetterStratrgy : IVisitorStrategy
        {
            public bool ShouldVisitProperty(object instance, PropertyInfo property)
                => property.GetMethod?.IsStatic == false
                && property.GetMethod?.IsPublic == true;
        }

        private class PrimitiveTypeInstanceStrategy : IVisitorStrategy
        {
            public bool ShouldVisitProperty(object instance, PropertyInfo property)
                => instance.GetType().GetTypeInfo().IsPrimitive == false;
        }

        private class StringInstanceStratrgy : IVisitorStrategy
        {
            public bool ShouldVisitProperty(object instance, PropertyInfo property)
                => (instance is string) == false;
        }

        private class DateTimeInstanceStrategy : IVisitorStrategy
        {
            public bool ShouldVisitProperty(object instance, PropertyInfo property)
                => (instance is DateTime) == false;
        }

        private class DateTimeOffsetInstanceStrategy : IVisitorStrategy
        {
            public bool ShouldVisitProperty(object instance, PropertyInfo property)
                => (instance is DateTimeOffset) == false;
        }

        private class Visitor
        {
            private readonly IVisitorStrategy _strategy =
                new CompositeVisitorStrategy(
                    new IndexerStrategy(),
                    new GetterStratrgy(),
                    new PrimitiveTypeInstanceStrategy(),
                    new StringInstanceStratrgy(),
                    new DateTimeInstanceStrategy(),
                    new DateTimeOffsetInstanceStrategy());

            private readonly Stack<object> _history = new Stack<object>();

            private readonly bool _breakOnFirstError;
            private readonly Action<ObjectValidationError> _onError;
            private bool _hasError;

            private Visitor(bool breakOnFirstError, Action<ObjectValidationError> onError)
            {
                _breakOnFirstError = breakOnFirstError;
                _onError = onError;
                _hasError = false;
            }

            private bool ShouldBreak => _hasError && _breakOnFirstError;

            public static void Visit(
                object? instance,
                bool breakOnFirstError,
                Action<ObjectValidationError> onError)
            {
                new Visitor(breakOnFirstError, onError).Visit(instance, objectPath: string.Empty);
            }

            private void Visit(object? instance, string objectPath)
            {
                if (instance == null)
                {
                    return;
                }

                if (Visited(instance) == false)
                {
                    LeaveVisitRecord(instance);
                    VisitProperties(instance, objectPath);
                    if (HasElements(instance, out IEnumerable? enumerable))
                    {
                        VisitElements(enumerable!, objectPath);
                    }
                }
            }

            private bool Visited(object instance)
                => _history.Any(x => ReferenceEquals(x, instance));

            private void LeaveVisitRecord(object instance)
                => _history.Push(instance);

            private void VisitProperties(object instance, string objectPath)
            {
                foreach (PropertyInfo property in GetVisiteeProperties(instance))
                {
                    VisitProperty(objectPath, instance, property, property.Name);
                }
            }

            private IEnumerable<PropertyInfo> GetVisiteeProperties(object instance)
            {
                return from type in AscendTypeHierarchy(instance)
                       from property in GetDeclaredProperties(type)
                       where ShouldVisit(instance, property)
                       select property;
            }

            private IEnumerable<Type> AscendTypeHierarchy(object instance)
            {
                Type? type = instance.GetType();
                while (type != null)
                {
                    yield return type;
                    type = type.GetTypeInfo().BaseType;
                }
            }

            private IEnumerable<PropertyInfo> GetDeclaredProperties(Type type)
                => type.GetTypeInfo().DeclaredProperties;

            private bool ShouldVisit(object instance, PropertyInfo property)
                => _strategy.ShouldVisitProperty(instance, property);

            private void VisitProperty(
                string objectPath, object instance, PropertyInfo property, string memberName)
            {
                object? value = property.GetValue(instance);
                foreach (ValidationAttribute validator in GetValidators(property))
                {
                    if (ShouldBreak)
                    {
                        return;
                    }

                    ValidateProperty(objectPath, instance, value, memberName, validator);
                }

                Visit(value, objectPath: PathComposer.Compose(objectPath, memberName));
            }

            private static IEnumerable<ValidationAttribute> GetValidators(PropertyInfo property)
                => property.GetCustomAttributes<ValidationAttribute>();

            private void ValidateProperty(
                string objectPath,
                object instance,
                object? value,
                string memberName,
                ValidationAttribute validator)
            {
                var validationContext = new ValidationContext(instance) { MemberName = memberName };
                ValidationResult? result = validator.GetValidationResult(value, validationContext);
                if (result != null)
                {
                    _hasError = true;
                    _onError.Invoke(new ObjectValidationError(objectPath, validator, result, value));
                }
            }

            private static bool HasElements(object instance, out IEnumerable? enumerable)
            {
                enumerable = instance as IEnumerable;
                return enumerable != null && (enumerable is string) == false;
            }

            private void VisitElements(IEnumerable enumerable, string objectPath)
            {
                int index = 0;
                foreach (object element in enumerable)
                {
                    string elementPrefix = $"{objectPath}[{index}]";
                    Visit(element, elementPrefix);
                    index++;
                }
            }
        }
    }
}
