
namespace AwesomeGICBank.ConsoleApp.Service.Service
{
    using AwesomeGICBank.ConsoleApp.Data.Interfaces;
    using AwesomeGICBank.ConsoleApp.Service.Interfaces;

    public class BankService : IBankService
    {
        private readonly ITransactionRepository txnRepo;
        private readonly IInterestRuleRepository ruleRepo;

        public BankService(ITransactionRepository txnRepo, IInterestRuleRepository ruleRepo)
        {
            this.txnRepo = txnRepo;
            this.ruleRepo = ruleRepo;
        }
        public bool AddInterestRule(string input, out string message)
        {
            throw new NotImplementedException();
        }

        public bool AddTransaction(string input, out string message)
        {
            throw new NotImplementedException();
        }

        public string GetStatement(string accountInput)
        {
            throw new NotImplementedException();
        }
    }
}