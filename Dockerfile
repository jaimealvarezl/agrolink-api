# Use the SDK image for building the application
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# Copy the solution file
COPY *.sln .

# Copy project files (maintaining structure is important for restore)
COPY src/AgroLink.Api/*.csproj ./src/AgroLink.Api/
COPY src/AgroLink.Application/*.csproj ./src/AgroLink.Application/
COPY src/AgroLink.Domain/*.csproj ./src/AgroLink.Domain/
COPY src/AgroLink.Infrastructure/*.csproj ./src/AgroLink.Infrastructure/

# Copy test project files
COPY tests/AgroLink.Api.Tests/*.csproj ./tests/AgroLink.Api.Tests/
COPY tests/AgroLink.Application.Tests/*.csproj ./tests/AgroLink.Application.Tests/
COPY tests/AgroLink.Domain.Tests/*.csproj ./tests/AgroLink.Domain.Tests/
COPY tests/AgroLink.Infrastructure.Tests/*.csproj ./tests/AgroLink.Infrastructure.Tests/
COPY tests/AgroLink.IntegrationTests/*.csproj ./tests/AgroLink.IntegrationTests/
COPY tests/TestProject1/*.csproj ./tests/TestProject1/

# Restore dependencies
RUN dotnet restore

# Copy the remaining source code
COPY . .

# Build and publish the API
WORKDIR /app/src/AgroLink.Api
RUN dotnet publish -c Release -o /app/publish

# Use the runtime image for the final container
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .

# Expose ports
EXPOSE 8080
EXPOSE 8081

ENTRYPOINT ["dotnet", "AgroLink.Api.dll"]
