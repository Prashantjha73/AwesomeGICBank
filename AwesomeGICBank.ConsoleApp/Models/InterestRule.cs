using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AwesomeGICBank.ConsoleApp.Models
{
    public class InterestRule
    {
        public DateTime Date { get; set; }
        public string RuleId { get; set; }
        public decimal RatePercent { get; set; }
    }
}