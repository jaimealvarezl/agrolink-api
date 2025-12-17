# AgroLink API - Cattle Control and Traceability System

A comprehensive mobile-first MVP for managing and tracking bovine cattle at farms in Nicaragua. Built with ASP.NET Core 8, PostgreSQL, and AWS S3 for photo storage.

## 🌾 Overview

AgroLink is designed to help farmers in Boaco, Nicaragua manage their cattle operations with offline-first capabilities. The system supports:

- **Animal Management**: Register, track, and manage individual animals with detailed information
- **Hierarchical Organization**: Farm → Paddock → Lot → Animals structure
- **Movement Tracking**: Record and track animal and lot movements
- **Health Monitoring**: Daily checklists for animal presence and condition
- **Photo Management**: Upload and sync photos with AWS S3
- **Genealogy Tracking**: Maternal and paternal lineage tracking
- **Offline Support**: Work offline and sync when connectivity is available
- **Multi-ownership**: Support for shared ownership with percentage splits

## 🏗️ Architecture

### Project Structure
```
AgroLink.API/              # Presentation Layer (ASP.NET Core Web API)
├── Controllers/           # API Controllers, orchestrates commands and queries
├── DTOs/                  # API-specific Data Transfer Objects
├── Program.cs             # Application startup and Dependency Injection configuration
└── appsettings.json      # Configuration files

AgroLink.Application/      # Application Layer (CQRS and Application-specific Logic)
├── DTOs/                  # Application-specific Data Transfer Objects (for commands, queries, responses)
├── Features/              # Commands, Queries, and their Handlers (organized by feature)
│   ├── Auth/              #   ├── Commands/Login, Register, etc.
│   └── Animals/           #   └── Queries/GetById, GetAll, etc.
├── Interfaces/            # Defines interfaces for specific repositories and external services (implemented in Infrastructure)
└── Services/              # Application services (e.g., TokenExtractionService - not part of CQRS handlers)

AgroLink.Domain/           # Domain Layer (Core Business Logic)
├── Entities/              # Domain entities
├── Interfaces/            # Defines generic repository interfaces and core domain service interfaces
└── Exceptions/            # Custom domain exceptions (if any)

AgroLink.Infrastructure/   # Infrastructure Layer (Implementations of Interfaces, Data Access, External Services)
├── Data/                  # Entity Framework DbContext and migrations
├── Repositories/          # Implementations of repository interfaces defined in Domain and Application
├── Services/              # Implementations of external service interfaces defined in Application (e.g., JWT token, AWS S3)
└── Migrations/            # Database migrations

AgroLink.Tests/            # Unit Tests
└── [Test files]           # NUnit test classes for Application and Domain logic

AgroLink.IntegrationTests/ # Integration Tests
└── [Test files]           # Integration test classes for API and Infrastructure components
```

### Technology Stack
- **Backend**: ASP.NET Core 8
- **Database**: PostgreSQL with Entity Framework Core
- **Authentication**: JWT Bearer tokens
- **Photo Storage**: AWS S3
- **Password Hashing**: BCrypt
- **API Documentation**: Swagger/OpenAPI
- **Code Formatting**: CSharpier (local .NET tool)

## 📊 Database Schema

### Core Entities

#### Farm
- `Id` (Primary Key)
- `Name` (Required, Max 200 chars)
- `Location` (Optional, Max 500 chars)
- `CreatedAt`, `UpdatedAt`

#### Paddock
- `Id` (Primary Key)
- `Name` (Required, Max 200 chars)
- `FarmId` (Foreign Key to Farm)
- `CreatedAt`, `UpdatedAt`

#### Lot
- `Id` (Primary Key)
- `Name` (Required, Max 200 chars)
- `PaddockId` (Foreign Key to Paddock)
- `Status` (ACTIVE, INACTIVE, MAINTENANCE)
- `CreatedAt`, `UpdatedAt`

#### Animal
- `Id` (Primary Key)
- `Tag` (Required, Unique, Max 50 chars)
- `Name` (Optional, Max 200 chars)
- `Color`, `Breed` (Optional, Max 100 chars each)
- `Sex` (MALE, FEMALE)
- `Status` (ACTIVE, SOLD, DEAD, MISSING)
- `BirthDate` (Optional)
- `LotId` (Foreign Key to Lot)
- `MotherId`, `FatherId` (Optional, Self-referencing)
- `CreatedAt`, `UpdatedAt`

#### Owner & AnimalOwner
- `Owner`: Basic owner information
- `AnimalOwner`: Junction table for shared ownership with percentage

#### Movement
- `Id` (Primary Key)
- `EntityType` (LOT, ANIMAL)
- `EntityId` (ID of the moved entity)
- `FromId`, `ToId` (Previous and new locations)
- `At` (Movement timestamp)
- `Reason` (Optional movement reason)
- `UserId` (Who performed the movement)

#### Checklist & ChecklistItem
- `Checklist`: Daily health/presence checks
- `ChecklistItem`: Individual animal status in checklist
- `Present` (Boolean), `Condition` (OK, OBS, URG)

#### Photo
- `Id` (Primary Key)
- `EntityType` (ANIMAL, CHECKLIST)
- `EntityId` (ID of associated entity)
- `UriLocal` (Local file path)
- `UriRemote` (AWS S3 URL)
- `Uploaded` (Sync status)

## 🚀 Getting Started

### Prerequisites
- .NET 8 SDK (see `global.json` for version requirements)
- PostgreSQL 12+ (or use Docker)
- AWS Account (for S3 photo storage)
- Git

### Installation

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd agrolink-api
   ```

2. **Restore .NET tools**
   ```bash
   dotnet tool restore
   ```

3. **Configure Database**
   ```bash
   # Update connection string in appsettings.json
   "ConnectionStrings": {
     "DefaultConnection": "Host=localhost;Database=agrolink;Username=postgres;Password=yourpassword"
   }
   ```

4. **Configure AWS S3**
   ```bash
   # Set AWS credentials (via AWS CLI, environment variables, or IAM roles)
   aws configure

   # Update S3 bucket name in appsettings.json
   "AWS": {
     "S3BucketName": "your-agrolink-photos-bucket"
   }
   ```

5. **Install Dependencies**
   ```bash
   dotnet restore
   ```

6. **Run Database Migrations**
   ```bash
   dotnet ef database update --project AgroLink.Infrastructure --startup-project AgroLink.API
   ```

7. **Run the Application**
   ```bash
   dotnet run --project AgroLink.API
   ```

8. **Access Swagger Documentation**
   Navigate to `https://localhost:5001/swagger` or `http://localhost:5000/swagger` (check `launchSettings.json` for configured ports)

### Database Setup

The application uses Entity Framework Core migrations for database schema management.

```bash
# Apply existing migrations
dotnet ef database update --project AgroLink.Infrastructure --startup-project AgroLink.API

# Create new migration (if you modify entities)
dotnet ef migrations add YourMigrationName --project AgroLink.Infrastructure --startup-project AgroLink.API

# Generate SQL script (optional, for review or manual execution)
dotnet ef migrations script --project AgroLink.Infrastructure --startup-project AgroLink.API --output migration.sql
```

## 📱 API Endpoints

### Authentication
- `POST /api/auth/login` - User login
- `POST /api/auth/register` - User registration
- `GET /api/auth/profile` - Get current user profile
- `POST /api/auth/validate` - Validate JWT token

### Farms
- `GET /api/farms` - Get all farms
- `GET /api/farms/{id}` - Get farm by ID
- `POST /api/farms` - Create new farm
- `PUT /api/farms/{id}` - Update farm
- `DELETE /api/farms/{id}` - Delete farm

### Paddocks
- `GET /api/paddocks` - Get all paddocks
- `GET /api/paddocks/farm/{farmId}` - Get paddocks by farm
- `GET /api/paddocks/{id}` - Get paddock by ID
- `POST /api/paddocks` - Create new paddock
- `PUT /api/paddocks/{id}` - Update paddock
- `DELETE /api/paddocks/{id}` - Delete paddock

### Lots
- `GET /api/lots` - Get all lots
- `GET /api/lots/paddock/{paddockId}` - Get lots by paddock
- `GET /api/lots/{id}` - Get lot by ID
- `POST /api/lots` - Create new lot
- `PUT /api/lots/{id}` - Update lot
- `DELETE /api/lots/{id}` - Delete lot
- `POST /api/lots/{id}/move` - Move lot to different paddock

### Animals
- `GET /api/animals` - Get all animals
- `GET /api/animals/lot/{lotId}` - Get animals by lot
- `GET /api/animals/{id}` - Get animal by ID
- `GET /api/animals/{id}/genealogy` - Get animal genealogy tree
- `POST /api/animals` - Create new animal
- `PUT /api/animals/{id}` - Update animal
- `DELETE /api/animals/{id}` - Delete animal
- `POST /api/animals/{id}/move` - Move animal to different lot

### Checklists
- `GET /api/checklists` - Get all checklists
- `GET /api/checklists/scope/{scopeType}/{scopeId}` - Get checklists by scope
- `GET /api/checklists/{id}` - Get checklist by ID
- `POST /api/checklists` - Create new checklist
- `PUT /api/checklists/{id}` - Update checklist
- `DELETE /api/checklists/{id}` - Delete checklist

### Movements
- `GET /api/movements/entity/{entityType}/{entityId}` - Get movements by entity
- `GET /api/movements/animal/{animalId}/history` - Get animal movement history
- `POST /api/movements` - Create new movement record

### Photos
- `GET /api/photos/entity/{entityType}/{entityId}` - Get photos by entity
- `POST /api/photos/upload` - Upload new photo
- `DELETE /api/photos/{id}` - Delete photo
- `POST /api/photos/sync` - Sync pending photos to S3

## 🔐 Authentication

The API uses JWT Bearer token authentication. Include the token in the Authorization header:

```
Authorization: Bearer <your-jwt-token>
```

### User Roles
- `ADMIN`: Full system access
- `USER`: Standard user access
- `WORKER`: Limited access for field workers

## 📸 Photo Management

Photos are stored locally first and then synced to AWS S3 when connectivity is available:

1. **Upload**: Photos are stored locally with `Uploaded = false`
2. **Sync**: Background process uploads to S3 and updates `Uploaded = true`
3. **Offline**: Photos remain local until connectivity is restored

## 🔄 Offline Support

The system is designed for offline-first operation:

- All data operations work without internet connectivity
- Photos are stored locally and synced when online
- Movement and checklist data is queued for sync
- JWT tokens are cached for offline authentication

## 🚀 Deployment

### Docker

Build and run with Docker:

```bash
# Build the image
docker build -t agrolink-api .

# Run the container
docker run -p 8080:80 agrolink-api
```

Example Dockerfile:
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["AgroLink.API/AgroLink.API.csproj", "AgroLink.API/"]
COPY ["AgroLink.Core/AgroLink.Core.csproj", "AgroLink.Core/"]
COPY ["AgroLink.Infrastructure/AgroLink.Infrastructure.csproj", "AgroLink.Infrastructure/"]
RUN dotnet restore "AgroLink.API/AgroLink.API.csproj"
COPY . .
WORKDIR "/src/AgroLink.API"
RUN dotnet build "AgroLink.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AgroLink.API.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AgroLink.API.dll"]
```

### AWS Lambda (Future)
The API can be adapted for AWS Lambda serverless deployment:
1. Package the application
2. Deploy to AWS Lambda
3. Configure API Gateway
4. Set up RDS PostgreSQL instance
5. Configure S3 bucket for photos

## 🔧 Development

### Code Formatting

This project uses **CSharpier** for consistent code formatting. CSharpier is installed as a local .NET tool and integrated into the GitHub Actions CI pipeline.

#### Format all files
```bash
dotnet tool run csharpier format .
```

#### Check formatting
```bash
dotnet tool run csharpier check .
```

#### IDE Integration
- **Visual Studio**: Install the CSharpier extension
- **VS Code**: Install the CSharpier extension
- **JetBrains Rider**: Install the CSharpier plugin

### CI/CD Pipeline

The GitHub Actions workflow (`.github/workflows/ci.yml`) includes:
- **Format Check**: Validates code formatting with CSharpier
- **Build**: Compiles the solution in Release mode
- **Test**: Runs unit tests with coverage reporting
- **Secret Detection**: Scans for exposed secrets using TruffleHog
- **Dependency Scanning**: Checks for vulnerable NuGet packages

The pipeline runs automatically on:
- Pushes to `main` and `develop` branches
- Pull requests targeting `main` and `develop` branches

## 🧪 Testing

### Running Tests

```bash
# Run all unit tests
dotnet test

# Run tests with coverage
dotnet test --collect:"XPlat Code Coverage"

# Run tests in Release mode
dotnet test --configuration Release
```

### API Testing with Swagger

1. Start the application: `dotnet run --project AgroLink.API`
2. Navigate to `/swagger`
3. Use the "Authorize" button to set JWT token
4. Test endpoints with sample data

### Sample Data

Create a test farm with the following structure:
```
Farm: "Finca San José"
├── Paddock: "Potrero Norte"
│   ├── Lot: "Lote A" (10 animals)
│   └── Lot: "Lote B" (8 animals)
└── Paddock: "Potrero Sur"
    ├── Lot: "Lote C" (12 animals)
    └── Lot: "Lote D" (6 animals)
```

## 📋 Key Features

### ✅ Implemented
- Complete domain model with all entities
- RESTful API with full CRUD operations
- JWT authentication and authorization
- Photo upload with AWS S3 integration
- Movement tracking for animals and lots
- Daily checklist system
- Genealogy tracking
- Multi-ownership support
- Offline-first architecture

### 🔄 Future Enhancements
- Mobile app integration
- Real-time notifications
- Advanced reporting and analytics
- Integration with external systems
- Bulk operations
- Data export/import
- Advanced search and filtering

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Make your changes
4. Format code with CSharpier (`dotnet tool run csharpier format .`)
5. Run tests (`dotnet test`)
6. Commit your changes (`git commit -m 'Add amazing feature'`)
7. Push to the branch (`git push origin feature/amazing-feature`)
8. Open a Pull Request

### Code Standards
- Follow Clean Architecture principles
- Write unit tests for new features
- Ensure all tests pass before submitting PR
- Code must pass formatting checks (CSharpier)

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.

## 📚 Additional Resources

- [Clean Architecture Guidelines](docs/clean-architecture.md) - Architecture documentation
- [Testing Guidelines](docs/testing.md) - Testing best practices
- [API Documentation](https://localhost:5001/swagger) - Interactive API documentation (when running)

## 🆘 Support

For support and questions:
- Create an issue in the repository
- Check existing documentation
- Review the Swagger API documentation

---

**AgroLink API** - Empowering Nicaraguan farmers with modern cattle management technology. 🌾🐄
