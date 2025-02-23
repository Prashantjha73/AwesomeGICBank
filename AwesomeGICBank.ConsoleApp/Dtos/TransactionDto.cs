using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AwesomeGICBank.ConsoleApp.Models.Enums;

namespace AwesomeGICBank.ConsoleApp.Dtos
{
    public class TransactionDto
    {
        public required DateTime Date { get; set; }
        public required string AccountId { get; set; }
        public required string Type { get; set; }
        public required decimal Amount { get; set; }

        public bool Validate(out string errorMessage)
        {
            errorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(AccountId))
            {
                errorMessage = "AccountId cannot be empty.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(Type) || (Type.ToUpper() != "D" && Type.ToUpper() != "W"))
            {
                errorMessage = "Transaction type must be D (deposit) or W (withdrawal).";
                return false;
            }

            if (Amount <= 0)
            {
                errorMessage = "Amount must be greater than zero.";
                return false;
            }

            if (decimal.Round(Amount, 2) != Amount)
            {
                errorMessage = "Amount cannot have more than two decimal places.";
                return false;
            }

            return true;
        }
    }
}