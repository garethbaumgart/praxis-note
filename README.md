# PraxisNote

A note-first task management system. Write notes with checkboxes that automatically become tasks on a board, with bidirectional sync.

## Overview

PraxisNote bridges the gap between free-form note-taking and structured task management. Write naturally in rich text notes, and any checkbox you create automatically becomes a trackable task on your board.

### Key Features

- **Note-First Workflow** - Capture thoughts in rich text notes
- **Automatic Task Extraction** - Checkboxes in notes become tasks on your board
- **Bidirectional Sync** - Complete a task on the board, and the checkbox in your note is checked (and vice versa)
- **Three-State Tasks** - Todo → In Progress → Done lifecycle with timestamps
- **Label Organization** - Tag notes and tasks; tasks inherit labels from their source note

## Tech Stack

- .NET 10, C# (nullable reference types, implicit usings)
- xUnit for testing
- DDD (Domain-Driven Design)

## Project Structure

```
src/
├── PraxisNote.Domain/           # Pure domain model
│   ├── Aggregates/              # User, Label, TaskItem (Note coming soon)
│   ├── ValueObjects/            # Email, ExternalIdentity, TaskStatus, DueDate, CheckboxRef
│   ├── Common/                  # Entity, AggregateRoot, ValueObject
│   └── Events/                  # IDomainEvent, DomainEventBase
└── PraxisNote.Domain.Tests/     # Unit tests
```

## Getting Started

```bash
dotnet build src/PraxisNote.slnx
dotnet test src/PraxisNote.slnx
```
