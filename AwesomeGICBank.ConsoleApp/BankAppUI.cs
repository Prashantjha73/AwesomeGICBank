namespace AwesomeGICBank.ConsoleApp
{
    using System;
    using System.Globalization;
    using AwesomeGICBank.ConsoleApp.Dtos;
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

                string[] parts = input.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 4)
                {
                    Console.WriteLine("Invalid input format. Please enter: <Date(YYYYMMdd)> <Account> <Type> <Amount>");
                    continue;
                }

                if (!ValidateDate(parts[0], out DateTime date))
                    continue;

                string accountId = parts[1];
                string type = parts[2];

                if (!decimal.TryParse(parts[3], out decimal amount))
                {
                    Console.WriteLine("Invalid amount");
                    continue;
                }

                if (decimal.Round(amount, 2) != amount)
                {
                    Console.WriteLine("Amount cannot have more than two decimal places.");
                    continue;
                }

                TransactionDto transactionDto = new TransactionDto
                {
                    Date = date,
                    AccountId = accountId,
                    Type = type,
                    Amount = amount
                };

                if (bankService.AddTransaction(transactionDto, out string message))
                {
                    Console.WriteLine(message);
                    string yearMonth = parts[0].Substring(0, 6);
                    PrintStatementInFormat($"{accountId} {yearMonth}");
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
                Console.WriteLine("Please enter interest rule details in <Date(YYYYMMdd)> <RuleId> <Rate in %> format (or enter blank to go back to main menu):");
                Console.Write(">");
                string? input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input))
                    break;

                string[] parts = input.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 3)
                {
                    Console.WriteLine("Invalid input format. Please enter: <Date(YYYYMMdd)> <RuleId> <Rate in %>");
                    continue;
                }

                if (!ValidateDate(parts[0], out DateTime date))
                    continue;

                string ruleId = parts[1];
                if (string.IsNullOrWhiteSpace(ruleId))
                {
                    Console.WriteLine("RuleId cannot be empty.");
                    continue;
                }
                if (!decimal.TryParse(parts[2], out decimal rate))
                {
                    Console.WriteLine("Invalid rate.");
                    continue;
                }

                InterestRuleDto interestRuleDto = new InterestRuleDto
                {
                    Date = date,
                    RuleId = ruleId,
                    RatePercent = rate
                };

                if (bankService.AddInterestRule(interestRuleDto, out string message))
                {
                    Console.WriteLine(message);
                    ShowInterestRules();
                }
                else
                {
                    Console.WriteLine(message);
                }
            }
        }

        private void ShowInterestRules()
        {
            var rules = bankService.GetInterestRules(out string message);
            if (rules == null)
            {
                Console.WriteLine(message);
                return;
            }
            if (rules.Count == 0)
            {
                Console.WriteLine("No interest rules defined.");
                return;
            }
            Console.WriteLine("Interest Rules:");
            Console.WriteLine("| Date     | RuleId | Rate (%) |");
            foreach (var rule in rules)
            {
                Console.WriteLine($"| {rule.Date:yyyyMMdd} | {rule.RuleId,-6} | {rule.RatePercent,8:N2} |");
            }
        }

        private void PrintStatement()
        {
            while (true)
            {
                Console.WriteLine("Please enter account and month to generate the statement in the format <Account> <YYYYMM> (or enter blank to go back to main menu):");
                Console.Write(">");
                string? input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input))
                    break;

                PrintStatementInFormat(input);
            }
        }

        private StatementRequestDto? SanitiseAccountStatementInputRequest(string input)
        {
            var parts = input.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2)
            {
                Console.WriteLine("Invalid input format.");
                return null;
            }

            string accountId = parts[0];
            string yearMonthStr = parts[1];
            if (yearMonthStr.Length != 6 ||
                !int.TryParse(yearMonthStr.Substring(0, 4), out int year) ||
                !int.TryParse(yearMonthStr.Substring(4, 2), out int month))
            {
                Console.WriteLine("Invalid YearMonth format. Use YYYYMM.");
                return null;
            }

            return new StatementRequestDto
            {
                AccountId = accountId,
                Year = year,
                Month = month
            }; ;
        }

        private void PrintStatementInFormat(string input)
        {
            var request = SanitiseAccountStatementInputRequest(input);

            if (request == null)
            {
                return;
            }
            var statement = bankService.GetStatement(request, out string message);
            if (statement == null)
            {
                Console.WriteLine(message);
                return;
            }
            if (statement.Count == 0)
            {
                Console.WriteLine("No transactions found for this period.");
                return;
            }

            Console.WriteLine("Account Statement:");
            Console.WriteLine("| Date     | Txn Id      | Type |  Amount |  Balance |");
            foreach (var txn in statement)
            {
                Console.WriteLine($"| {txn.Date:yyyyMMdd} | {txn.TransactionId,-12} | {txn.Type.ToString()[0]}    | {txn.Amount,7:N2} | {txn.Balance,8:N2} |");
            }
        }

        private bool ValidateDate(string dateString, out DateTime date)
        {
            if (!DateTime.TryParseExact(dateString, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
            {
                Console.WriteLine("Invalid date format. Use YYYYMMdd.");
                return false;
            }
            return true;
        }
    }
}