namespace AwesomeGICBank.ConsoleApp.Service.Service
{
    using AwesomeGICBank.ConsoleApp.Data.Interfaces;
    using AwesomeGICBank.ConsoleApp.Dtos;
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

        public bool AddTransaction(TransactionDto transactionDto, out string message)
        {
            message = string.Empty;
            if (!transactionDto.Validate(out message))
            {
                return false;
            }

            var transaction = new Transaction
            {
                Date = transactionDto.Date,
                AccountId = transactionDto.AccountId,
                Type = transactionDto.Type.ToUpper() == "D" ? TransactionType.Deposit : TransactionType.Withdrawal,
                Amount = transactionDto.Amount
            };

            transaction.TransactionId = GenerateTransactionId(transaction.AccountId, transaction.Date);

            if (!ValidateBusinessRules(transaction, out message))
            {
                return false;
            }

            txnRepo.Add(transaction);
            message = $"Transaction added. {transaction.AccountId} statement updated.";
            return true;
        }

        public bool AddInterestRule(InterestRuleDto interestRuleDto, out string message)
        {
            message = string.Empty;
            if (!interestRuleDto.Validate(out message))
            {
                return false;
            }

            var rule = new InterestRule
            {
                Date = interestRuleDto.Date,
                RuleId = interestRuleDto.RuleId,
                RatePercent = interestRuleDto.RatePercent
            };

            ruleRepo.AddOrUpdate(rule);
            message = "Interest rule added/updated successfully.";
            return true;
        }

        public List<Transaction> GetStatement(string accountInput)
        {
            var statement = new List<Transaction>();

            if (string.IsNullOrWhiteSpace(accountInput))
                return statement;

            var parts = accountInput.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2)
                return statement;

            string accountId = parts[0];
            string yearMonthStr = parts[1];
            if (yearMonthStr.Length != 6 ||
                !int.TryParse(yearMonthStr.Substring(0, 4), out int year) ||
                !int.TryParse(yearMonthStr.Substring(4, 2), out int month))
            {
                return statement;
            }

            var allTxns = txnRepo.GetTransactions(accountId).ToList();
            if (!allTxns.Any())
                return statement;

            DateTime periodStart = new DateTime(year, month, 1);
            DateTime periodEnd = periodStart.AddMonths(1).AddDays(-1);

            decimal startingBalance = allTxns
                .Where(t => t.Date < periodStart)
                .Aggregate(0m, (sum, t) => sum + (t.Type == TransactionType.Deposit ? t.Amount : -t.Amount));

            var monthlyTxns = allTxns
                .Where(t => t.Date >= periodStart && t.Date <= periodEnd)
                .OrderBy(t => t.Date)
                .ThenBy(t => t.TransactionId)
                .ToList();

            // Calculate statement details and compute running balance.
            decimal runningBalance = startingBalance;
            decimal totalInterest = 0m;
            var txnsByDate = monthlyTxns.GroupBy(t => t.Date.Date)
                                        .ToDictionary(g => g.Key, g => g.ToList());
            DateTime current = periodStart;
            while (current <= periodEnd)
            {
                if (txnsByDate.TryGetValue(current.Date, out List<Transaction> dayTxns))
                {
                    foreach (var txn in dayTxns)
                    {
                        if (txn.Type == TransactionType.Deposit)
                            runningBalance += txn.Amount;
                        else if (txn.Type == TransactionType.Withdrawal)
                            runningBalance -= txn.Amount;

                        // Create a copy with computed balance.
                        var stmtTxn = new Transaction
                        {
                            Date = txn.Date,
                            TransactionId = txn.TransactionId,
                            AccountId = txn.AccountId,
                            Type = txn.Type,
                            Amount = txn.Amount,
                            Balance = runningBalance
                        };
                        statement.Add(stmtTxn);
                    }
                }

                // Compute interest for the day.
                var rule = ruleRepo.GetEffectiveRule(current);
                if (rule != null)
                {
                    totalInterest += (runningBalance * (rule.RatePercent / 100m));
                }

                current = current.AddDays(1);
            }

            decimal interest = Math.Round(totalInterest / 365m, 2);
            if (interest > 0)
            {
                runningBalance += interest;
                var interestTxn = new Transaction
                {
                    Date = periodEnd,
                    TransactionId = string.Empty,
                    AccountId = accountId,
                    Type = TransactionType.Interest,
                    Amount = interest,
                    Balance = runningBalance
                };
                statement.Add(interestTxn);
            }

            return statement;
        }

        public List<InterestRule> GetInterestRules()
        {
            return ruleRepo.GetAllRules().ToList();
        }

        private string GenerateTransactionId(string accountId, DateTime txnDate)
        {
            int countForDay = txnRepo.GetTransactionsByDate(accountId, txnDate).Count();
            return $"{txnDate:yyyyMMdd}-{(countForDay + 1):D2}";
        }

        private bool ValidateBusinessRules(Transaction newTxn, out string message)
        {
            message = string.Empty;
            var existingTxns = txnRepo.GetTransactions(newTxn.AccountId).ToList();
            var tempTxns = new List<Transaction>(existingTxns) { newTxn }
                           .OrderBy(t => t.Date)
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
                runningBalance += txn.Type == TransactionType.Deposit ? txn.Amount : -txn.Amount;
                if (runningBalance < 0)
                {
                    message = "Transaction would cause account balance to go negative.";
                    return false;
                }
            }

            return true;
        }
    }
}