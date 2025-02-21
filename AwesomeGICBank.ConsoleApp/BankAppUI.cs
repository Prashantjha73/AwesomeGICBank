
namespace AwesomeGICBank.ConsoleApp
{
    using AwesomeGICBank.ConsoleApp.Service.Interfaces;
    public class BankAppUI
    {
        private readonly IBankService bankService;

        public BankAppUI(IBankService bankService)
        {
            this.bankService = bankService;
        }

        public void Run()
        {
            bool running = true;
            while (running)
            {
                Console.WriteLine("Welcome to AwesomeGIC Bank! What would you like to do?");
                Console.WriteLine("[T] Input transactions");
                Console.WriteLine("[I] Define interest rules");
                Console.WriteLine("[P] Print statement");
                Console.WriteLine("[Q] Quit");
                Console.Write(">");
                string? choice = Console.ReadLine()?.Trim().ToUpper();

                switch (choice)
                {
                    case "T":
                        // To Do: Add Transactions
                        break;
                    case "I":
                        // To Do: Define Intrest rules Transactions
                        break;
                    case "P":
                        // To Do: Print statements
                        break;
                    case "Q":
                        running = false;
                        Console.WriteLine("Thank you for banking with AwesomeGIC Bank.\nHave a nice day!");
                        break;
                    default:
                        Console.WriteLine("Invalid option. Please choose again.");
                        break;
                }
            }
        }
    }
}