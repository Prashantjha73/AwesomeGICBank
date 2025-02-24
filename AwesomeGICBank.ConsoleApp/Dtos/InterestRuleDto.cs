using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AwesomeGICBank.ConsoleApp.Dtos
{
    public class InterestRuleDto
    {
        public DateTime Date { get; set; }
        public string? RuleId { get; set; }
        public decimal RatePercent { get; set; }

        public bool Validate(out string errorMessage)
        {
            errorMessage = string.Empty;

            // RuleId must not be empty.
            if (string.IsNullOrWhiteSpace(RuleId))
            {
                errorMessage = "RuleId cannot be empty.";
                return false;
            }

            // RatePercent must be greater than 0 and less than 100.
            if (RatePercent <= 0 || RatePercent >= 100)
            {
                errorMessage = "Interest rate must be greater than 0 and less than 100.";
                return false;
            }

            return true;
        }
    }
}