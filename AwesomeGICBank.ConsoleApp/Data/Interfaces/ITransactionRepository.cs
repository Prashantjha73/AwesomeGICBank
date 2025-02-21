
namespace AwesomeGICBank.ConsoleApp.Data.Interfaces
{
    using AwesomeGICBank.ConsoleApp.Models;
    public interface ITransactionRepository
    {
        void Add(Transaction txn);
        IEnumerable<Transaction> GetTransactions(string accountId);
        IEnumerable<Transaction> GetTransactionsByDate(string accountId, DateTime date);
    }
}