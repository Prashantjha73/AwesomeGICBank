namespace AwesomeGICBank.ConsoleApp.Data.Interfaces
{
    using AwesomeGICBank.ConsoleApp.Models;
    public interface IInterestRuleRepository
    {
        void AddOrUpdate(InterestRule rule);
        IEnumerable<InterestRule> GetAllRules();
        InterestRule GetEffectiveRule(DateTime date);
    }
}