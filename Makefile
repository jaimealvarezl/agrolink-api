.PHONY: format check inspect cleanup test build

# Variables
SOLUTION=agrolink-api.sln

# Default target
all: build test

# Build the solution
build:
	dotnet build $(SOLUTION)

# Run tests
test:
	dotnet test $(SOLUTION)

# Format code using CSharpier
format:
	dotnet tool run csharpier format .

# Check code formatting using CSharpier
check:
	dotnet tool run csharpier check .

# ReSharper Static Analysis (Inspections)
inspect:
	dotnet jb inspectcode $(SOLUTION) --output=resharper-report.xml --severity=SUGGESTION

# ReSharper Code Cleanup (Auto-format and refactor)
cleanup:
	dotnet jb cleanupcode $(SOLUTION)
