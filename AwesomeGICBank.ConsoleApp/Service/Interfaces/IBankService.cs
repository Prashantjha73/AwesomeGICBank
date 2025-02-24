using System.Collections.Generic;
using AwesomeGICBank.ConsoleApp.Dtos;
using AwesomeGICBank.ConsoleApp.Models;

namespace AwesomeGICBank.ConsoleApp.Service.Interfaces
{
    public interface IBankService
    {
        bool AddTransaction(TransactionDto transactionDto, out string message);
        bool AddInterestRule(InterestRuleDto interestRuleDto, out string message);
        List<Transaction>? GetStatement(StatementRequestDto request);
        List<InterestRule> GetInterestRules();
    }
}