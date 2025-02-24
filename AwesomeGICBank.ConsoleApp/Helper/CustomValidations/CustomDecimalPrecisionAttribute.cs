using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace AwesomeGICBank.ConsoleApp.Helper.CustomValidations
{
    public class CustomDecimalPrecisionAttribute : ValidationAttribute
    {
        public int Precision { get; }

        public CustomDecimalPrecisionAttribute(int precision)
        {
            Precision = precision;
        }

        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value is decimal dec)
            {
                if (decimal.Round(dec, Precision) != dec)
                {
                    return new ValidationResult(ErrorMessage ?? $"The value cannot have more than {Precision} decimal places.");
                }
                return ValidationResult.Success;
            }
            return new ValidationResult("Invalid decimal value.");
        }
    }
}