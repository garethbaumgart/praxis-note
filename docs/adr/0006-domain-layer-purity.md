# ADR-0006: Domain Layer Purity

## Status

Accepted

## Context

The Domain layer is the core of the application, containing business logic and rules. To maintain a clean architecture and testability, we need strict guidelines about what belongs in the Domain layer and what doesn't.

## Decision

The Domain layer must remain **pure** - no external dependencies, no infrastructure concerns.

### Rules

**DO:**
- Define aggregates as consistency boundaries (e.g., `TaskItem`, `User`, `Note`)
- Use value objects for immutable concepts (e.g., `Email`, `TaskStatus`, `DueDate`)
- Define repository interfaces in Domain (e.g., `ITaskRepository`)
- Raise domain events for significant state changes
- Keep entities focused on behavior, not just data
- Use factory methods for complex object creation
- Validate invariants in constructors and methods

**DON'T:**
- Reference EF Core, ASP.NET, or any infrastructure packages
- Use `DateTime.Now` directly (inject `IDateTimeProvider` or pass time as parameter)
- Throw infrastructure exceptions (use domain-specific exceptions)
- Include DTOs or API models
- Reference the Application, Infrastructure, or Web layers

### Structure

```
Domain/
├── Aggregates/
│   ├── TaskItem/
│   │   ├── TaskItem.cs          # Aggregate root
│   │   ├── Comment.cs           # Entity within aggregate
│   │   └── ITaskRepository.cs   # Repository interface
│   └── User/
│       ├── User.cs
│       └── IUserRepository.cs
├── ValueObjects/
│   ├── Email.cs
│   ├── TaskStatus.cs
│   └── DueDate.cs
├── Common/
│   ├── Entity.cs
│   ├── AggregateRoot.cs
│   └── ValueObject.cs
└── Events/
    ├── TaskCreatedEvent.cs
    └── TaskCompletedEvent.cs
```

### Testing

The Domain layer should be 100% unit testable with no mocking of infrastructure. Tests should read like specifications of business rules.

## Consequences

**Positive:**
- Domain logic is isolated and easy to test
- No coupling to frameworks or databases
- Business rules are explicit and centralized
- Can swap infrastructure without touching domain
- Onboarding: new devs understand business logic without framework knowledge

**Negative:**
- Requires discipline to maintain purity
- May need to create abstractions for things like time, random, etc.
- More upfront design work
