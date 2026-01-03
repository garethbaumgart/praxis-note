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

## Development Tools

### Mock Authentication

For local development and E2E testing, PraxisNote includes a mock authentication system that bypasses Google OAuth.

**How it works:**
- A dev toolbar appears at the bottom of the screen in Development/E2E environments
- Click "Enable Mock Auth" to activate, then enter any email/name and click "Login"
- The Angular app sends an `X-Mock-User` header with API requests
- The backend `MockAuthenticationHandler` processes this header and creates/authenticates users
- Mock auth is completely disabled in Production builds

**When to use:**
- Local development without Google OAuth credentials
- E2E tests that need authenticated API access
- Testing user-specific features quickly

## E2E Tests

End-to-end tests use Playwright with a separate PostgreSQL instance (port 5433).

```bash
# Run E2E tests (starts its own database container)
cd tests/PraxisNote.E2E.Tests
npm install
npm test
```

E2E tests use the mock authentication system via the `X-Mock-User` header to authenticate API requests without requiring Google OAuth.

## CI/CD Pipeline

GitHub Actions runs automatically on every push to `main` and on pull requests:

| Workflow | Trigger | What it does |
|----------|---------|--------------|
| **Unit Tests** | Push/PR | Runs `dotnet test` on Domain tests (185+ tests) |
| **E2E Tests** | Push/PR | Spins up PostgreSQL, runs migrations, starts the app, runs Playwright tests |
| **Copilot Review** | PR only | AI-powered code review with suggestions |

### E2E Test Pipeline Details

The E2E workflow:
1. Starts a PostgreSQL service container (port 5432)
2. Applies EF Core migrations to create the schema
3. Builds and starts the .NET application in E2E mode
4. Runs 8 Playwright smoke tests (auth, health, API access)
5. Uploads HTML test reports as artifacts on failure

PRs require all tests to pass before merge.
