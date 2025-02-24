
namespace AwesomeGICBank.ConsoleApp.Helper.CustomValidations
{
    using System.ComponentModel.DataAnnotations;
    public class CustomYearRangeAttribute : ValidationAttribute
    {
        public int Minimum { get; }
        public CustomYearRangeAttribute(int minimum)
        {
            Minimum = minimum;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is DateTime dt)
            {
                if (dt.Year < Minimum || dt.Year > DateTime.Now.Year)
                {
                    return new ValidationResult(ErrorMessage ?? $"Year must be between {Minimum} and {DateTime.Now.Year}.");
                }
                return ValidationResult.Success;
            }
            return new ValidationResult("Invalid date format.");
        }
    }
}