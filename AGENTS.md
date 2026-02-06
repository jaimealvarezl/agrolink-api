# AgroLink API - Development Guidelines

This document provides instructions for AI agents and developers working on the AgroLink API codebase.
Adhere strictly to these guidelines to maintain consistency and quality.

## 1. Environment & Commands

### Build & Run
- **Build Solution:** `dotnet build agrolink-api.sln`
- **Run API:** `dotnet run --project src/AgroLink.Api`
- **Restore Dependencies:** `dotnet restore`
- **Database Migrations:**
  `dotnet ef database update --project src/AgroLink.Infrastructure --startup-project src/AgroLink.Api`

### Testing
- **Framework:** NUnit with Shouldly assertions.
- **Run All Tests:** `dotnet test`
- **Run Single Test:** `dotnet test --filter "TestName"`
  - *Example:* `dotnet test --filter "User_CanBeCreated_WithValidData"`
- **Run Tests in Project:** `dotnet test tests/AgroLink.Domain.Tests`

### Formatting & Linting
- **Format Code:** `dotnet tool run csharpier format .`
  - *Note:* Always run this after making changes.
- **Check Formatting:** `dotnet tool run csharpier check .`
- **Static Analysis:** `dotnet jb inspectcode agrolink-api.sln`
  - *Note:* Addresses code smells and potential bugs.

## 2. Architecture & Structure

### Clean Architecture
- **Domain:** Core entities, interfaces, exceptions. No external dependencies.
- **Application:** Business logic, CQRS (MediatR), DTOs. Depends on Domain.
- **Infrastructure:** DB context, repositories, external services. Depends on Application.
- **API:** Controllers, startup configuration. Depends on Application & Infrastructure.

### Vertical Slices
- Organize `Application` layer by Feature, not by type.
- Structure: `src/AgroLink.Application/Features/{FeatureName}/{Commands|Queries}/{ActionName}/`
  - *Example:* `Features/Animals/Commands/Create/CreateAnimalCommand.cs`

## 3. Code Style & Conventions

### C# Modern Standards
- **Namespaces:** Use file-scoped namespaces (`namespace AgroLink.Api;`).
- **Constructors:** Use Primary Constructors for classes with simple dependency injection.
  - *Example:* `public class MyHandler(IRepository repo) ...`
- **DTOs:** Use `record` types for DTOs and MediatR Requests.
  - *Example:* `public record CreateFarmCommand(string Name) : IRequest<FarmDto>;`
- **Variables:** Use `var` when the type is obvious from the right-hand side.

### Naming
- **Classes:** PascalCase (e.g., `FarmRepository`).
- **Interfaces:** PascalCase with 'I' prefix (e.g., `IFarmRepository`).
- **Methods:** PascalCase (e.g., `GetByIdAsync`).
- **Async:** Suffix async methods with `Async` (e.g., `SaveChangesAsync`).
- **Files:** One class per file. File name must match class name.

### Error Handling
- Use **Exceptions** for flow control (not Result pattern).
- Throw specific exceptions (e.g., `UnauthorizedAccessException`, `NotFoundException`).
- Middleware in the API layer handles exception-to-HTTP-response mapping.

### Dependencies
- **MediatR:** Used for all Commands and Queries.
- **Entity Framework Core:** Used for data access.
- **AutoMapper:** Avoid if possible; prefer manual mapping in Handlers for clarity.

## 4. Testing Guidelines

- **Unit Tests:** Place in `tests/{Project}.Tests`.
- **Naming:** `MethodName_StateUnderTesting_ExpectedBehavior`.
  - *Example:* `User_CanBeCreated_WithValidData`
- **Assertions:** Use `Shouldly`.
  - *Example:* `user.Name.ShouldBe("Test");`
- **Focus:** Test business logic, complex algorithms, and edge cases. Avoid testing trivial getters/setters.
- **Mocking:** Use `NSubstitute` (or similar, inferred from context) for interface mocking.

## 5. Agent Instructions

- **No "Checking" Comments:** Do not add "Checking..." or "Analyzing..." comments in the code.
- **Minimal Comments:** Comment *why*, not *what*. Code should be self-documenting.
- **Safe Refactoring:** Always run tests before and after refactoring.
- **New Files:** Ensure new files are created in the correct Feature folder.
- **Verification:** Always run `dotnet build` and `dotnet test` before confirming a task is complete.
