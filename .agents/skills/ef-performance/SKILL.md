---
name: ef-performance
description: Entity Framework Core performance best practices, diagnostics, modeling, and advanced optimization techniques. Use when writing, reviewing, or optimizing EF Core data access code.
---

# Entity Framework Core Performance Guide

Optimize EF Core applications by following official performance best practices, from modeling to runtime execution.

**Reference:** [EF Core Performance Documentation](https://learn.microsoft.com/en-us/ef/core/performance/)

## Core Philosophy

1. **Identify and Measure:** Use `BenchmarkDotNet` for benchmarking. Don't optimize without data.
2. **Know the SQL:** Inspect the generated SQL to ensure it matches expectations.
3. **Minimize Roundtrips:** Reduce the number of times the application contacts the database.
4. **Minimize Data Transfer:** Only fetch the columns and rows you actually need.

## Performance Diagnosis

### 1. Logging and Correlation
Capture command execution times using `LogTo` (simple logging) or `ILoggerFactory`. Use **Query Tags** to correlate LINQ queries with SQL logs.

```csharp
var results = await context.Blogs
    .TagWith("SearchBlogsByRating")
    .Where(b => b.Rating > 3)
    .ToListAsync();
```

### 2. Query Plan Analysis
Identify slow queries and analyze their **Execution Plans** using database tools (e.g., SQL Server Management Studio, Azure Data Studio). Look for missing indexes and costly joins.

### 3. Metrics and EventCounters
Monitor `Microsoft.EntityFrameworkCore` metrics:
- **Query Cache Hit Rate:** Should be near 100%. Low rates indicate dynamic query issues.
- **Active DbContexts:** Helps detect context leaks.
- **Execution Time:** Average command execution duration.

## Modeling for Performance

### 1. Inheritance Mapping
- **TPH (Table-per-Hierarchy):** Generally the most performant (single table, no joins).
- **TPC (Table-per-Concrete-Type):** Excellent for querying a single leaf type.
- **TPT (Table-per-Type):** Use sparingly; involves complex joins that often degrade performance.

### 2. Denormalization and Caching
- **Stored Computed Columns:** Cache calculations within the same table.
- **Triggers:** Recalculate aggregate values (e.g., average rating) on update.
- **Materialized Views:** Pre-calculate complex query results at the database level.

## Efficient Querying

### 1. Projection (Fetch only what you need)
Always use `.Select()` to project only required properties. Avoid fetching entire entities for read-only operations.

```csharp
var names = await context.Blogs.Select(b => b.Name).ToListAsync();
```

### 2. No-Tracking Queries
For read-only operations, use `.AsNoTracking()` to avoid the overhead of change tracking and identity resolution.

```csharp
var blogs = await context.Blogs.AsNoTracking().ToListAsync();
```

### 3. Loading Related Data
- **Eager Loading:** Use `.Include()` for data you know you'll need.
- **Avoid N+1:** Never access navigation properties inside a loop with lazy loading enabled.
- **Split Queries:** Use `.AsSplitQuery()` to avoid "Cartesian Explosion" in 1:N joins.

## Efficient Updating

### 1. Bulk Operations (EF Core 7+)
Use `ExecuteUpdate` and `ExecuteDelete` for mass updates without loading entities into memory.

```csharp
await context.Employees
    .Where(e => e.Salary < 50000)
    .ExecuteUpdateAsync(s => s.SetProperty(e => e.Salary, e => e.Salary + 1000));
```

### 2. Batching
EF Core automatically batches multiple changes into a single roundtrip on `SaveChanges`. Avoid calling `SaveChanges` inside loops.

## Advanced Optimization

### 1. DbContext Pooling
Reduces context setup overhead in high-traffic applications.

```csharp
builder.Services.AddDbContextPool<MyDbContext>(o => o.UseSqlServer(connectionString));
```

### 2. Compiled Queries
Pre-compile LINQ queries into delegates for absolute maximum performance in hot paths.

```csharp
private static readonly Func<MyDbContext, int, Task<Blog?>> _getBlogById =
    EF.CompileAsyncQuery((MyDbContext context, int id) => context.Blogs.FirstOrDefault(b => b.Id == id));
```

### 3. NativeAOT and Precompiled Queries (EF Core 8+)
For startup performance, enable NativeAOT and use query precompilation via interceptors.
- Add `Microsoft.EntityFrameworkCore.Tasks` package.
- Use `dotnet publish` to statically identify queries and generate interceptors.

## Checklist for Performance Reviews
- [ ] Are we using `.Select()` to limit fetched columns?
- [ ] Are read-only queries using `.AsNoTracking()`?
- [ ] Is the N+1 problem avoided?
- [ ] Is `AsSplitQuery()` used for multiple 1:N joins?
- [ ] Are mass updates using `ExecuteUpdate`/`ExecuteDelete`?
- [ ] Are queries parameterized (avoiding constants in LINQ)?
- [ ] Is inheritance mapped efficiently (TPH/TPC preferred over TPT)?
- [ ] Are slow queries identified via `TagWith` and Execution Plans?
