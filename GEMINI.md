# AgroLink API

## Project Overview
AgroLink is a backend system designed for cattle control and traceability for farmers in Nicaragua, supporting offline-first client operations.

**Technologies & Architecture:**
- **Framework:** .NET 10 (ASP.NET Core Web API)
- **Architecture:** Clean Architecture (Api, Application, Domain, Infrastructure layers) and CQRS using MediatR.
- **Database:** PostgreSQL via Entity Framework Core. Deployed on Cloud SQL PostgreSQL 16.
- **File Storage:** Google Cloud Storage (GCS) via `Google.Cloud.Storage.V1`.
- **Authentication:** Firebase Authentication. The API validates Firebase ID tokens using Google's public JWKS (JwtBearer + Authority). `FirebaseUserMiddleware` auto-provisions internal `User` records on first login and injects `userid`/`role` claims for downstream authorization.
- **External AI/Messaging:** OpenAI (Whisper transcription, GPT-4o intent extraction, TTS) and Telegram Bot API. All calls are made directly from the API — no proxy layer needed (Cloud Run has unrestricted internet access).
- **Infrastructure:** Provisioned via Terraform (`iac/` directory) targeting GCP. The API is deployed as a Cloud Run service with a Cloud SQL Unix socket connection.

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

*Note: For local development, there is a Docker Compose setup (`iac/docker-compose.yml`) that provides PostgreSQL, pgAdmin, and `fake-gcs-server` (GCS emulator). Set `STORAGE_EMULATOR_HOST=http://gcs-emulator:4443` to redirect GCS calls locally.*

## Development Conventions

- **Clean Architecture Strictness:**
  - **Dependency Inversion Principle:** All dependencies MUST point inward toward the Domain layer. Business logic must remain entirely independent of frameworks, databases, and external UI concerns.
  - **Domain:** Core entities, models, and interfaces. Must have NO outside dependencies.
  - **Application:** Use cases and CQRS handlers using MediatR, organized by `Features/`. Interfaces for repositories and external services are defined here.
  - **Infrastructure:** EF Core, GCS, repository implementations, and service adapters. Depends only on Application.
  - **Api (Presentation):** Thin controllers that orchestrate MediatR requests. No business logic.
- **CQRS:** Commands (mutations) and Queries (reads) via MediatR, handlers isolated per feature.
- **Voice Commands:** The `POST /api/farms/{farmId}/voice/commands` endpoint processes audio synchronously (upload → Whisper transcription → GPT-4o intent → entity resolution) and returns the result directly. No job polling.
- **Formatting & Quality:** **CSharpier** for formatting and **JetBrains ReSharper** for static analysis. Code must pass `make format` and `make check`.
- **Migrations:** EF Core migration bundles are built in CI and executed as a Cloud Run Job before each deployment. Do not call `Database.Migrate()` on startup.
- **Secrets:** Application secrets (DB password, API keys) are stored in GCP Secret Manager and injected as environment variables into the Cloud Run service via Terraform.
- **Internal Endpoints:** `POST /api/internal/cleanup` is called daily by Cloud Scheduler. It requires an `X-Scheduler-Secret` header matching the `Internal:SchedulerSecret` configuration value.
