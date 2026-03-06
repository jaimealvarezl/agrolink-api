# AgroLink API

## Project Overview
AgroLink is a backend system designed for cattle control and traceability for farmers in Nicaragua, supporting offline-first client operations.

**Technologies & Architecture:**
- **Framework:** .NET 10 (ASP.NET Core Web API)
- **Architecture:** Clean Architecture (Api, Application, Domain, Infrastructure layers) and CQRS using MediatR.
- **Database:** PostgreSQL via Entity Framework Core.
- **External Services:** AWS S3 for photo management and AWS Secrets Manager for secure configuration.
- **Authentication:** JWT Bearer tokens with Role-Based Access Control (Admin, User, Worker).
- **Infrastructure:** Provisioned via Terraform (`iac/` directory) targeting AWS. The API is deployed as an AWS Lambda function (arm64) behind API Gateway, with a direct connection to an Aurora PostgreSQL Serverless v2 database.

## Building and Running

The project includes a `Makefile` outlining the primary development commands:

- **Build:** `make build` (or `dotnet build agrolink-api.sln`)
- **Test:** `make test` (or `dotnet test agrolink-api.sln`)
- **Run Application:** `dotnet run --project src/AgroLink.Api`
- **Apply Local Migrations:** `make migrate` (Applies EF Core migrations to the local PostgreSQL database)
- **Format Code:** `make format` (Runs CSharpier formatter)
- **Check Formatting:** `make check` (Validates CSharpier formatting)
- **Static Analysis:** `make inspect` (Runs JetBrains ReSharper inspections)
- **Code Cleanup:** `make cleanup` (Runs automated ReSharper refactoring)

*Note: For local development, there is a Docker Compose setup (`iac/docker-compose.yml`) that provides PostgreSQL, pgAdmin, and MinIO (S3 compatible).*

## Development Conventions

- **Clean Architecture Strictness (per Jenil Sojitra's guidelines):**
  - **Dependency Inversion Principle:** All dependencies MUST point inward toward the Domain layer. Business logic must remain entirely independent of frameworks, databases, and external UI concerns.
  - **Domain:** Core entities, models, and interfaces. Must have NO outside dependencies (no infrastructure or application layers).
  - **Application:** Houses use cases and the CQRS pattern implementation using MediatR, organized by `Features/` (e.g., `Animals`, `Lots`). Interfaces for external concerns (Repository Pattern, external services) are defined here.
  - **Infrastructure:** Database access (EF Core), external services (AWS), and repository implementations. Depends only on the Application layer.
  - **Api (Presentation):** Entry point with **thin controllers** that merely orchestrate MediatR requests and define API-specific DTOs. Should not contain business logic.
- **CQRS:** Implement specific Commands (for mutations) and Queries (for reads) using MediatR, keeping handlers isolated.
- **Formatting & Quality:** The codebase strictly relies on **CSharpier** for formatting and **JetBrains ReSharper** tools for static analysis. Code should pass `make format` and `make check` without issues.
- **Migrations & Deployments:** Entity Framework Migrations are executed via a dedicated Migration AWS Lambda function as part of the GitHub Actions CI/CD pipeline (`.github/workflows/deploy.yml`). Avoid `Database.Migrate()` on application startup to prevent Lambda timeouts.
- **Secrets Management:** The application retrieves its database connection string at runtime securely from AWS Secrets Manager using the configured `AgroLink__DbSecretArn` environment variable.
