
namespace AwesomeGICBank.ConsoleApp.Models
{
    using AwesomeGICBank.ConsoleApp.Models.Enums;
    public class Transaction
    {
        public DateTime Date { get; set; }
        public string TransactionId { get; set; }
        public string AccountId { get; set; }
        public TransactionType Type { get; set; }
        public decimal Amount { get; set; }
    }
}