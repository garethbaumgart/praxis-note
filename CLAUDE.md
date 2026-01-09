# CLAUDE.md - Project Preferences

## Backend (.NET 10)

- **Architecture**: Vertical Slice Architecture (feature folders, not layer folders)
- **Pattern**: CQRS (Command Query Responsibility Segregation)
- **DDD Principles**: Repository interfaces live in Domain layer
- **Clean Architecture Layers**: Domain → Application → Infrastructure → Web
- **Style**: Primary constructors, Minimal APIs
- **Database**: Entity Framework Core with SQLite (dev) / PostgreSQL (prod)

## Frontend (Angular v21)

- **Components**: Standalone components only
- **State Management**: Signals (zoneless change detection)
- **UI Framework**: PrimeNG components + Tailwind CSS only
- **NO custom CSS** - use Tailwind utilities and PrimeNG theming exclusively
- **DI**: Use `inject()` function, not constructor injection

### Signal Best Practices

```typescript
// Form inputs - use native event binding, NOT ngModel
[value]="mySignal()"
(input)="mySignal.set($any($event.target).value)"

// PrimeNG two-way binding
[visible]="showDialog()"
(visibleChange)="showDialog.set($event)"

// Signal patterns
signal()           // mutable local state
computed()         // derived/calculated state
input()            // component inputs
input.required()   // required component inputs
output()           // component outputs
.asReadonly()      // expose read-only signals publicly
```

### Signal Members

Always mark signal members as `readonly`:

```typescript
readonly task = input.required<Task>();
readonly onStatusChange = output<'Todo' | 'InProgress' | 'Done'>();
readonly editing = signal(false);
```

## UX/UI Guidelines

- **Responsive**: All UI must work on mobile devices AND desktop
- **Design**: Clean, modern aesthetic
- **Consistency**: Maintain consistent patterns across the entire application
- **Accessibility**: Include proper aria-labels and semantic HTML

### UI Design Resources

When designing UI components, reference these official documentation sites:

- **PrimeNG v21**: https://primeng.org - Component API, examples, theming
- **PrimeNG Showcase**: https://primeng.org/installation - Live demos of every component
- **Tailwind CSS v4**: https://tailwindcss.com/docs - Utility class reference
- **Tailwind Cheat Sheet**: https://tailwindcomponents.com/cheatsheet - Quick class lookup

## Project Structure

```
Backend
├── Domain/           # Aggregates, entities, value objects, repository interfaces
├── Application/      # Use cases, DTOs, commands, queries (vertical slices)
├── Infrastructure/   # EF Core, repository implementations, external services
└── Web/              # Minimal API endpoints, authentication

Frontend (ClientApp)
└── src/app/
    ├── auth/         # Authentication service and models
    ├── home/         # Home page
    ├── tasks/        # Task feature (service, page, components)
    └── shared/       # Shared components (if needed)
```

## Naming Conventions

- **Pages**: `*.page.ts` (e.g., `tasks.page.ts`)
- **Components**: `*.component.ts` (e.g., `task-card.component.ts`)
- **Services**: `*.service.ts` (e.g., `task.service.ts`)
- **Models**: `*.model.ts` (e.g., `task.model.ts`)
- **Clear naming is critical**: Function and variable names must be descriptive and unambiguous

## Pre-PR Checklist

Before creating or updating a PR, always run:

1. **Unit tests**: `dotnet test` (all 185+ tests must pass)
2. **E2E tests**: `cd tests/PraxisNote.E2E.Tests && npm test` (all tests must pass)

Never put up a PR with failing tests.

## Post-PR Workflow

After creating a PR, follow this workflow:

1. **Self code review**: Review the PR diff and add comments to the PR in GitHub for any issues found
2. **Wait for CI**: Monitor GitHub Actions for completion
3. **Check for warnings**: Review action logs AND annotations for any warnings (not just failures)
   - Use `gh api repos/{owner}/{repo}/check-runs/{job_id}/annotations` to fetch annotations
   - The "Annotations" section in GitHub UI also shows these
   - Common warnings: deprecation notices, bundle size budgets, artifact upload failures, EF Core model validation
   - **ALL warnings must be addressed** - either fix the issue or update the workflow if it's a false positive
4. **Wait for Copilot**: Allow Copilot to complete its review
5. **Address all comments**: Fix any issues raised by Copilot or other reviewers
6. **Verify CI passes**: Ensure all checks pass after fixes (no warnings in annotations)

Only request merge approval once all comments are addressed, CI is green, and there are no warnings in annotations.

## Technical Debt / TODOs

When switching from SQLite to PostgreSQL:
- [ ] `TaskRepository.GetByUserIdAsync` - Move `OrderByDescending(t => t.CreatedAt)` back to the EF query (currently sorting in memory due to SQLite DateTimeOffset limitation)
