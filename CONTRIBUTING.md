# Contributing to PraxisNote

Welcome! This is a passion project built by a human and his AI assistant, and we're glad you're here. This guide explains how we work, how AI agents contribute, and how you can join in.

## Table of Contents

- [How We Work](#how-we-work)
- [Dev Workflow](#dev-workflow)
- [Claude Code Skills](#claude-code-skills)
- [AI-Assisted Development](#ai-assisted-development)
- [Architecture](#architecture)
- [Testing Philosophy](#testing-philosophy)
- [CI/CD Pipeline](#cicd-pipeline)
- [Production Secrets](#production-secrets)
- [Documentation](#documentation)
- [Onboarding](#onboarding)

## How We Work

This project runs on a **refine → execute → PR pipeline** powered by Claude Code. Here's the flow:

1. **Refine** — AI reads the issue, explores the codebase, creates UX mockups if needed, writes a step-by-step plan, updates the issue
2. **Execute** — AI implements the plan, writes tests, commits code
3. **PR** — AI creates the PR, runs tests, monitors CI, addresses review comments, merges when green

It's not magic — it's guardrails, iteration, and a lot of "no, try again." But it works.

## Dev Workflow

### Prerequisites

- Docker (for local dev stack and E2E tests)
- .NET 10 SDK (optional, if you want to run dotnet commands outside Docker)
- Node.js (optional, for running Angular CLI commands outside Docker)

### Local Development

Start the full stack with hot reload:

```bash
docker compose --profile dev-stack up
```

This spins up:
- **PostgreSQL** on port 5432
- **.NET API** on port 5002 (with hot reload)
- **Angular** on port 4200 (with hot reload)

Open http://localhost:4200 to develop. Use the mock auth toolbar at the bottom to log in — no Google OAuth setup required.

To stop:
```bash
docker compose --profile dev-stack down
```

### Optional: Cloud Run Log Access

To enable the `/postmortem` skill to query production logs for bug investigations, authenticate with `gcloud`:

1. **Install the gcloud CLI** (if not already installed): https://cloud.google.com/sdk/docs/install

2. **Authenticate**:
```bash
gcloud auth login
```

3. **Set the project**:
```bash
gcloud config set project praxisnote-prod
```

4. **Verify Cloud Run access**:
```bash
gcloud run services describe praxisnote --region australia-southeast1
```

5. **Verify logging access**:
```bash
gcloud logging read "resource.type=cloud_run_revision" --limit 10
```

Without `gcloud` authentication, the `/postmortem` skill can still perform code analysis but won't be able to query production logs or verify deployment status.

### Feature Branches

**ALWAYS** create a new feature branch when working on a new issue. Never commit directly to `main`.

```bash
# Create and switch to a new feature branch
git checkout -b feat/short-description

# Examples:
git checkout -b feat/editor-toolbar-enhancements
git checkout -b fix/login-redirect-bug
git checkout -b chore/update-dependencies
```

**Branch naming conventions:**
- `feat/` — New features
- `fix/` — Bug fixes
- `chore/` — Maintenance tasks, refactoring, dependencies
- `docs/` — Documentation only changes

### Atomic Commits

Keep commits atomic: commit only the files you touched and list each path explicitly.

**For tracked (modified) files:**
```bash
git commit -m "<scoped message>" -- path/to/file1 path/to/file2
```

**For brand-new (untracked) files:**
```bash
git restore --staged :/ && git add "path/to/file1" "path/to/file2" && git commit -m "<scoped message>" -- path/to/file1 path/to/file2
```

**Rules:**
- **NEVER** use `git add .` or `git add -A` — these can stage unrelated files
- Always list files explicitly by path
- Each commit should contain only related changes (one logical unit of work)

## Claude Code Skills

Custom slash commands in `.claude/skills/` that automate development workflows. These are the AI's power tools.

| Skill | Usage | Purpose |
|-------|-------|---------|
| `/refine` | `/refine 336` | Reads a GitHub issue, explores the codebase, creates UX mockups if needed, writes a step-by-step implementation plan with acceptance criteria and verification steps, and updates the issue body |
| `/execute-issues` | `/execute-issues 340 341 342` | Scans issues for readiness (must have implementation plan), then sequentially implements each one: refine if needed, create feature branch, implement, create PR, broadcast if user-facing, merge when green |
| `/pr` | `/pr` | Creates a pull request with tests, self-review, browser validation, AI review monitoring, and merge when CI passes |
| `/broadcast` | `/broadcast` | Generates an EF Core migration to add a "What's New" notification for user-facing changes (shown in-app on next login) |
| `/postmortem` | `/postmortem` | Conducts a structured bug investigation: verifies deployment status, queries production logs via gcloud, traces code paths, identifies root causes, and produces bug fix issues with guardrail updates |

These skills are the reason this project can move fast without breaking things (most of the time).

## AI-Assisted Development

This project is built with Claude Code — an AI agent that writes code, runs tests, and creates PRs. It's not a gimmick; it's the primary contributor.

**How it works:**
1. **Guardrails** — CLAUDE.md contains strict rules about banned patterns, theming, signals, accessibility, and architecture
2. **Iteration** — When the AI breaks a rule or writes bad code, we update the guardrails and try again
3. **Verification** — Every issue has acceptance criteria and verification steps to ensure the AI did what was asked
4. **Human oversight** — The human reviews PRs, merges when green, and updates guardrails when the AI goes off the rails

**Why this matters:**
- Features ship faster (the AI doesn't sleep)
- Code quality is enforced via guardrails, not pull request comments
- The codebase stays consistent because the AI follows the same rules every time

**The catch:**
- Guardrails need constant refinement
- The AI sometimes misinterprets instructions (hence the verification steps)
- It's not AGI — it's a fancy autocomplete with delusions of grandeur

## Architecture

PraxisNote follows **Vertical Slice Architecture** with **Domain-Driven Design** principles.

**Backend (.NET 10):**
- **Domain Layer** — Aggregates, entities, value objects, repository interfaces (pure business logic, no dependencies)
- **Application Layer** — CQRS commands/queries, DTOs, feature folders (vertical slices)
- **Infrastructure Layer** — EF Core, repository implementations, external services
- **Web Layer** — Minimal API endpoints, authentication

**Frontend (Angular 21):**
- **Standalone components** (no NgModules)
- **Signals for state** (zoneless change detection, no Zone.js)
- **Dependency injection via `inject()`** (not constructor injection)
- **PrimeNG components** for UI (buttons, dialogs, tables, etc.)
- **Tailwind CSS** for styling

**Reference files** (see `CLAUDE.md` for full table):
- Domain aggregate: `src/PraxisNote.Domain/Aggregates/Tasks/TaskItem.cs`
- CQRS command: `src/PraxisNote.Application/Features/Tasks/CreateTask.cs`
- Repository: `src/PraxisNote.Infrastructure/Persistence/Repositories/TaskRepository.cs`
- API endpoints: `src/PraxisNote.Web/Endpoints/TaskEndpoints.cs`
- Feature service: `src/PraxisNote.Web/ClientApp/src/app/tasks/task.service.ts`
- List page: `src/PraxisNote.Web/ClientApp/src/app/tasks/tasks.page.ts`

For detailed conventions, see [CLAUDE.md](CLAUDE.md).

## Testing Philosophy

### Well-Named Tests Are Essential

Test names should clearly describe what is being tested and the expected outcome. A developer should understand what broke just by reading the test name.

**Format:** `MethodName_Scenario_ExpectedBehavior` or `Should_ExpectedBehavior_When_Condition`

```csharp
// Good - Clear and descriptive
Create_WithValidContent_ReturnsComment()
Create_WithNullContent_ThrowsArgumentException()
Complete_WhenAlreadyDone_PreservesOriginalCompletedAt()

// Bad - Vague or meaningless
TestCreate()
Test1()
WorksCorrectly()
```

### Domain Unit Tests

**Target: ~100% coverage** for the Domain layer. Domain contains core business logic, aggregates, entities, and value objects. These are pure, deterministic, and easy to test.

**DO test:**
- All factory methods (Create, etc.)
- All state-changing methods
- Validation rules (null, empty, whitespace, invalid values)
- Edge cases and boundary conditions
- Immutability of value objects

**Patterns to follow** (see `tests/PraxisNote.Domain.Tests/Aggregates/TaskItemTests.cs`):
- AAA pattern (Arrange-Act-Assert)
- Theory tests with InlineData for validation scenarios
- Regions to organize test methods by functionality

### Frontend Unit Tests

Frontend unit tests run via `ng test` (Vitest + jsdom). Tests live alongside source files as `*.spec.ts`.

```bash
cd src/PraxisNote.Web/ClientApp && npx ng test --watch=false
```

**Testing patterns:**
- **Pure functions** (services, utilities): Import and test directly. No TestBed needed.
- **TipTap editor actions**: Instantiate a real `Editor` with the production `tiptapExtensions` array, execute commands, and assert against `getJSON()`, `getHTML()`, or `isActive()`.

**TipTap editor test template** (see `src/PraxisNote.Web/ClientApp/src/app/notes/editor/tiptap-editor.spec.ts`):

```typescript
import { Editor } from '@tiptap/core';
import { tiptapExtensions } from './tiptap-extensions';

let editor: Editor;

beforeEach(() => {
  editor = new Editor({
    element: document.createElement('div'),
    extensions: [...tiptapExtensions, Placeholder.configure({ placeholder: 'Test...' })],
  });
});

afterEach(() => editor.destroy());

it('toggleBold applies bold mark', () => {
  editor.commands.setContent({ type: 'doc', content: [{ type: 'paragraph', content: [{ type: 'text', text: 'hello' }] }] });
  editor.commands.selectAll();
  editor.chain().focus().toggleBold().run();
  expect(editor.isActive('bold')).toBe(true);
});
```

### E2E Tests

E2E tests are expensive to write, maintain, and run. Only add E2E tests for **critical flows that would break the business if they fail**.

**Current E2E coverage** (use `tests/PraxisNote.E2E.Tests/tests/tasks.spec.ts` as the template):
- `health.spec.ts` — System health/startup verification
- `auth.spec.ts` — Authentication and access control
- `tasks.spec.ts` — Core task workflow (create, delete, kanban state transitions, priority)
- `due-date.spec.ts` — Due date display and styling
- `icon-sizing.spec.ts` — Icon rendering quality

**DO write E2E tests for:**
- Authentication/authorization flows
- Core business operations (task CRUD, workflow state changes)
- Critical user journeys that span API + UI

**DO NOT write E2E tests for:**
- Individual UI components or styling changes
- New buttons, dialogs, or form fields (unit test these instead)
- Features already covered by existing workflow tests
- Non-critical enhancements (search, sorting, keyboard shortcuts)

When adding a new feature, ask: "If this breaks, does the app become unusable?" If no, skip the E2E test.

### Flaky Tests Are Not Acceptable

**Zero tolerance for flaky tests.** A flaky test is one that sometimes passes and sometimes fails without code changes.

**If a test is flaky, fix it immediately:**
1. Identify the root cause (timing, race conditions, state leakage)
2. Fix the underlying issue, don't just add retries
3. Run the test multiple times locally to verify reliability

**Common causes of flaky E2E tests:**
- Using `page.route()` for auth headers (use `page.setExtraHTTPHeaders()` instead)
- Using `waitForLoadState('networkidle')` when SSE connections are open
- Shared test data without proper cleanup
- Hardcoded timeouts instead of proper waits

## CI/CD Pipeline

GitHub Actions runs on every push to `main` and on pull requests:

- **Unit Tests** — Runs domain tests with xUnit
- **E2E Tests** — Spins up PostgreSQL, runs Playwright tests in headless mode
- **AI Reviews** — CodeRabbit and Copilot provide code review on PRs
- **Deploy** — Auto-deploys to Google Cloud Run (Sydney region) with Neon PostgreSQL

Dependency updates are automated via [Renovate](https://renovatebot.com/).

**Branch protection:**
- Direct commits to `main` are blocked
- All PRs require passing CI checks before merge
- No manual approval required (AI agents merge when green)

## Production Secrets

Secrets are stored in **Google Cloud Secret Manager** and injected into Cloud Run at deploy time.

### Anthropic API Key (AI Meeting Analysis)

1. Create a secret in GCP Secret Manager named `ANTHROPIC_API_KEY`
2. Add your Anthropic API key (`sk-ant-...`) as the secret value
3. Grant the Cloud Run service account access to the secret

The deploy workflow automatically maps this to `MeetingAnalysis__ApiKey` in Cloud Run.

### Deepgram API Key (Live Transcription)

1. Create a secret in GCP Secret Manager named `DEEPGRAM_API_KEY`
2. Add your Deepgram API key as the secret value
3. Grant the Cloud Run service account access to the secret

The deploy workflow automatically maps this to `Deepgram__ApiKey` in Cloud Run.

### Google OAuth (Calendar Sync)

1. Create secrets in GCP Secret Manager: `GOOGLE_CLIENT_ID` and `GOOGLE_CLIENT_SECRET`
2. Add your Google OAuth credentials as the secret values
3. Grant the Cloud Run service account access to the secrets
4. Ensure the production redirect URI (`https://your-domain/api/calendar/callback/google`) is added to the Google OAuth client's authorized redirect URIs

The deploy workflow automatically maps these to `Authentication__Google__ClientId` and `Authentication__Google__ClientSecret` in Cloud Run.

### Jira OAuth (Jira Integration)

1. Create secrets in GCP Secret Manager: `JIRA_CLIENT_ID` and `JIRA_CLIENT_SECRET`
2. Add your Jira OAuth credentials as the secret values
3. Grant the Cloud Run service account access to the secrets
4. Ensure the production redirect URI is added to the Jira OAuth client's authorized redirect URIs

## Documentation

User-facing documentation is built with [Starlight](https://starlight.astro.build/) and lives in `docs/`. It auto-deploys to Vercel on push to `main`.

**Structure:**
- Content pages: `docs/src/content/docs/*.mdx`
- Custom theme: `docs/src/styles/custom.css` (Nord palette)
- Config: `docs/astro.config.mjs`

**When to update docs:**
- New feature → add/update the relevant `docs/src/content/docs/*.mdx` page
- Changed behavior → update affected docs sections
- New keyboard shortcut → update `keyboard-shortcuts.mdx`

**When NOT to update docs:**
- Internal refactoring, backend performance, test-only changes, CI/CD, developer docs (`README.md`, `CLAUDE.md`, `CONTRIBUTING.md`)

**Contextual help links:**
Use the shared `<app-help-link path="..." />` component. Add links to:
- Feature page empty states
- Complex form fields (priority, due dates, tags)
- Settings panels, error states with recovery steps, onboarding flows

## Onboarding

### For Humans

1. **Read this file** (you're already here, nice work)
2. **Read [CLAUDE.md](CLAUDE.md)** to understand the coding conventions and guardrails
3. **Start the dev stack** with `docker compose --profile dev-stack up`
4. **Pick an issue** from the GitHub issue tracker (look for `good first issue` labels)
5. **Create a feature branch** and start coding
6. **Run tests** before creating a PR
7. **Create a PR** and wait for CI to pass
8. **Merge** when green

### For AI Agents

1. **Read [CLAUDE.md](CLAUDE.md)** — This is your constitution. Follow it.
2. **Use the skills** — `/refine`, `/execute-issues`, `/pr`, `/broadcast`, `/postmortem`
3. **Write tests** — Unit tests for domain logic, E2E tests for critical flows
4. **Verify your work** — Every issue has acceptance criteria and verification steps
5. **Don't break the build** — If CI fails, fix it before merging
6. **Update guardrails** — If you make a mistake, help the human update CLAUDE.md so you don't make it again

Welcome aboard. Let's ship.
