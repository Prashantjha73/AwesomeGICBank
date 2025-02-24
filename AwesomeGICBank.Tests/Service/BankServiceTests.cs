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

            // No existing transactions means this would be the first.
            mockTxnRepo.Setup(x => x.GetTransactions("A1")).Returns(new List<Transaction>());
            mockTxnRepo.Setup(x => x.GetTransactionsByDate("A1", dto.Date)).Returns(new List<Transaction>());

            bool result = bankService.AddTransaction(dto, out string message);
            Assert.False(result);
            Assert.Equal("First transaction cannot be a withdrawal.", message);
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
            // Existing deposit of 200 on 2024-11-01.
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
            Assert.Equal(2, result.Count);
            Assert.Contains(result, r => r.RuleId == "RULE01");
            Assert.Contains(result, r => r.RuleId == "RULE02");
        }
    }
}