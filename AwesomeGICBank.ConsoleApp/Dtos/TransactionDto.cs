using System.ComponentModel.DataAnnotations;
using AwesomeGICBank.ConsoleApp.Helper.CustomValidations;

namespace AwesomeGICBank.ConsoleApp.Dtos
{
    public class TransactionDto
    {
        [Required(ErrorMessage = "Date is required.")]
        [CustomYearRange(1900, ErrorMessage = "Invalid Year. Year must be greater than 1900.")]
        public DateTime Date { get; set; }

        [Required(ErrorMessage = "AccountId cannot be empty.")]
        public string AccountId { get; set; }

        [Required(ErrorMessage = "Transaction type is required.")]
        [RegularExpression("^(D|W)$", ErrorMessage = "Transaction type must be D (deposit) or W (withdrawal).")]
        public string Type { get; set; }

        [Required(ErrorMessage = "Amount is required.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than zero.")]
        [CustomDecimalPrecision(2, ErrorMessage = "Amount cannot have more than two decimal places.")]
        public decimal Amount { get; set; }
    }
}