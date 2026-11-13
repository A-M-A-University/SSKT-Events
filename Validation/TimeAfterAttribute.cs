using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace SSKT_Events.Validation
{
    public class TimeAfterAttribute : ValidationAttribute
    {
        private readonly string _startTimeProperty;

        public TimeAfterAttribute(string startTimeProperty)
        {
            _startTimeProperty = startTimeProperty;
        }

        protected override ValidationResult? IsValid(
            object? value,
            ValidationContext validationContext)
        {
            if (value == null)
                return ValidationResult.Success;

            PropertyInfo? property = validationContext.ObjectType
                .GetProperty(_startTimeProperty);

            if (property == null)
                return new ValidationResult("Not found.");

            TimeSpan startTime = (TimeSpan)property.GetValue(validationContext.ObjectInstance)!;
            TimeSpan endTime = (TimeSpan)value;

            if (endTime > startTime)
                return ValidationResult.Success;

            return new ValidationResult(ErrorMessage);
        }
    }
}