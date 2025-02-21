
namespace AwesomeGICBank.ConsoleApp.Data.Repository
{
    using AwesomeGICBank.ConsoleApp.Data.Interfaces;
    using AwesomeGICBank.ConsoleApp.Models;
    public class TransactionRepository : ITransactionRepository
    {
        // Using a List to store transactions in memory.
        private readonly List<Transaction> transactions = new();

        public void Add(Transaction txn)
        {
            this.transactions.Add(txn);
        }

        public IEnumerable<Transaction> GetTransactions(string accountId)
        {
            return this.transactions
                .Where(t => t.AccountId.Equals(accountId, StringComparison.OrdinalIgnoreCase))
                .OrderBy(t => t.Date)
                .ThenBy(t => t.TransactionId);
        }

        public IEnumerable<Transaction> GetTransactionsByDate(string accountId, DateTime date)
        {
            return this.transactions.Where(t =>
                t.AccountId.Equals(accountId, StringComparison.OrdinalIgnoreCase) &&
                t.Date.Date == date.Date);
        }
    }
}