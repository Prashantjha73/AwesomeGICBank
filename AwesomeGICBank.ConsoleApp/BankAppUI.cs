using System;
using System.Globalization;
using AwesomeGICBank.ConsoleApp.Dtos;
using AwesomeGICBank.ConsoleApp.Service.Interfaces;
using AwesomeGICBank.ConsoleApp.Models;

namespace AwesomeGICBank.ConsoleApp
{
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

                if (!DateTime.TryParseExact(parts[0], "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date))
                {
                    Console.WriteLine("Invalid date format. Use YYYYMMdd.");
                    continue;
                }

                string accountId = parts[1];
                string type = parts[2];
                if (string.IsNullOrWhiteSpace(type) || (type.ToUpper() != "D" && type.ToUpper() != "W"))
                {
                    Console.WriteLine("Transaction type must be D (deposit) or W (withdrawal).");
                    continue;
                }

                if (!decimal.TryParse(parts[3], out decimal amount) || amount <= 0)
                {
                    Console.WriteLine("Amount must be a positive number.");
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

                if (!transactionDto.Validate(out string dtoError))
                {
                    Console.WriteLine(dtoError);
                    continue;
                }

                if (bankService.AddTransaction(transactionDto, out string message))
                {
                    Console.WriteLine(message);
                    string yearMonth = parts[0].Substring(0, 6);
                    var statement = bankService.GetStatement($"{accountId} {yearMonth}");
                    if (statement.Count == 0)
                        Console.WriteLine("No transactions found for this period.");
                    else
                    {
                        Console.WriteLine("Account Statement:");
                        Console.WriteLine("| Date     | Txn Id      | Type |  Amount |  Balance |");
                        foreach (var txn in statement)
                        {
                            // For interest transactions, TransactionId may be empty.
                            Console.WriteLine($"| {txn.Date:yyyyMMdd} | {txn.TransactionId,-12} | {txn.Type.ToString()[0]}    | {txn.Amount,7:N2} | {txn.Balance,8:N2} |");
                        }
                    }
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

                if (!DateTime.TryParseExact(parts[0], "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date))
                {
                    Console.WriteLine("Invalid date format. Use YYYYMMdd.");
                    continue;
                }

                string ruleId = parts[1];
                if (string.IsNullOrWhiteSpace(ruleId))
                {
                    Console.WriteLine("RuleId cannot be empty.");
                    continue;
                }

                if (!decimal.TryParse(parts[2], out decimal rate) || rate <= 0 || rate >= 100)
                {
                    Console.WriteLine("Interest rate must be greater than 0 and less than 100.");
                    continue;
                }

                InterestRuleDto interestRuleDto = new InterestRuleDto
                {
                    Date = date,
                    RuleId = ruleId,
                    RatePercent = rate
                };

                if (!interestRuleDto.Validate(out string dtoError))
                {
                    Console.WriteLine(dtoError);
                    continue;
                }

                if (bankService.AddInterestRule(interestRuleDto, out string message))
                {
                    Console.WriteLine(message);
                    ShowInterestRules();
                }
                else
                    Console.WriteLine(message);
            }
        }

        private void ShowInterestRules()
        {
            var rules = bankService.GetInterestRules();
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
            Console.WriteLine("Please enter account and month to generate the statement in the format <Account> <YYYYMM> (or enter blank to go back to main menu):");
            Console.Write(">");
            string? input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input))
                return;

            var statement = bankService.GetStatement(input);
            if (statement.Count == 0)
            {
                Console.WriteLine("Invalid input or no transactions found.");
                return;
            }
            Console.WriteLine("Account Statement:");
            Console.WriteLine("| Date     | Txn Id      | Type |  Amount |  Balance |");
            foreach (var txn in statement)
            {
                Console.WriteLine($"| {txn.Date:yyyyMMdd} | {txn.TransactionId,-12} | {txn.Type.ToString()[0]}    | {txn.Amount,7:N2} | {txn.Balance,8:N2} |");
            }
        }
    }
}