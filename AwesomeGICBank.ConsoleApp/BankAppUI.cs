
namespace AwesomeGICBank.ConsoleApp
{
    using AwesomeGICBank.ConsoleApp.Data.Interfaces;
    using AwesomeGICBank.ConsoleApp.Service.Interfaces;
    using AwesomeGICBank.ConsoleApp.Service.Service;

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
                        InputTransactions();
                        break;
                    case "I":
                        DefineInterestRules();
                        break;
                    case "P":
                        PrintStatement();
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

        private void InputTransactions()
        {
            while (true)
            {
                Console.WriteLine("Please enter transaction details in <Date(YYYYMMdd)> <Account> <Type> <Amount> format (or enter blank to go back to main menu):");
                Console.Write(">");
                string? input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input))
                    break;

                if (this.bankService.AddTransaction(input, out string message))
                {
                    Console.WriteLine(message);
                    string[] parts = input.Trim().Split(' ');
                    string accountId = parts[1];
                    string yearMonth = parts[0].Substring(0, 6);
                    Console.WriteLine(this.bankService.GetStatement($"{accountId} {yearMonth}"));
                }
                else
                {
                    Console.WriteLine(message);
                }
            }
        }

        private void DefineInterestRules()
        {
            while (true)
            {
                Console.WriteLine("Please enter interest rules details in <Date(YYYYMMdd)> <RuleId> <Rate in %> format (or enter blank to go back to main menu):");
                Console.Write(">");
                string? input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input))
                    break;

                if (this.bankService.AddInterestRule(input, out string message))
                    Console.WriteLine(message);
                else
                    Console.WriteLine(message);

                // Display all interest rules ordered by date.
                var allRules = (this.bankService as BankService)
                    ?.GetType().GetField("_ruleRepo", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.GetValue(this.bankService) as IInterestRuleRepository;
                if (allRules != null)
                {
                    Console.WriteLine("Interest rules:");
                    Console.WriteLine("| Date     | RuleId | Rate (%) |");
                    foreach (var rule in allRules.GetAllRules())
                    {
                        Console.WriteLine($"| {rule.Date:yyyyMMdd} | {rule.RuleId,-6} | {rule.RatePercent,8:N2} |");
                    }
                }
            }
        }

        private void PrintStatement()
        {
            Console.WriteLine("Please enter account and month to generate the statement <Account> <Year><Month>(YYYYMM) (or enter blank to go back to main menu):");
            Console.Write(">");
            string? input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input))
                return;

            string output = this.bankService.GetStatement(input);
            Console.WriteLine(output);
        }
    }
}