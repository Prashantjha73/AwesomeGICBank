namespace AwesomeGICBank.ConsoleApp.Service.Interfaces
{
    public interface IBankService
    {
        bool AddTransaction(string input, out string message);
        bool AddInterestRule(string input, out string message);
        string GetStatement(string accountInput);
    }
}