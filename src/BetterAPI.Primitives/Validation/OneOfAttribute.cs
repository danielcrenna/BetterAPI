using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;

namespace BetterAPI.Validation
{
    public sealed class OneOfAttribute : ValidationAttribute
    {
        private readonly object[] _oneOf;

        public string[] OneOfStrings { get; }

        public OneOfAttribute(params object[] oneOf)
        {
            _oneOf = oneOf.OrderBy(x => x).ToArray();
            OneOfStrings = oneOf.Select(x => x.ToString() ?? string.Empty).ToArray();
            ErrorMessage = "{0} is not one of: {1}";
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext) => _oneOf.Contains(value) ? ValidationResult.Success : new ValidationResult(FormatErrorMessage(value?.ToString()));

        public override string FormatErrorMessage(string? name) => string.Format(CultureInfo.CurrentCulture, ErrorMessageString, new object[] { name!, string.Join(", ", _oneOf) });
    }
}
