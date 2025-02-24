# Awesome GIC Bank

## Overview

AwesomeGICBank is a simple banking system implemented as a .NET 8 console application. The application demonstrates a clean, modular architecture following the SOLID principles, with dependency injection and a three-layer design (Data, Service, and Presentation). It simulates a banking environment by allowing users to input transactions, define interest rules, and print account statements.

## Technical Specs

- **Target Framework:** .NET 8 (or later)
- **Testing Framework:** xUnit with Moq for mocking dependencies
- **Dependency Injection:** Implemented using [Microsoft.Extensions.DependencyInjection](https://www.nuget.org/packages/Microsoft.Extensions.DependencyInjection/)

## Features

- **Input Transactions:**
  - Supports deposits and withdrawals.
  - Automatically creates an account on the first transaction.
  - Enforces business rules (e.g., first transaction cannot be a withdrawal).
- **Define Interest Rules:**
  - Allows setting interest rates effective from a given date.
  - Ensures only the latest rule for a given day is kept.
  - Validates that interest rates are greater than 0 and less than 100.
- **Print Account Statement:**
  - Generates a monthly account statement.
  - Computes daily running balances and applies interest on end-of-day balances.
  - Displays transactions along with an interest credit entry, if applicable.

## Architecture

- **Domain Layer:** Contains entity classes such as Transaction and InterestRule, and the TransactionType enum.
- **Data Layer:** Provides repository interfaces and in-memory repository implementations (`ITransactionRepository` and `IInterestRuleRepository`).
  Data is stored in in-memory lists (no external database).
- **Service Layer:** Implements business logic in the `BankService` class, including validation, transaction processing, and interest calculation.
  The service is designed to be testable via dependency injection.
- **Presentation (Console UI):** The `BankAppUI` class handles console input and output. It validates raw input, maps data into DTOs, and communicates with the service layer.

## Installation

1. **Clone the Repository:**
   ```bash
   git clone https://github.com/Prashantjha73/AwesomeGICBank.git
   cd AwesomeGICBank
   ```
2. **Restore Dependencies & Build:**
   ```bash
   dotnet restore
   dotnet build
   ```
3. **Restore Dependencies & Build:**
   Verify that the .NET 8 SDK is installed on your machine.

## Usage

1. **Clone the Repository:**

   ```bash
   dotnet run --project AwesomeGICBank
   ```

   You will be prompted to:

   ```bash
   Welcome to AwesomeGIC Bank! What would you like to do?
   [T] Input transactions
   [I] Define interest rules
   [P] Print statement
   [Q] Quit
   >
   ```

## Code Organization

- **/Models:**  
  Contains models:
  - `Enums`/`TransactionType` (enum)
  - `InterestRule`
  - `Transaction`
- **/Services**  
  Contains service interfaces and implementations:
  - `IBankService.cs` and `BankService.cs`
- **/Data:**
  Storage management
  - `IInterestRuleRepository.cs` and `InterestRuleRepository.cs`
  - `ITransactionRepository.cs` and `TransactionRepository.cs`
- **UI:**
  - `BankAppUI`
- **Testing Layer:**
  - Contains xUnit test (e.g., `BankServiceTests.cs`).

## Example Scenarios

- **Input Transactions:** When selecting [T] Input transactions, you can enter:
  ```bash
    20241101 A1 D 250
    20241102 A1 D 100
    20241110 A1 W 10
    20241115 A1 D 1000
    20241127 A1 W 128
  ```
  The system creates the account and updates the statement after each transaction.
- **Define Interest Rules:** Selecting [I] Define interest rules, you might enter:
  ```bash
    20240101 R1 2.2
    20241111 R2 2.5
    20241126 R3 1.8
  ```
  The latest interest rule for a given day is applied.
- **Print Statement:** When choosing [P] Print statement, provide input like:
  ```bash
    A1 202411
  ```
  The application prints the account statement, showing all transactions for November 2024 along with the calculated interest (for example, interest of 1.61 and a final balance of 1213.61).

## Testing Steps

1. **Run Automated Tests:** Execute the following command in the root directory:
   ```bash
   dotnet test
   ```
2. **Review Test Output:**

   - **Bank Service:** Checking different scenarios of transaction and interest calculations (`BankServiceTests`).

3. **Manual Testing:** Run the application using `dotnet run` and try various scenarios to validate application behavior.
