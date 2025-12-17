# Gemini Guidelines and Memories

## Gemini Added Memories
- The user wants me to only add unit tests that are worth having. This means focusing on tests that cover critical business logic, complex algorithms, important invariants, and integration points. I should avoid over-testing trivial code and prioritize tests that provide high value, are maintainable, and have meaningful assertions.
- The user wants me to avoid adding unnecessary comments. I should add code comments sparingly, focusing on *why* something is done for complex logic, rather than *what* is done.

## Architectural and Code Style Guidelines (derived from "How to Design a Maintainable .NET Solution Structure for Growing Teams")

### 1. Solution Structure
- Organize the solution into `src/` and `tests/` directories.
  - `src/` contains core application projects (Domain, Application, Infrastructure, Api).
  - `tests/` contains test projects corresponding to the `src/` projects.

### 2. Project Naming
- Use the convention `{Company}.{Product}.{Layer}`. (e.g., `AgroLink.Api`, `AgroLink.Application`).

### 3. Layer Responsibilities
- **Domain Layer:** Contains core business logic, entities, value objects, enums, events, exceptions, and repository interfaces. It should have zero external dependencies.
- **Application Layer:** Contains application-specific business rules, use cases, commands, queries, and Data Transfer Objects (DTOs). It depends only on the `Domain` layer.
- **Infrastructure Layer:** Manages external concerns such as databases, external APIs, email services, and identity. It implements interfaces defined in the `Domain` or `Application` layers. It depends on the `Application` layer (and transitively `Domain`).
- **API Layer (Presentation):** The application's entry point (e.g., web host). It acts as a thin composition root that wires all components together. It depends on both `Application` and `Infrastructure`.
- **Shared.Kernel (Optional):** Used for cross-cutting primitives like base entities, value objects, domain events, result types, and common extensions. Should be kept minimal and used only when necessary.

### 4. Dependency Rule
- Dependencies must always point inward: outer layers can depend on inner layers, but never the reverse (e.g., `Api` → `Application` → `Domain`).

### 5. Folder Structure within Projects (Vertical Slices & Conventions)
- **General:** Use plural names for collections (e.g., `Entities/`, `Services/`) and singular names for features (e.g., `Orders/`, `Customers/`).
- **Feature-based Organization (Vertical Slices):** Within the `Application` layer (and potentially others), organize code by feature rather than by type. This means commands, queries, and DTOs related to a specific feature should reside within that feature's folder (e.g., `Features/Animals/Commands/`, `Features/Animals/DTOs/`).
- **File Naming:**
  - Prefer a "one class per file" convention.
  - Suffix files with their role (e.g., `OrderService.cs`, `OrderRepository.cs`).
  - Commands and queries should get full names (e.g., `PlaceOrderCommand.cs`, `GetAnimalByIdQuery.cs`).

### 6. Decentralized Dependency Injection
- Each layer should define its own dependency injection extension methods (e.g., `AddApplication(this IServiceCollection services)`, `AddInfrastructure(this IServiceCollection services, IConfiguration configuration)`) to encapsulate its service registrations. The API layer's `Program.cs` should then call these extension methods.
