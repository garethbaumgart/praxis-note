# ADR-0001: Vertical Slice Architecture

## Status

Accepted

## Context

Traditional layered architecture (Controllers → Services → Repositories) leads to:
- Scattered feature code across multiple folders/projects
- High coupling between layers
- Difficulty understanding a complete feature
- Changes to one feature often touch many layers

We needed an architecture that keeps feature code cohesive and makes it easy to understand, modify, and delete features independently.

## Decision

Adopt Vertical Slice Architecture where code is organized by feature rather than by technical layer.

**Structure:**
```
Application/
├── Tasks/
│   ├── CreateTask/
│   │   ├── CreateTaskCommand.cs
│   │   ├── CreateTaskHandler.cs
│   │   └── CreateTaskValidator.cs
│   ├── GetTasks/
│   │   ├── GetTasksQuery.cs
│   │   └── GetTasksHandler.cs
│   └── ...
├── Users/
│   └── ...
```

Each slice contains everything needed for that operation: command/query, handler, validation, and DTOs.

## Consequences

**Positive:**
- Feature code is cohesive and easy to locate
- Low coupling between features
- Easy to delete or refactor a feature without affecting others
- New developers can understand a feature by looking at one folder
- Scales well as the application grows

**Negative:**
- Some code duplication between slices (acceptable trade-off)
- Less familiar to developers used to layered architecture
- Requires discipline to avoid cross-slice dependencies
