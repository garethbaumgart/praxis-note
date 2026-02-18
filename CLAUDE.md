# CLAUDE.md - Project Preferences

> **Length guideline:** This file should stay under ~700 lines. If it grows beyond that, move reference tables (icon sizes, dialog sizes, detailed UX patterns) to linked files like `UX-PATTERNS.md`.

## Critical Rules (ENFORCED)

These rules cause the most common sub-agent mistakes. They are listed first for maximum visibility.

### Banned Patterns

```html
<!-- BANNED: Old structural directives -->
*ngIf, *ngFor, *ngSwitch, [ngClass]

<!-- USE INSTEAD: New control flow -->
@if (loading()) { <spinner /> }
@for (item of items(); track item.id) { ... }
@switch (status()) { @case ('done') { ... } }
[class.active]="isActive()"
```

### Auth Interceptor — `fetch()` vs `HttpClient`

The auth interceptor (`auth.interceptor.ts`) catches **any 401 response** from `HttpClient` calls and forces a full page reload (`window.location.href = '/'`). This is by design for session expiry — but it means **non-critical API calls that use `HttpClient` can crash the entire page if auth fails unexpectedly.**

```typescript
// WRONG: Non-critical call uses HttpClient — 401 causes page refresh
this.http.post('/api/tags/123/starters', {}).subscribe({ ... });

// CORRECT: Non-critical call uses fetch() — 401 is handled gracefully
const response = await fetch('/api/tags/123/starters', {
  method: 'POST',
  headers,
  credentials: 'include',
  body: '{}',
});
```

**Rule:** Use `fetch()` (not `HttpClient`) for API calls where a failure should NOT trigger a page-level auth redirect. This includes:
- AI chat starters, suggestions, and other "nice to have" data
- Background/fire-and-forget calls
- SSE streaming endpoints (already required for technical reasons)

Use `HttpClient` for calls where a 401 genuinely means "user needs to re-authenticate" (core CRUD operations like loading tasks, notes, meetings).

When using `fetch()`, you must manually add auth headers (mock auth in dev mode, profile ID). Follow the pattern in `tag-ai-chat.service.ts:send()`.

### Zoneless Anti-Patterns

Angular 21 is zoneless by default. These patterns **will NOT work**:

```typescript
// WRONG: Plain properties don't trigger change detection
items: Item[] = [];
this.items.push(newItem);  // View won't update!

// CORRECT: Use signals instead
readonly items = signal<Item[]>([]);
this.items.update(arr => [...arr, newItem]);  // View updates!

// WRONG: Don't mutate signal values directly
this.items().push(newItem);  // Wrong!

// CORRECT: Always create new references
this.items.update(arr => [...arr, newItem]);  // Correct!
```

### Theming Rules

- **ALWAYS use semantic tokens** from `styles.css` (e.g., `bg-surface`, `text-foreground`, `bg-todo`)
- **NEVER use hardcoded Tailwind colors** (e.g., `bg-gray-100`, `text-violet-600`)
- **NEVER use `dark:` prefix** — our CSS variable system handles dark mode automatically
- **Respect token semantics**: Use `-foreground` tokens for text, background tokens for backgrounds
- **New colors**: Add semantic tokens to `styles.css` in both `:root` and `[data-theme="dark"]` blocks within `@layer theme`, then map in `@theme inline`
- **Reference**: See `src/PraxisNote.Web/ClientApp/THEMING.md` for the full token reference

### Component CSS Colors

When using colors in component `styles: []` (not Tailwind classes), use CSS variables:

```css
/* CORRECT: Use semantic CSS variables */
background: var(--color-surface-subtle);
color: var(--color-foreground);
border-color: var(--color-border);

/* WRONG: Hardcoded colors */
background: #f5f5f5;

/* WRONG: Using foreground token as background */
background: var(--color-todo-foreground);  /* This is a text color! */
```

### Boy Scout Rule (Scoped)

When modifying code, fix banned or discouraged patterns in the lines you're already changing — but do NOT expand scope.

**DO fix (within lines you're already editing):**
- `*ngIf` / `*ngFor` / `*ngSwitch` → `@if` / `@for` / `@switch` control flow
- `[ngClass]` → `[class.x]="expr"` bindings
- Hardcoded Tailwind colors (`bg-gray-100`) → semantic tokens (`bg-surface`)
- `dark:` prefixed classes → remove (CSS variable system handles it)
- Constructor injection → `inject()` DI
- Hardcoded CSS colors (`#f5f5f5`) → CSS variables (`var(--color-surface-subtle)`)

**DO NOT:**
- Refactor functions or methods you aren't otherwise changing
- Add types, comments, or docstrings to unchanged code
- Rename files or move code to different locations
- Create separate commits for Boy Scout cleanup — fold fixes into the feature commit
- Touch lines outside the scope of the current task

## Pattern Examples (Real Files)

When implementing a common pattern, use these real files as references instead of guessing.

### Backend Patterns

| Pattern | Exemplar File |
|---------|--------------|
| Domain aggregate with factory method | `src/PraxisNote.Domain/Aggregates/Tasks/TaskItem.cs` |
| Repository interface (Domain layer) | `src/PraxisNote.Domain/Aggregates/Tasks/ITaskRepository.cs` |
| Repository implementation (Infrastructure) | `src/PraxisNote.Infrastructure/Persistence/Repositories/TaskRepository.cs` |
| CQRS Command handler | `src/PraxisNote.Application/Features/Tasks/CreateTask.cs` |
| CQRS Query handler | `src/PraxisNote.Application/Features/Tasks/GetUserTasks.cs` |
| DTO / Response model | `src/PraxisNote.Application/Features/Tasks/TaskDto.cs` |
| Minimal API endpoints | `src/PraxisNote.Web/Endpoints/TaskEndpoints.cs` |
| Domain unit tests | `tests/PraxisNote.Domain.Tests/Aggregates/TaskItemTests.cs` |
| Application layer tests | `tests/PraxisNote.Application.Tests/Tags/MergeTagsTests.cs` |

### Frontend Patterns

| Pattern | Exemplar File |
|---------|--------------|
| Feature service (signals, CRUD, API calls) | `src/PraxisNote.Web/ClientApp/src/app/tasks/task.service.ts` |
| List page with loading/error/empty states | `src/PraxisNote.Web/ClientApp/src/app/tasks/tasks.page.ts` |
| Editor page (detail view) | `src/PraxisNote.Web/ClientApp/src/app/notes/note-editor.page.ts` |
| Shared reusable component | `src/PraxisNote.Web/ClientApp/src/app/shared/components/error-state.component.ts` |
| Feature model/interface | `src/PraxisNote.Web/ClientApp/src/app/tasks/task.model.ts` |
| Dialog with inline footer | `src/PraxisNote.Web/ClientApp/src/app/meetings/meetings.page.ts` |
| E2E test (Playwright) | `tests/PraxisNote.E2E.Tests/tests/tasks.spec.ts` |
| Frontend unit test (TipTap) | `src/PraxisNote.Web/ClientApp/src/app/notes/editor/tiptap-editor.spec.ts` |

### Shared Components (Check Before Creating New Ones)

| Component | File | Use For |
|-----------|------|---------|
| `ErrorStateComponent` | `src/app/shared/components/error-state.component.ts` | Page/section error display |
| `DeleteConfirmButtonComponent` | `src/app/shared/components/delete-confirm-button.component.ts` | Inline delete with countdown |
| `PageContentComponent` | `src/app/shared/components/page-content.component.ts` | Page layout wrapper |
| `ToastService` | `src/app/shared/services/toast.service.ts` | Mutation success/error feedback |
| `date-utils.ts` | `src/app/shared/date-utils.ts` | Date formatting utilities |
| `HelpLinkComponent` | `src/app/shared/components/help-link.component.ts` | Contextual "Learn more" links to docs |

## Backend (.NET 10)

- **Architecture**: Vertical Slice Architecture (feature folders, not layer folders)
- **Pattern**: CQRS (Command Query Responsibility Segregation)
- **DDD Principles**: Repository interfaces live in Domain layer
- **Clean Architecture Layers**: Domain -> Application -> Infrastructure -> Web
- **Style**: Primary constructors, Minimal APIs
- **Database**: Entity Framework Core with PostgreSQL (all environments)

## Frontend (Angular v21)

### Core Principles

- **Standalone components only** - No NgModules
- **Signals for state** - Zoneless change detection (Zone.js not included)
- **DI via `inject()`** - Not constructor injection

### Component Decision Hierarchy

When building UI, follow this order strictly:

1. **PrimeNG Component** - ALWAYS check https://primeng.org first (buttons, inputs, dialogs, dropdowns, menus, tooltips, tables, cards, date pickers, etc.)
2. **Tailwind Utilities** - Style with utility classes only
3. **Custom CSS (LAST RESORT)** - Only for library integration (CDK), animations, theming, or PrimeNG overrides

**Before writing custom CSS, ask:** "Can this be achieved with Tailwind utilities or PrimeNG props?"

### Signal Patterns

```typescript
// Always mark signal members as readonly
readonly items = signal<Item[]>([]);
readonly count = computed(() => this.items().length);
readonly task = input.required<Task>();
readonly onChange = output<void>();
```

```html
<!-- Form inputs - use native event binding, NOT ngModel -->
<input [value]="mySignal()" (input)="mySignal.set($any($event.target).value)" />

<!-- PrimeNG two-way binding -->
<p-dialog [visible]="showDialog()" (visibleChange)="showDialog.set($event)" />
```

### Signal Forms (Angular 21 - Use for New Forms)

For new form components, use Signal Forms instead of Reactive Forms:

```typescript
// Signal Forms (preferred for new forms)
filter = signal({ from: '', to: '' });
filterForm = form(this.filter, (path) => {
  required(path.from);
  minLength(path.from, 3);
});
```

### Loading/Error/Empty State Pattern

Standardize how views handle async data:

```typescript
// Service signals
readonly loading = signal(false);
readonly error = signal<string | null>(null);
readonly items = signal<Item[]>([]);
```

```html
<!-- Template pattern -->
@if (loading()) {
  <!-- Use p-skeleton with role="status" — see UX Patterns > Loading States -->
} @else if (error()) {
  <!-- Use <app-error-state> — see UX Patterns > Error States -->
} @else if (items().length === 0) {
  <!-- Use empty state pattern — see UX Patterns > Empty States -->
} @else {
  @for (item of items(); track item.id) {
    <app-item-card [item]="item" />
  }
}
```

For detailed examples and rules, see the **UX Patterns (ENFORCED)** section below.

## UX/UI Guidelines

- **Responsive**: All UI must work on mobile devices AND desktop
- **Design**: Clean, modern aesthetic
- **Consistency**: Maintain consistent patterns across the entire application

### Accessibility (Required)

```html
<!-- Always include aria-labels on icon-only buttons -->
<button (click)="delete()" aria-label="Delete task">
  <i class="pi pi-trash"></i>
</button>

<!-- Use semantic HTML elements -->
<button>...</button>    <!-- Not <div (click)="..."> -->
<nav>...</nav>          <!-- Not <div class="nav"> -->
<main>...</main>        <!-- Not <div class="main"> -->

<!-- Include keyboard navigation for interactive elements -->
(keydown.enter)="action()"
(keydown.escape)="cancel()"
```

### UX Patterns (ENFORCED)

These patterns are established across the codebase. Follow them exactly for consistency.

#### 1. Empty States (Two Tiers)

**Page-level empty state** — used when an entire page has no content:

```html
<div class="text-center py-16">
  <i class="pi pi-file-edit text-4xl text-foreground-muted mb-4"></i>
  <p class="text-lg font-semibold text-foreground mb-2">No notes yet</p>
  <p class="text-sm text-foreground-muted">Click "New Note" to create your first note</p>
</div>
```

**Component-level empty state** — used inside panels, sidebars, or subsections:

```html
<div class="flex flex-col items-center justify-center py-8 text-foreground-muted">
  <i class="pi pi-inbox text-2xl mb-2"></i>
  <p class="text-sm">All caught up!</p>
</div>
```

**Rules:**
- Always include an icon, a heading, and (for page-level) a hint or action
- Use `text-foreground-muted` for secondary text, `text-foreground` for headings
- Differentiate between "no data yet" vs "no results for a filter/search" with appropriate messaging

#### 2. Loading States

**Page/section loading** — use PrimeNG `p-skeleton` to mimic the content layout:

```html
<div role="status" aria-label="Loading daily summary">
  <span class="sr-only">Loading daily summary...</span>
  <p-skeleton width="40%" height="28px" styleClass="mb-2" />
  <p-skeleton width="100%" height="12px" styleClass="mb-2" />
</div>
```

**Inline/action loading** — use PrimeNG spinner icon for short operations inside dialogs or buttons:

```html
<div role="status" aria-label="Analyzing transcript">
  <i class="pi pi-spin pi-spinner text-sm" aria-hidden="true"></i>
  <span class="sr-only">Analyzing transcript...</span>
</div>
```

**Rules:**
- Always add `role="status"` and `aria-label` to loading containers
- Always include a `sr-only` text alternative
- Prefer skeletons for initial page loads; use spinners for inline/action feedback

#### 3. Error States (Three Tiers)

**Page/section error** — use the shared `ErrorStateComponent`:

```html
<app-error-state
  title="Something went wrong"
  [message]="service.error()!"
  (retry)="service.reload()"
/>
```

**Field-level error** — inline validation text below the input:

```html
<small class="text-danger text-xs">Title is required</small>
```

**Mutation error** — use `ToastService` for failed API calls:

```typescript
this.toastService.error('Failed to save', 'Please try again later');
this.toastService.success({ summary: 'Meeting created', detail: 'Your meeting has been saved' });
```

**Rules:**
- Page/section errors must always offer a retry action
- Mutation errors use toasts; never show a full-page error for a failed save/delete
- Never hardcode colors — use `text-danger` and `bg-danger-bg` semantic tokens

#### 4. Dialogs

All dialogs use PrimeNG `p-dialog` with an **inline footer** (native buttons inside the template, not PrimeNG's `p-footer`).

```html
<p-dialog
  [visible]="visible()"
  (visibleChange)="visible.set($event)"
  [modal]="true"
  [draggable]="false"
  [resizable]="false"
  [dismissableMask]="true"
  [closable]="true"
  [style]="{ width: '30rem' }"
  [breakpoints]="{ '640px': '95vw' }"
  header="Dialog Title"
>
  <!-- Content -->

  <!-- Inline footer -->
  <div class="flex justify-end gap-3 px-5 py-4 border-t border-border">
    <button type="button"
      class="px-4 py-1.5 text-sm text-foreground-secondary hover:text-foreground transition-colors"
      (click)="visible.set(false)">Cancel</button>
    <button type="button"
      class="px-4 py-1.5 text-sm text-white bg-accent-solid hover:opacity-90 rounded-md transition-opacity"
      (click)="save()">Save</button>
  </div>
</p-dialog>
```

**Size presets:** sm=`24rem`, md=`30rem`, lg=`36rem`, full=`90vw`/`maxWidth:700px`

**Rules:**
- Always set `[draggable]="false"` and `[resizable]="false"`
- Set `[dismissableMask]="true"` for non-destructive dialogs
- Use `[breakpoints]="{ '640px': '95vw' }"` for responsive mobile sizing
- Footer button order: Cancel (left/text-only), Primary action (right/filled)

#### 5. Icon Buttons, Delete Confirmations, Hover-Reveal, Context Menus, Page Layout

See `UX-PATTERNS.md` for detailed reference on:
- Icon button sizes (three tiers: sm/md/lg)
- Delete confirmation patterns (inline vs dialog)
- Hover-reveal dual-element pattern
- Context menu setup with PrimeNG `p-menu`
- Page content layout with `PageContentComponent`

### UI Design Resources

- **PrimeNG v21**: https://primeng.org - Components, theming, examples
- **Tailwind CSS v4**: https://tailwindcss.com/docs - Utility classes
- **Angular 21**: https://angular.dev - Official documentation

### Mockups (Required for UI Changes)

Before implementing UI features, create a mockup HTML file in `mockups/` to explore design options.

**Requirements:**
- Create standalone HTML file using Tailwind CDN + PrimeIcons
- Include **at least 5 distinct design options**
- Mark one option as "Recommended" with reasoning
- Include pros/cons for each option

**Template:** Copy an existing file from `mockups/` (e.g., `due-date-colors.html`)

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

**Patterns to follow** (see `TaskItemTests.cs`):
- AAA pattern (Arrange-Act-Assert)
- Theory tests with InlineData for validation scenarios
- Regions to organize test methods by functionality

### E2E Tests

E2E tests are expensive to write, maintain, and run. Only add E2E tests for **critical flows that would break the business if they fail**.

**Current E2E coverage** (use `tasks.spec.ts` as the template):
- `health.spec.ts` - System health/startup verification
- `auth.spec.ts` - Authentication and access control
- `tasks.spec.ts` - Core task workflow (create, delete, kanban state transitions, priority)
- `due-date.spec.ts` - Due date display and styling
- `icon-sizing.spec.ts` - Icon rendering quality

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

### Frontend Unit Tests

Frontend unit tests run via `ng test` (Vitest + jsdom). Tests live alongside source files as `*.spec.ts`.

```bash
cd src/PraxisNote.Web/ClientApp && npx ng test --watch=false
```

**Testing patterns:**

- **Pure functions** (services, utilities): Import and test directly. No TestBed needed.
- **TipTap editor actions**: Instantiate a real `Editor` with the production `tiptapExtensions` array, execute commands, and assert against `getJSON()`, `getHTML()`, or `isActive()`.

**TipTap editor test template** (see `tiptap-editor.spec.ts`):

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

**Rules:**
- When adding a new editor action or slash command, add a corresponding test in `tiptap-editor.spec.ts`
- The "Slash Command Completeness" meta-test will fail if a new slash command is added without a test
- Shared utilities (e.g., `url-utils.ts`, `date-utils.ts`) must have their own `*.spec.ts` file

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

## Development

### Database Migrations

**Migrations merged to main are immutable.** Once merged, a migration may have been pulled by other developers or deployed to any environment.

| Phase | Can Edit? |
|-------|-----------|
| Local branch | Yes - review, customize, test freely |
| In PR (not merged) | Yes - edit based on review feedback |
| Merged to main | Never - create a new migration instead |

To create a new migration:
```bash
cd src/PraxisNote.Infrastructure
dotnet ef migrations add MigrationName --startup-project ../PraxisNote.Web
```

To remove an unapplied migration (local only):
```bash
dotnet ef migrations remove --startup-project ../PraxisNote.Web
```

### Starting the Dev Stack

Run the full stack locally with hot reload (requires Docker):

```bash
docker compose --profile dev-stack up
```

This starts:
- **PostgreSQL** on port 5432
- **.NET API** on port 5002 (with hot reload)
- **Angular** on port 4200 (with hot reload)

Open http://localhost:4200 to develop. Use the mock auth toolbar to log in.

To stop:
```bash
docker compose --profile dev-stack down
```

### Running Tests

```bash
# Unit tests
dotnet test src/PraxisNote.slnx

# E2E tests (starts its own PostgreSQL container on port 5433)
docker compose --profile e2e up -d --wait
cd tests/PraxisNote.E2E.Tests && npm test

# Clean up E2E containers
docker compose --profile e2e down
```

## Git Workflow

### Feature Branches

**ALWAYS** create a new feature branch when working on a new issue or feature. Never commit directly to `main`.

```bash
# Create and switch to a new feature branch
git checkout -b feat/short-description

# Examples:
git checkout -b feat/editor-toolbar-enhancements
git checkout -b fix/login-redirect-bug
git checkout -b chore/update-dependencies
```

**Branch naming conventions:**
- `feat/` - New features
- `fix/` - Bug fixes
- `chore/` - Maintenance tasks, refactoring, dependencies
- `docs/` - Documentation only changes

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
- Use `git restore --staged :/` before adding new files to ensure a clean staging area

### PR Workflow

**ALWAYS** use the `/pr` skill when creating or updating a pull request. This ensures README is reviewed, tests are run, the PR is properly reviewed, and CI checks are monitored for warnings.

**Markdown-only PRs**: When a PR contains ONLY markdown file changes (`.md` files), merge immediately without waiting for CI or review comments. These are documentation-only changes with no runtime impact.

## Documentation Site (Starlight)

### Structure

The user-facing documentation lives at `docs/` and is built with [Starlight](https://starlight.astro.build/) (Astro).

- Content pages: `docs/src/content/docs/*.mdx`
- Custom theme: `docs/src/styles/custom.css` (Nord palette)
- Config: `docs/astro.config.mjs`

### Deployment

Vercel auto-deploys from `docs/` on push to `main`. No GitHub Actions workflow needed.

### Docs URL

The external docs URL is defined in `src/PraxisNote.Web/ClientApp/src/app/shared/constants.ts` as `DOCS_URL`. Update this constant when a custom domain is configured.

### When to Update Docs

Any PR that changes user-facing behavior must include doc updates:
- New feature -> add/update the relevant `docs/src/content/docs/*.mdx` page
- Changed behavior -> update affected docs sections
- New keyboard shortcut -> update `keyboard-shortcuts.mdx`

### When NOT to Update Docs

Internal refactoring, backend performance, test-only changes, CI/CD, developer docs (`README.md`, `CLAUDE.md`).

### Contextual Help Links

Use the shared `<app-help-link path="..." />` component. Add links to:
- Feature page empty states
- Complex form fields (priority, due dates, tags)
- Settings panels, error states with recovery steps, onboarding flows

## Technical Debt / TODOs

_None currently tracked._
