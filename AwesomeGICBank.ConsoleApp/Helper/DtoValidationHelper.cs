
namespace AwesomeGICBank.ConsoleApp.Helper
{
    using System.ComponentModel.DataAnnotations;
    public static class DtoValidationHelper
    {
        public static bool Validate<T>(T dto, out string errorMessage)
        {
            errorMessage = string.Empty;
            var context = new ValidationContext(dto, serviceProvider: null, items: null);
            var results = new List<ValidationResult>();

            bool isValid = Validator.TryValidateObject(dto, context, results, validateAllProperties: true);

            if (!isValid)
            {
                errorMessage = string.Join("\n", results.Select(x => x.ErrorMessage));
                return false;
            }

            return true;
        }
    }
}