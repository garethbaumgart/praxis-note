# ADR-0002: CQRS Pattern (without MediatR)

## Status

Accepted

## Context

We needed a pattern that:
- Works well with Vertical Slice Architecture
- Separates read and write concerns
- Makes operations explicit and testable
- Provides a clear structure for each operation

Traditional service classes tend to grow large with mixed read/write methods, making them hard to test and maintain.

Many CQRS implementations use MediatR for dispatching commands/queries, but this adds:
- Another dependency to maintain
- Indirection that can make debugging harder
- Magic that obscures the call path
- Overhead that isn't justified for simpler applications

## Decision

Adopt CQRS pattern **without MediatR**. Use simple handler classes with direct dependency injection.

**Commands** (write operations):
- `CreateTaskCommand`, `UpdateTaskCommand`, `DeleteTaskCommand`
- Return the affected entity or void
- May have side effects

**Queries** (read operations):
- `GetTasksQuery`, `GetTaskByIdQuery`
- Return data, never modify state
- Idempotent and cacheable

Each command/query has a dedicated handler class, injected directly where needed.

**Example:**
```csharp
// Direct injection, no MediatR dispatcher
public class TasksEndpoints
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/api/tasks", async (
            CreateTaskCommand command,
            CreateTaskHandler handler) =>
        {
            var result = await handler.HandleAsync(command);
            return Results.Created($"/api/tasks/{result.Id}", result);
        });
    }
}
```

**When to reconsider MediatR:**
- If we need pipeline behaviors (logging, validation, caching) across many handlers
- If the number of handlers makes manual DI registration painful
- If we adopt event-driven architecture

## Consequences

**Positive:**
- Clear separation between reads and writes
- Each operation is a focused, testable unit
- Direct, debuggable call paths (no magic)
- One less dependency to maintain
- Simpler mental model for developers

**Negative:**
- More files/classes than traditional service approach
- Cross-cutting concerns must be added manually to each handler
- May need to adopt MediatR later if complexity grows
