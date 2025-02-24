using System.ComponentModel.DataAnnotations;
using AwesomeGICBank.ConsoleApp.Helper.CustomValidations;

namespace AwesomeGICBank.ConsoleApp.Dtos
{
    public class InterestRuleDto
    {
        [Required(ErrorMessage = "Date is required.")]
        [CustomYearRange(1900, ErrorMessage = "Invalid Year. Year must be greater than 1900.")]
        public DateTime Date { get; set; }

        [Required(ErrorMessage = "RuleId cannot be empty.")]
        public string RuleId { get; set; }

        [Range(0.01, 99.99, ErrorMessage = "Interest rate must be greater than 0 and less than 100.")]
        public decimal RatePercent { get; set; }
    }
}