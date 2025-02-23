
namespace AwesomeGICBank.ConsoleApp.Data.Repository
{
    using AwesomeGICBank.ConsoleApp.Data.Interfaces;
    using AwesomeGICBank.ConsoleApp.Models;

    public class InterestRuleRepository : IInterestRuleRepository
    {
        // Using a List to store interest rules in memory.
        private readonly List<InterestRule> rules = new();

        public void AddOrUpdate(InterestRule rule)
        {
            var existing = this.rules.FirstOrDefault(r => r.Date.Date == rule.Date.Date);
            if (existing != null)
            {
                existing.RuleId = rule.RuleId;
                existing.RatePercent = rule.RatePercent;
            }
            else
            {
                this.rules.Add(rule);
            }
        }

        public IEnumerable<InterestRule> GetAllRules()
        {
            return rules.OrderBy(r => r.Date);
        }

        public InterestRule? GetEffectiveRule(DateTime date)
        {
            return this.rules
                .Where(r => r.Date.Date <= date.Date)
                .OrderByDescending(r => r.Date)
                .FirstOrDefault();
        }
    }
}