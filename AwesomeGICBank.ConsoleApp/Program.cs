using AwesomeGICBank.ConsoleApp;
using AwesomeGICBank.ConsoleApp.Data.Interfaces;
using AwesomeGICBank.ConsoleApp.Data.Repository;
using AwesomeGICBank.ConsoleApp.Service.Interfaces;
using AwesomeGICBank.ConsoleApp.Service.Service;
using Microsoft.Extensions.DependencyInjection;

internal class Program
{
    private static void Main(string[] args)
    {
        try
        {
            var services = new ServiceCollection();
            services.AddSingleton<ITransactionRepository, TransactionRepository>();
            services.AddSingleton<IInterestRuleRepository, InterestRuleRepository>();
            services.AddSingleton<IBankService, BankService>();
            services.AddSingleton<BankAppUI>();

            var serviceProvider = services.BuildServiceProvider();

            var app = serviceProvider.GetRequiredService<BankAppUI>();
            app.Run();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Failed to start the application. " + ex.Message);
        }
    }
}