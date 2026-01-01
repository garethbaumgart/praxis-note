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
- Angular 21 with PrimeNG and Tailwind CSS
- EF Core with SQLite (PostgreSQL planned for production)
- xUnit for testing
- DDD (Domain-Driven Design)

## Project Structure

```
src/
├── PraxisNote.Domain/           # Pure domain model
│   ├── Aggregates/              # User, Label, TaskItem, Note
│   ├── ValueObjects/            # Email, ExternalIdentity, TaskStatus, DueDate, CheckboxRef, Checkbox
│   ├── Common/                  # Entity, AggregateRoot, ValueObject
│   └── Events/                  # IDomainEvent, DomainEventBase
├── PraxisNote.Domain.Tests/     # Unit tests
├── PraxisNote.Infrastructure/   # Application layer + persistence
│   ├── Application/             # Use cases, repository interfaces
│   └── Persistence/             # EF Core DbContext, configurations, repositories
└── PraxisNote.Web/              # ASP.NET Core backend + Angular frontend
    ├── Endpoints/               # Minimal API endpoints
    └── ClientApp/               # Angular 21 SPA
```

## Getting Started

### Prerequisites

- .NET 10 SDK
- Node.js 20+

### Build and Test

```bash
dotnet build src/PraxisNote.slnx
dotnet test src/PraxisNote.slnx
```

### Running Locally

1. Build the Angular frontend:
   ```bash
   cd src/PraxisNote.Web/ClientApp
   npm install
   npm run build
   ```

2. Configure Google OAuth (see below)

3. Run the application:
   ```bash
   cd src/PraxisNote.Web
   dotnet run
   ```

## Authentication Setup

PraxisNote uses Google OAuth for authentication. Secrets are stored using .NET User Secrets to keep them out of source control.

### 1. Create Google OAuth Credentials

1. Go to [Google Cloud Console](https://console.cloud.google.com/)
2. Create a new project (or select an existing one)
3. Navigate to **APIs & Services > Credentials**
4. Click **Create Credentials > OAuth client ID**
5. Select **Web application**
6. Add authorized redirect URI: `http://localhost:5002/signin-google`
   - Note: `/signin-google` is the default callback path used by ASP.NET Core's Google authentication middleware
7. Copy the Client ID and Client Secret

### 2. Configure User Secrets

User Secrets stores credentials outside your project directory at `~/.microsoft/usersecrets/`.

```bash
cd src/PraxisNote.Web

# Set your Google OAuth credentials
dotnet user-secrets set "Authentication:Google:ClientId" "YOUR_CLIENT_ID.apps.googleusercontent.com"
dotnet user-secrets set "Authentication:Google:ClientSecret" "YOUR_CLIENT_SECRET"

# Verify secrets are stored
dotnet user-secrets list
```

### 3. Managing Secrets

```bash
# View all secrets
dotnet user-secrets list

# Remove a specific secret
dotnet user-secrets remove "Authentication:Google:ClientId"

# Clear all secrets
dotnet user-secrets clear
```

## CI

Unit tests run automatically via GitHub Actions on every push to `main` and on pull requests. PRs require passing tests before merge.
