# PraxisNote

A note-first task management system designed for busy professionals.

## Overview

PraxisNote bridges the gap between free-form note-taking and structured task management. Write naturally in rich text notes, and any checkbox you create automatically becomes a trackable task on your board - with full bidirectional sync.

### Key Features

- **Note-First Workflow** - Capture thoughts in rich text notes using a TipTap editor
- **Automatic Task Extraction** - Checkboxes in notes become tasks on your board
- **Bidirectional Sync** - Complete a task on the board, and the checkbox in your note is checked (and vice versa)
- **Three-State Tasks** - Todo → In Progress → Done lifecycle with timestamps
- **Label Organization** - Tag notes and tasks with shared labels; tasks inherit labels from their source note
- **Google OAuth** - Simple authentication with your Google account

## Domain Model

| Aggregate | Purpose |
|-----------|---------|
| **User** | Authentication & ownership scoping |
| **Note** | Rich text content with embedded checkboxes |
| **Task** | Actionable items with status lifecycle |
| **Label** | Cross-cutting organization tags |

## Project Structure

```
src/
├── PraxisNote.slnx              # .NET 10 solution
└── PraxisNote.Domain/           # Pure domain model (no dependencies)
```

## Tech Stack

- .NET 10
- C# with nullable reference types enabled

## Getting Started

```bash
cd src
dotnet build PraxisNote.slnx
```

## License

[MIT](LICENSE)
