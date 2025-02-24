using System.ComponentModel.DataAnnotations;

namespace AwesomeGICBank.ConsoleApp.Dtos
{
    public class StatementRequestDto
    {
        [Required(ErrorMessage = "AccountId cannot be empty.")]
        public string AccountId { get; set; }

        [Range(1900, 3000, ErrorMessage = "Invalid Year. Year must be greater than 1900.")]
        public int Year { get; set; }

        [Range(1, 12, ErrorMessage = "Invalid Month.")]
        public int Month { get; set; }
    }
}