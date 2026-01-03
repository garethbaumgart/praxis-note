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
- EF Core with PostgreSQL
- xUnit for testing
- DDD (Domain-Driven Design)

## Project Structure

```
src/
├── PraxisNote.Domain/           # Pure domain model (no dependencies)
│   ├── Aggregates/              # User, Label, TaskItem, Note
│   ├── ValueObjects/            # Email, TaskStatus, DueDate, etc.
│   ├── Common/                  # Entity, AggregateRoot, ValueObject
│   └── Events/                  # Domain events
├── PraxisNote.Application/      # Application layer (use cases)
│   ├── Common/                  # IUnitOfWork, shared interfaces
│   └── Features/                # Use cases by feature
├── PraxisNote.Infrastructure/   # Persistence & external services
│   ├── Persistence/             # EF Core DbContext, repositories
│   └── Migrations/              # EF Core migrations
└── PraxisNote.Web/              # ASP.NET Core + Angular frontend
    ├── Endpoints/               # Minimal API endpoints
    └── ClientApp/               # Angular 21 SPA

tests/
├── PraxisNote.Domain.Tests/     # Domain unit tests (xUnit)
└── PraxisNote.E2E.Tests/        # Playwright E2E tests
    ├── smoke-tests/             # Auth and health tests
    └── helpers/                 # DB reset utilities
```

## Getting Started

### Prerequisites

- .NET 10 SDK
- Node.js 22+
- Docker (for PostgreSQL)

### Build and Test

```bash
dotnet build src/PraxisNote.slnx
dotnet test tests/PraxisNote.Domain.Tests
```

### Running Locally

1. Start PostgreSQL:
   ```bash
   docker compose up -d
   ```

2. Configure secrets (see Database and Authentication sections below)

3. Build the Angular frontend:
   ```bash
   cd src/PraxisNote.Web/ClientApp
   npm install
   npm run build
   ```

4. Run the application:
   ```bash
   cd src/PraxisNote.Web
   dotnet run
   ```

## Database Setup

PraxisNote uses PostgreSQL. The connection string must be configured via user secrets (not stored in source control).

### Configure Connection String

```bash
cd src/PraxisNote.Web

# Set the PostgreSQL connection string
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=praxisnote;Username=praxisnote;Password=devpassword"
```

The `docker-compose.yml` creates a PostgreSQL container with these default credentials. Migrations run automatically on startup in Development mode.

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

## E2E Tests

End-to-end tests use Playwright with a separate PostgreSQL instance.

```bash
# Run E2E tests (starts its own database container)
cd tests/PraxisNote.E2E.Tests
npm install
npm test
```

## CI

GitHub Actions runs automatically on every push to `main` and on pull requests:
- **Unit tests** - Domain model tests
- **E2E tests** - Playwright smoke tests with PostgreSQL

PRs require passing tests before merge.
