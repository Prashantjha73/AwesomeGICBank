using Moq;
using AwesomeGICBank.ConsoleApp.Data.Interfaces;
using AwesomeGICBank.ConsoleApp.Dtos;
using AwesomeGICBank.ConsoleApp.Models;
using AwesomeGICBank.ConsoleApp.Models.Enums;
using AwesomeGICBank.ConsoleApp.Service.Interfaces;
using AwesomeGICBank.ConsoleApp.Service.Service;

namespace AwesomeGICBank.ConsoleApp.Tests
{
    public class BankServiceTests
    {
        private readonly Mock<ITransactionRepository> mockTxnRepo;
        private readonly Mock<IInterestRuleRepository> mockRuleRepo;
        private readonly IBankService bankService;

        public BankServiceTests()
        {
            mockTxnRepo = new Mock<ITransactionRepository>();
            mockRuleRepo = new Mock<IInterestRuleRepository>();
            bankService = new BankService(mockTxnRepo.Object, mockRuleRepo.Object);
        }

        [Fact]
        public void AddTransaction_InvalidDto_ReturnsFalse()
        {
            var dto = new TransactionDto
            {
                Date = default(DateTime),
                AccountId = "A1",
                Type = "Y",
                Amount = 100m
            };

            bool result = bankService.AddTransaction(dto, out string message);
            Assert.False(result);
            Assert.Equal("Transaction type must be D (deposit) or W (withdrawal).", message);
        }

        [Fact]
        public void AddTransaction_FirstTransactionWithdrawal_ReturnsFalse()
        {
            var dto = new TransactionDto
            {
                Date = new DateTime(2024, 11, 01),
                AccountId = "A1",
                Type = "W",
                Amount = 100m
            };

            mockTxnRepo.Setup(x => x.GetTransactions("A1")).Returns(new List<Transaction>());
            mockTxnRepo.Setup(x => x.GetTransactionsByDate("A1", dto.Date)).Returns(new List<Transaction>());

            bool result = bankService.AddTransaction(dto, out string message);
            Assert.False(result);
            Assert.Equal("First transaction cannot be a withdrawal.", message);
        }

        [Fact]
        public void AddTransaction_FirstTransactionWithdrawalType2_ReturnsFalse()
        {
            var dto = new TransactionDto
            {
                Date = new DateTime(2024, 11, 01),
                AccountId = "A1",
                Type = "D",
                Amount = 100m
            };

            var dto2 = new TransactionDto
            {
                Date = new DateTime(2024, 10, 30),
                AccountId = "A1",
                Type = "W",
                Amount = 100m
            };

            mockTxnRepo.Setup(x => x.GetTransactions("A1")).Returns(new List<Transaction>());
            mockTxnRepo.Setup(x => x.GetTransactionsByDate("A1", dto.Date)).Returns(new List<Transaction>());
            bankService.AddTransaction(dto, out string message);
            bool result = bankService.AddTransaction(dto2, out string message2);
            Assert.False(result);
            Assert.Equal("First transaction cannot be a withdrawal.", message2);
        }

        [Fact]
        public void AddTransaction_RunningBalanceNegative_ReturnsFalse()
        {
            var existingTxn = new Transaction
            {
                Date = new DateTime(2024, 11, 01),
                TransactionId = "20241101-01",
                AccountId = "A1",
                Type = TransactionType.Deposit,
                Amount = 50m
            };

            var dto = new TransactionDto
            {
                Date = new DateTime(2024, 11, 02),
                AccountId = "A1",
                Type = "W",
                Amount = 100m
            };

            mockTxnRepo.Setup(x => x.GetTransactions("A1")).Returns(new List<Transaction> { existingTxn });
            mockTxnRepo.Setup(x => x.GetTransactionsByDate("A1", dto.Date)).Returns(new List<Transaction>());

            bool result = bankService.AddTransaction(dto, out string message);
            Assert.False(result);
            Assert.Equal("Transaction would cause account balance to go negative.", message);
        }

        [Fact]
        public void AddTransaction_ValidTransaction_ReturnsTrue()
        {
            var existingTxn = new Transaction
            {
                Date = new DateTime(2024, 11, 01),
                TransactionId = "20241101-01",
                AccountId = "A1",
                Type = TransactionType.Deposit,
                Amount = 200m
            };

            var dto = new TransactionDto
            {
                Date = new DateTime(2024, 11, 02),
                AccountId = "A1",
                Type = "W",
                Amount = 100m
            };

            mockTxnRepo.Setup(x => x.GetTransactions("A1")).Returns(new List<Transaction> { existingTxn });
            mockTxnRepo.Setup(x => x.GetTransactionsByDate("A1", dto.Date)).Returns(new List<Transaction>());

            bool result = bankService.AddTransaction(dto, out string message);
            Assert.True(result);
            Assert.Contains("Transaction added", message);
            mockTxnRepo.Verify(x => x.Add(It.IsAny<Transaction>()), Times.Once);
        }


        [Fact]
        public void AddInterestRule_InvalidDto_ReturnsFalse()
        {
            var dto = new InterestRuleDto
            {
                Date = new DateTime(2024, 11, 01),
                RuleId = "RULE01",
                RatePercent = 0m
            };

            bool result = bankService.AddInterestRule(dto, out string message);
            Assert.False(result);
            Assert.Equal("Interest rate must be greater than 0 and less than 100.", message);
        }

        [Fact]
        public void AddInterestRule_Valid_ReturnsTrue()
        {
            var dto = new InterestRuleDto
            {
                Date = new DateTime(2024, 11, 01),
                RuleId = "RULE01",
                RatePercent = 1.5m
            };

            bool result = bankService.AddInterestRule(dto, out string message);
            Assert.True(result);
            Assert.Equal("Interest rule added/updated successfully.", message);
            mockRuleRepo.Verify(x => x.AddOrUpdate(It.IsAny<InterestRule>()), Times.Once);
        }

        [Fact]
        public void GetStatement_EmptyRequest_ReturnsNull()
        {
            var request = new StatementRequestDto { AccountId = "", Year = 2024, Month = 11 };
            var result = bankService.GetStatement(request);
            Assert.Null(result);
        }

        [Fact]
        public void GetStatement_NoTransactions_ReturnsEmptyList()
        {
            mockTxnRepo.Setup(x => x.GetTransactions("A1")).Returns(new List<Transaction>());
            var request = new StatementRequestDto { AccountId = "A1", Year = 2024, Month = 11 };
            var result = bankService.GetStatement(request);
            Assert.Empty(result);
        }

        [Fact]
        public void GetStatement_WithTransactions_ReturnsStatementAndInterest()
        {
            var txnBefore = new Transaction
            {
                Date = new DateTime(2024, 10, 31),
                TransactionId = "20241031-01",
                AccountId = "A1",
                Type = TransactionType.Deposit,
                Amount = 200m
            };

            var txnWithin = new Transaction
            {
                Date = new DateTime(2024, 11, 05),
                TransactionId = "20241105-01",
                AccountId = "A1",
                Type = TransactionType.Deposit,
                Amount = 100m
            };

            mockTxnRepo.Setup(x => x.GetTransactions("A1")).Returns(new List<Transaction> { txnBefore, txnWithin });
            mockRuleRepo.Setup(x => x.GetEffectiveRule(It.IsAny<DateTime>()))
                        .Returns((DateTime dt) => new InterestRule { Date = dt, RuleId = "RULE01", RatePercent = 1.0m });

            var request = new StatementRequestDto { AccountId = "A1", Year = 2024, Month = 11 };
            var result = bankService.GetStatement(request);
            Assert.NotEmpty(result);
            Assert.Equal(TransactionType.Interest, result.Last().Type);
        }

        [Fact]
        public void GetInterestRules_ReturnsAllRules()
        {
            var rules = new List<InterestRule>
            {
                new InterestRule { Date = new DateTime(2024, 11, 01), RuleId = "RULE01", RatePercent = 1.0m },
                new InterestRule { Date = new DateTime(2024, 11, 15), RuleId = "RULE02", RatePercent = 1.5m }
            };

            mockRuleRepo.Setup(x => x.GetAllRules()).Returns(rules);
            var result = bankService.GetInterestRules();
            Assert.Equal(2, result?.Count);
            Assert.Contains(result, r => r.RuleId == "RULE01");
            Assert.Contains(result, r => r.RuleId == "RULE02");
        }

        [Fact]
        public void AddTransaction_RepositoryThrowsException_ReturnsFalse()
        {
            var dto = new TransactionDto
            {
                Date = new DateTime(2024, 11, 02),
                AccountId = "A1",
                Type = "D",
                Amount = 100m
            };

            // Simulate exception when fetching transactions.
            mockTxnRepo.Setup(x => x.GetTransactions(It.IsAny<string>())).Throws(new Exception("Repository error"));

            bool result = bankService.AddTransaction(dto, out string message);
            Assert.False(result);
            Assert.Contains("Exception", message);
        }

        [Fact]
        public void AddInterestRule_RepositoryThrowsException_ReturnsFalse()
        {
            var dto = new InterestRuleDto
            {
                Date = new DateTime(2024, 11, 01),
                RuleId = "RULE01",
                RatePercent = 1.5m
            };

            mockRuleRepo.Setup(x => x.AddOrUpdate(It.IsAny<InterestRule>())).Throws(new Exception("Repository error"));

            bool result = bankService.AddInterestRule(dto, out string message);
            Assert.False(result);
            Assert.Contains("Exception", message);
        }

        [Fact]
        public void GetInterestRules_RepositoryThrowsException_ReturnsEmptyList()
        {
            mockRuleRepo.Setup(x => x.GetAllRules()).Throws(new Exception("Repository error"));
            var result = bankService.GetInterestRules();
            Assert.Null(result);
        }

        [Fact]
        public void GetStatement_ValidScenario_ReturnsCorrectInterestAndBalance()
        {
            var transactions = new List<Transaction>
            {
                new Transaction
                {
                    Date = new DateTime(2024, 11, 01),
                    TransactionId = "20241101-01",
                    AccountId = "A1",
                    Type = TransactionType.Deposit,
                    Amount = 250m
                },
                new Transaction
                {
                    Date = new DateTime(2024, 11, 02),
                    TransactionId = "20241102-01",
                    AccountId = "A1",
                    Type = TransactionType.Deposit,
                    Amount = 100m
                },
                new Transaction
                {
                    Date = new DateTime(2024, 11, 10),
                    TransactionId = "20241110-01",
                    AccountId = "A1",
                    Type = TransactionType.Withdrawal,
                    Amount = 10m
                },
                new Transaction
                {
                    Date = new DateTime(2024, 11, 15),
                    TransactionId = "20241115-01",
                    AccountId = "A1",
                    Type = TransactionType.Deposit,
                    Amount = 1000m
                },
                new Transaction
                {
                    Date = new DateTime(2024, 11, 27),
                    TransactionId = "20241127-01",
                    AccountId = "A1",
                    Type = TransactionType.Withdrawal,
                    Amount = 128m
                },
            };
            mockTxnRepo.Setup(x => x.GetTransactions("A1")).Returns(transactions);
            mockRuleRepo.Setup(x => x.GetEffectiveRule(It.IsAny<DateTime>()))
                        .Returns((DateTime dt) =>
                        {
                            if (dt < new DateTime(2024, 11, 11))
                                return new InterestRule { Date = new DateTime(2024, 1, 1), RuleId = "R1", RatePercent = 2.2m };
                            else if (dt < new DateTime(2024, 11, 26))
                                return new InterestRule { Date = new DateTime(2024, 11, 11), RuleId = "R2", RatePercent = 2.5m };
                            else
                                return new InterestRule { Date = new DateTime(2024, 11, 26), RuleId = "R3", RatePercent = 1.8m };
                        });

            var request = new StatementRequestDto { AccountId = "A1", Year = 2024, Month = 11 };
            var statement = bankService.GetStatement(request);
            var interestTxn = statement.Last();
            Assert.Equal(TransactionType.Interest, interestTxn.Type);
            Assert.Equal(1.61m, interestTxn.Amount);
            Assert.Equal(1213.61m, interestTxn.Balance);
        }
    }
}