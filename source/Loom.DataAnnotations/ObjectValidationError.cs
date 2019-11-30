namespace Loom.DataAnnotations
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;

    public sealed class ObjectValidationError
    {
        internal ObjectValidationError(
            string objectPath,
            ValidationAttribute validationAttribute,
            ValidationResult validationResult,
            object value)
        {
            ObjectPath = objectPath;
            ValidationAttribute = validationAttribute;
            ValidationResult = validationResult;
            Value = value;
        }

        public string ObjectPath { get; }

        public ValidationAttribute ValidationAttribute { get; }

        public ValidationResult ValidationResult { get; }

        public object Value { get; }

        public IEnumerable<string> MemberPaths
            => from memberName in ValidationResult.MemberNames
               select PathComposer.Compose(ObjectPath, memberName);
    }
}
