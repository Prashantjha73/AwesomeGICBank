namespace AwesomeGICBank.ConsoleApp.Service.Service
{
    using AwesomeGICBank.ConsoleApp.Data.Interfaces;
    using AwesomeGICBank.ConsoleApp.Dtos;
    using AwesomeGICBank.ConsoleApp.Helper;
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
            try
            {
                message = string.Empty;
                if (!DtoValidationHelper.Validate(transactionDto, out message))
                    return false;

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
            catch (Exception ex)
            {
                message = $"Failed to add transaction for AccountId: {transactionDto.AccountId}. Exception: {ex.Message}";
                return false;
            }
        }

        public bool AddInterestRule(InterestRuleDto interestRuleDto, out string message)
        {
            try
            {
                message = string.Empty;

                if (!DtoValidationHelper.Validate(interestRuleDto, out message))
                    return false;

                var rule = new InterestRule
                {
                    Date = interestRuleDto.Date,
                    RuleId = interestRuleDto.RuleId!,
                    RatePercent = interestRuleDto.RatePercent
                };

                ruleRepo.AddOrUpdate(rule);
                message = "Interest rule added/updated successfully.";
                return true;
            }
            catch (Exception ex)
            {
                message = $"Failed to add rule. Exception: {ex.Message}";
                return false;
            }
        }

        public List<Transaction>? GetStatement(StatementRequestDto request, out string message)
        {
            try
            {
                message = string.Empty;
                if (!DtoValidationHelper.Validate(request, out message))
                    return null;

                var accountId = request.AccountId!;
                var periodStart = new DateTime(request.Year, request.Month, 1);
                var periodEnd = periodStart.AddMonths(1).AddDays(-1);

                var allTxns = txnRepo.GetTransactions(accountId).ToList();
                if (!allTxns.Any())
                    return new List<Transaction>();

                decimal startingBalance = ComputeStartingBalance(allTxns, periodStart);
                var monthlyTxns = allTxns
                    .Where(t => t.Date >= periodStart && t.Date <= periodEnd)
                    .OrderBy(t => t.Date)
                    .ThenBy(t => t.TransactionId)
                    .ToList();

                return CalculateStatementDetails(periodStart, periodEnd, monthlyTxns, startingBalance);
            }
            catch (Exception ex)
            {
                message = $"Failed to retrieve statement. Exception: {ex.Message}";
                return null;
            }
        }

        public List<InterestRule>? GetInterestRules(out string message)
        {
            try
            {
                message = string.Empty;
                return ruleRepo.GetAllRules().ToList();
            }
            catch (Exception ex)
            {
                message = $"Failed to retrieve rules. Exception: {ex.Message}";
                return null;
            }
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

        private decimal ComputeStartingBalance(List<Transaction> allTxns, DateTime periodStart)
        {
            return allTxns
                .Where(t => t.Date < periodStart)
                .Aggregate(0m, (sum, t) => sum + (t.Type == TransactionType.Deposit ? t.Amount : -t.Amount));
        }

        private List<Transaction> CalculateStatementDetails(DateTime periodStart, DateTime periodEnd, List<Transaction> monthlyTxns, decimal startingBalance)
        {
            var statement = new List<Transaction>();
            decimal runningBalance = startingBalance;
            decimal totalInterest = 0m;
            var txnsByDate = monthlyTxns.GroupBy(t => t.Date.Date)
                                        .ToDictionary(g => g.Key, g => g.ToList());
            DateTime current = periodStart;

            while (current <= periodEnd)
            {
                if (txnsByDate.TryGetValue(current.Date, out List<Transaction>? dayTxns))
                {
                    foreach (var txn in dayTxns)
                    {
                        if (txn.Type == TransactionType.Deposit)
                            runningBalance += txn.Amount;
                        else if (txn.Type == TransactionType.Withdrawal)
                            runningBalance -= txn.Amount;

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

                // Compute daily interest.
                var rule = ruleRepo.GetEffectiveRule(current);
                if (rule != null)
                {
                    totalInterest += runningBalance * (rule.RatePercent / 100m);
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
                    AccountId = monthlyTxns.FirstOrDefault()?.AccountId ?? string.Empty,
                    Type = TransactionType.Interest,
                    Amount = interest,
                    Balance = runningBalance
                };
                statement.Add(interestTxn);
            }

            return statement;
        }
    }
}