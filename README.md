# AgroLink API — Cattle Control and Traceability System

A mobile-first backend for managing and tracking bovine cattle at farms in Nicaragua. Built with ASP.NET Core 10, PostgreSQL, Firebase Authentication, and Google Cloud.

## Overview

AgroLink helps farmers in Boaco, Nicaragua manage their cattle operations with offline-first capabilities:

- **Animal Management** — Register, track, and manage individual animals
- **Hierarchical Organization** — Farm → Paddock → Lot → Animals
- **Movement Tracking** — Record animal and lot movements
- **Health Monitoring** — Daily checklists for animal presence and condition
- **Photo Management** — Upload and manage photos via Google Cloud Storage
- **Genealogy Tracking** — Maternal lineage tracking
- **Multi-ownership** — Shared ownership with percentage splits
- **Clinical Cases** — AI-assisted medication advice via OpenAI
- **Voice Commands** — Natural language voice input processed via Whisper + GPT-4o
- **Telegram Integration** — Bot-based farm notifications

## Technology Stack

| Layer | Technology |
|---|---|
| Runtime | .NET 10 / ASP.NET Core |
| Architecture | Clean Architecture + CQRS (MediatR) |
| Database | PostgreSQL 16 (Cloud SQL) via EF Core |
| Authentication | Firebase Authentication (JWT JWKS validation) |
| File Storage | Google Cloud Storage |
| AI | OpenAI (Whisper transcription, GPT-4o intent/advice, TTS) |
| Messaging | Telegram Bot API |
| Hosting | Google Cloud Run |
| IaC | Terraform (GCP) |
| Formatting | CSharpier |
| Static Analysis | JetBrains ReSharper |

## Project Structure

```
src/
├── AgroLink.Api/               # Presentation — thin controllers, middleware, DI wiring
│   ├── Controllers/
│   ├── DTOs/
│   ├── Filters/
│   ├── Middleware/
│   ├── Security/
│   └── Services/
├── AgroLink.Application/       # Use cases — CQRS commands/queries via MediatR
│   ├── Common/                 # Exceptions, shared services, utilities
│   ├── Features/               # One folder per domain area
│   │   ├── Animals/            # Commands + Queries (Create, Update, Move, Retire, Photos…)
│   │   ├── Auth/               # UpdateProfile, GetUserProfile
│   │   ├── Checklists/
│   │   ├── ClinicalCases/
│   │   ├── ExternalWorkers/    # Worker operation models
│   │   ├── Farms/
│   │   ├── Lots/
│   │   ├── Movements/
│   │   ├── Owners/
│   │   ├── Paddocks/
│   │   └── VoiceCommands/      # ProcessVoiceCommandInline (synchronous)
│   ├── Interfaces/
│   └── Mappings/
├── AgroLink.Domain/            # Core entities, interfaces, enums (no external dependencies)
│   ├── Constants/
│   ├── Entities/
│   ├── Enums/
│   ├── Interfaces/
│   └── Models/
└── AgroLink.Infrastructure/    # EF Core, GCS, OpenAI/Telegram services, repositories
    ├── Data/
    │   ├── Configurations/     # EF entity configurations
    │   └── Interceptors/
    ├── Migrations/
    ├── Repositories/
    └── Services/               # GcsStorageService, DirectExternalApiWorkerClient, …

tests/
├── AgroLink.Api.Tests/
├── AgroLink.Application.Tests/
├── AgroLink.Domain.Tests/
├── AgroLink.Infrastructure.Tests/
├── AgroLink.IntegrationTests/  # Real Postgres via Testcontainers
└── AgroLink.Workers.Tests/     # Tests for DirectExternalApiWorkerClient dispatch logic

iac/                            # Terraform — Cloud Run, Cloud SQL, GCS, IAM, Scheduler
```

## Authentication

AgroLink uses **Firebase Authentication**. Clients sign in with Firebase (email/password, Google, etc.) and send the resulting ID token as a Bearer token:

```
Authorization: Bearer <firebase-id-token>
```

`FirebaseUserMiddleware` validates the token against Google's public JWKS and auto-provisions an internal `User` record on first login. No separate registration endpoint exists — account creation is handled by Firebase.

## Getting Started (Local Development)

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker](https://www.docker.com/) (for docker-compose)
- A Firebase project with Authentication enabled
- A Google service account JSON (for GCS and Firebase Admin SDK)

### 1. Clone and restore tools

```bash
git clone <repository-url>
cd agrolink-api
dotnet tool restore
```

### 2. Start local infrastructure

```bash
cd iac
docker-compose up -d
```

This starts PostgreSQL (port 5432), pgAdmin (port 5050), and `fake-gcs-server` (port 4443).

### 3. Configure environment

Copy `src/AgroLink.Api/appsettings.Development.json` or use user secrets:

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=agrolinkdb;Username=agrolinkadmin;Password=agrolinkpassword" --project src/AgroLink.Api
dotnet user-secrets set "Firebase:ProjectId" "your-firebase-project-id" --project src/AgroLink.Api
dotnet user-secrets set "GCS:BucketName" "agrolink-files" --project src/AgroLink.Api
dotnet user-secrets set "OpenAI:ApiKey" "sk-..." --project src/AgroLink.Api
dotnet user-secrets set "Telegram:BotToken" "..." --project src/AgroLink.Api
dotnet user-secrets set "Telegram:WebhookSecretToken" "..." --project src/AgroLink.Api

# Point GCS client at the local emulator
export STORAGE_EMULATOR_HOST=http://localhost:4443
```

For Firebase Admin SDK (optional — only needed for token revocation checks):

```bash
export GOOGLE_APPLICATION_CREDENTIALS=/path/to/service-account.json
```

### 4. Apply migrations

```bash
make migrate
# or:
ASPNETCORE_ENVIRONMENT=Development dotnet ef database update \
  --project src/AgroLink.Infrastructure \
  --startup-project src/AgroLink.Api
```

### 5. Run the API

```bash
dotnet run --project src/AgroLink.Api
# Swagger: http://localhost:5001/swagger
```

## Key Makefile Commands

| Command | Description |
|---|---|
| `make build` | Build the solution |
| `make test` | Run all tests |
| `make migrate` | Apply EF Core migrations locally |
| `make format` | Format with CSharpier |
| `make check` | Check CSharpier formatting |
| `make inspect` | ReSharper static analysis |
| `make cleanup` | ReSharper automated cleanup |

## API Endpoints

### Auth
| Method | Path | Description |
|---|---|---|
| GET | `/api/auth/profile` | Get current user profile |
| PUT | `/api/auth/profile` | Update profile |

### Farms & Structure
| Method | Path |
|---|---|
| GET/POST | `/api/farms` |
| GET/PUT/DELETE | `/api/farms/{id}` |
| GET/POST | `/api/farms/{farmId}/paddocks` |
| GET/POST | `/api/farms/{farmId}/lots` |
| GET/POST | `/api/farms/{farmId}/animals` |

### Animals
| Method | Path | Description |
|---|---|---|
| GET | `/api/animals/{id}` | Get animal detail |
| PUT | `/api/animals/{id}` | Update animal |
| POST | `/api/animals/{id}/move` | Move to different lot |
| POST | `/api/animals/{id}/retire` | Mark as retired |
| GET/POST | `/api/animals/{id}/photos` | Manage photos |

### Voice Commands
| Method | Path | Description |
|---|---|---|
| POST | `/api/farms/{farmId}/voice/commands` | Upload audio, returns result synchronously |

Audio is transcribed via Whisper, intent extracted via GPT-4o, and farm entities resolved — all within the request. Response includes `intent`, `confidence`, `rawTranscription`, and `entities`.

### Clinical Cases
| Method | Path |
|---|---|
| GET/POST | `/api/farms/{farmId}/clinical-cases` |
| GET/PUT | `/api/farms/{farmId}/clinical-cases/{id}` |
| POST | `/api/farms/{farmId}/clinical-cases/{id}/events` |

### Telegram Webhook
| Method | Path | Description |
|---|---|---|
| POST | `/api/integrations/telegram/webhook` | Receives Telegram updates |

Validated against `X-Telegram-Bot-Api-Secret-Token` header.

### Internal (Cloud Scheduler)
| Method | Path | Description |
|---|---|---|
| POST | `/api/internal/cleanup` | Delete stale records (requires `X-Scheduler-Secret` header) |

## Database Migrations

```bash
# Create a new migration
dotnet ef migrations add MigrationName \
  --project src/AgroLink.Infrastructure \
  --startup-project src/AgroLink.Api

# Apply locally
dotnet ef database update \
  --project src/AgroLink.Infrastructure \
  --startup-project src/AgroLink.Api

# Generate SQL script
dotnet ef migrations script \
  --project src/AgroLink.Infrastructure \
  --startup-project src/AgroLink.Api \
  --output migration.sql
```

In CI/CD, migrations run as a Cloud Run Job before each deployment.

## Testing

```bash
dotnet test                              # all tests
dotnet test --configuration Release      # release mode
dotnet test --collect:"XPlat Code Coverage"
```

Integration tests use **Testcontainers** to spin up a real PostgreSQL instance and override Firebase auth with a local HMAC key. No external services are required.

## Deployment

Deployments are automated via GitHub Actions (`.github/workflows/deploy.yml`) on push to `main`:

1. Run all tests
2. Build Docker image and push to Artifact Registry
3. Build EF Core migration bundle and execute as a Cloud Run Job
4. Deploy new revision to Cloud Run with zero-downtime traffic shifting

Infrastructure is managed with Terraform (`iac/`). Required GitHub secrets:

| Secret | Description |
|---|---|
| `GCP_WORKLOAD_IDENTITY_PROVIDER` | Workload Identity Federation provider |
| `GCP_SERVICE_ACCOUNT` | CI/CD service account email |
| `GCP_PROJECT_ID` | GCP project ID |
| `CLOUD_SQL_CONNECTION_NAME` | Cloud SQL instance connection name |
| `DB_CONNECTION_STRING` | PostgreSQL connection string for migrations |

## Local Docker Build

```bash
docker build -t agrolink-api .
docker run -p 8080:8080 \
  -e ConnectionStrings__DefaultConnection="..." \
  -e Firebase__ProjectId="your-project" \
  agrolink-api
```

## Contributing

1. Create a feature branch from `main`
2. Make changes following Clean Architecture conventions
3. Run `make format` and `make check`
4. Ensure `dotnet test` passes
5. Open a pull request — the CI pipeline runs automatically
