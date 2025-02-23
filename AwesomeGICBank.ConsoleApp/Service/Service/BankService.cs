
namespace AwesomeGICBank.ConsoleApp.Service.Service
{
    using System.Globalization;
    using AwesomeGICBank.ConsoleApp.Data.Interfaces;
    using AwesomeGICBank.ConsoleApp.Models;
    using AwesomeGICBank.ConsoleApp.Models.Enums;
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
            message = string.Empty;
            if (string.IsNullOrWhiteSpace(input))
                return false;

            var parts = input.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 3)
            {
                message = "Invalid input format. Please enter: <Date> <RuleId> <Rate in %>";
                return false;
            }

            if (!DateTime.TryParseExact(parts[0], "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime ruleDate))
            {
                message = "Invalid date format. Use YYYYMMdd.";
                return false;
            }

            string ruleId = parts[1];

            if (!decimal.TryParse(parts[2], out decimal rate) || rate <= 0 || rate >= 100)
            {
                message = "Interest rate must be greater than 0 and less than 100.";
                return false;
            }

            var rule = new InterestRule
            {
                Date = ruleDate,
                RuleId = ruleId,
                RatePercent = rate
            };

            this.ruleRepo.AddOrUpdate(rule);
            message = "Interest rule added/updated successfully.";
            return true;
        }

        public bool AddTransaction(string input, out string message)
        {
            message = string.Empty;
            if (string.IsNullOrWhiteSpace(input))
                return false;

            var parts = input.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 4)
            {
                message = "Invalid input format. Please enter: <Date> <Account> <Type> <Amount>";
                return false;
            }

            if (!DateTime.TryParseExact(parts[0], "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime txnDate))
            {
                message = "Invalid date format. Use YYYYMMdd.";
                return false;
            }

            string accountId = parts[1];
            char typeChar = char.ToUpper(parts[2][0]);
            TransactionType txnType;
            if (typeChar == 'D')
                txnType = TransactionType.Deposit;
            else if (typeChar == 'W')
                txnType = TransactionType.Withdrawal;
            else
            {
                message = "Invalid transaction type. Use D for deposit or W for withdrawal.";
                return false;
            }

            if (!decimal.TryParse(parts[3], out decimal amount) || amount <= 0)
            {
                message = "Amount must be a positive number.";
                return false;
            }

            // Get existing transactions for the account.
            var existingTxns = this.txnRepo.GetTransactions(accountId).ToList();

            // Determine the transaction id for the new transaction.
            int countForDay = this.txnRepo.GetTransactionsByDate(accountId, txnDate).Count();
            string txnId = $"{txnDate:yyyyMMdd}-{(countForDay + 1):D2}";

            var newTxn = new Transaction
            {
                Date = txnDate,
                AccountId = accountId,
                TransactionId = txnId,
                Type = txnType,
                Amount = amount
            };

            var tempTxns = new List<Transaction>(existingTxns) { newTxn };
            tempTxns = tempTxns.OrderBy(t => t.Date)
                               .ThenBy(t => t.TransactionId)
                               .ToList();

            if (tempTxns.First().Type == TransactionType.Withdrawal)
            {
                message = "First transaction cannot be a withdrawal.";
                return false;
            }

            decimal runningBalance = 0m;
            foreach (var txn in tempTxns)
            {
                if (txn.Type == TransactionType.Deposit)
                    runningBalance += txn.Amount;
                else if (txn.Type == TransactionType.Withdrawal)
                    runningBalance -= txn.Amount;

                if (runningBalance < 0)
                {
                    message = "Transaction would cause account balance to go negative.";
                    return false;
                }
            }

            // If validations pass, add the new transaction.
            this.txnRepo.Add(newTxn);
            message = $"Transaction added. {accountId} statement updated.";
            return true;
        }

        public string GetStatement(string accountInput)
        {
            if (string.IsNullOrWhiteSpace(accountInput))
                return string.Empty;

            var parts = accountInput.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2)
                return "Invalid input. Please enter: <Account> <Year><Month>";

            string accountId = parts[0];
            string yearMonthStr = parts[1];
            if (yearMonthStr.Length != 6 ||
                !int.TryParse(yearMonthStr.Substring(0, 4), out int year) ||
                !int.TryParse(yearMonthStr.Substring(4, 2), out int month))
            {
                return "Invalid YearMonth. Format should be YYYYMM.";
            }

            // Get all transactions for the account
            var allTxns = this.txnRepo.GetTransactions(accountId).ToList();
            if (!allTxns.Any())
                return "Account not found.";

            // Compute starting balance: transactions before the given month
            DateTime periodStart = new DateTime(year, month, 1);
            decimal startingBalance = allTxns
                .Where(t => t.Date < periodStart)
                .Aggregate(0m, (sum, t) => sum + (t.Type == TransactionType.Deposit ? t.Amount : -t.Amount));

            DateTime periodEnd = periodStart.AddMonths(1).AddDays(-1);
            var monthlyTxns = allTxns.Where(t => t.Date >= periodStart && t.Date <= periodEnd).OrderBy(t => t.Date).ThenBy(t => t.TransactionId).ToList();

            var lines = new List<string>();
            decimal balance = startingBalance;

            decimal totalInterest = 0m;
            DateTime current = periodStart;
            var txnsByDate = monthlyTxns.GroupBy(t => t.Date.Date)
                                        .ToDictionary(g => g.Key, g => g.ToList());

            while (current <= periodEnd)
            {
                if (txnsByDate.TryGetValue(current.Date, out List<Transaction> dayTxns))
                {
                    foreach (var txn in dayTxns)
                    {
                        if (txn.Type == TransactionType.Deposit)
                        {
                            balance += txn.Amount;
                        }
                        else if (txn.Type == TransactionType.Withdrawal)
                        {
                            balance -= txn.Amount;
                        }

                        lines.Add($"{txn.Date:yyyyMMdd} | {txn.TransactionId,-12} | {txn.Type.ToString()[0]}    | {txn.Amount,7:N2} | {balance,8:N2}");
                    }
                }

                var rule = this.ruleRepo.GetEffectiveRule(current);
                if (rule != null)
                {
                    var cal = (balance * (rule.RatePercent / 100m));
                    totalInterest += (balance * (rule.RatePercent / 100m));
                    // Console.WriteLine($"Date: {current.Date} | Balance: {balance} | Percentage: {rule.RatePercent} | Cal: {cal} | Total: {totalInterest}");
                }

                current = current.AddDays(1);
            }

            totalInterest = Math.Round(totalInterest / 365m, 2);

            if (totalInterest > 0)
            {
                balance += totalInterest;
                lines.Add($"{periodEnd:yyyyMMdd} | {"",12} | I    | {totalInterest,7:N2} | {balance,8:N2}");
            }

            var output = $"Account: {accountId}\n";
            output += "| Date     | Txn Id      | Type |  Amount |  Balance |\n";
            output += string.Join("\n", lines);
            return output;
        }

        private decimal ComputeBalance(string accountId)
        {
            var txns = this.txnRepo.GetTransactions(accountId);
            decimal balance = 0m;
            foreach (var t in txns)
            {
                if (t.Type == TransactionType.Deposit)
                    balance += t.Amount;
                else if (t.Type == TransactionType.Withdrawal)
                    balance -= t.Amount;
            }
            return balance;
        }
    }
}