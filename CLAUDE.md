# CLAUDE.md - Project Preferences

## Backend (.NET 10)

- **Architecture**: Vertical Slice Architecture (feature folders, not layer folders)
- **Pattern**: CQRS (Command Query Responsibility Segregation)
- **DDD Principles**: Repository interfaces live in Domain layer
- **Clean Architecture Layers**: Domain → Application → Infrastructure → Web
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

### Template Syntax (ENFORCED)

Use Angular's new control flow syntax. The old structural directives are **banned**.

```html
<!-- ✅ USE: New control flow -->
@if (loading()) { <spinner /> }
@for (item of items(); track item.id) { ... }
@switch (status()) { @case ('done') { ... } }

<!-- ❌ BANNED: Old directives -->
*ngIf, *ngFor, *ngSwitch

<!-- ✅ USE: Class bindings -->
[class.active]="isActive()"
[class.text-red-500]="hasError()"

<!-- ❌ BANNED: ngClass -->
[ngClass]="{'active': isActive()}"
```

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

### Zoneless Anti-Patterns

Angular 21 is zoneless by default. These patterns **will NOT work**:

```typescript
// ❌ Plain properties don't trigger change detection
items: Item[] = [];
this.items.push(newItem);  // View won't update!

// ✅ Use signals instead
readonly items = signal<Item[]>([]);
this.items.update(arr => [...arr, newItem]);  // View updates!

// ❌ Don't mutate signal values directly
this.items().push(newItem);  // Wrong!

// ✅ Always create new references
this.items.update(arr => [...arr, newItem]);  // Correct!
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

For detailed examples and rules, see the **UX Patterns (ENFORCED)** section under UX/UI Guidelines.

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
<button>...</button>    <!-- ✅ Not <div (click)="..."> -->
<nav>...</nav>          <!-- ✅ Not <div class="nav"> -->
<main>...</main>        <!-- ✅ Not <div class="main"> -->

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
- Prefer skeletons for initial page loads; use spinners for inline/action feedback (e.g., inside dialogs, after button clicks)

#### 3. Error States (Three Tiers)

**Page/section error** — use the shared `ErrorStateComponent`:

```html
<app-error-state
  title="Something went wrong"
  [message]="service.error()!"
  (retry)="service.reload()"
/>
```

The component supports `size` (`'md'` | `'sm'`), optional `showRetry`, and uses semantic tokens (`text-danger`, `bg-danger-bg`). See `src/app/shared/components/error-state.component.ts`.

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

**Standard dialog setup:**

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

**Size presets:**

| Size | Width | Use case |
|------|-------|----------|
| sm | `24rem` | Confirmations, simple prompts |
| md | `30rem` | Forms, import dialogs |
| lg | `36rem` | Multi-section editors |
| full | `90vw` / `maxWidth: 700px` | Rich editors (notes) |

**Rules:**
- Always set `[draggable]="false"` and `[resizable]="false"`
- Set `[dismissableMask]="true"` for read-only or non-destructive dialogs
- For destructive dialogs (delete confirmations), omit `dismissableMask` or bind it conditionally
- Use `[breakpoints]="{ '640px': '95vw' }"` for responsive mobile sizing
- Footer button order: Cancel (left/text-only), Primary action (right/filled)

#### 5. Icon Button Sizes (Three Tiers)

| Size | Classes | Pixels | Use case |
|------|---------|--------|----------|
| sm | `w-7 h-7` | 28px | Inline/row actions (kebab menus, tag buttons, checkboxes) |
| md | `w-9 h-9` | 36px | Toolbar/header buttons (navigation, page-level actions) |
| lg | `w-11 h-11` | 44px | Reserved for large touch targets (future use) |

**Standard icon button pattern:**

```html
<button type="button"
  class="touch-target w-9 h-9 flex items-center justify-center rounded-lg
         text-foreground-muted hover:bg-surface-muted transition"
  (click)="action()"
  aria-label="Action description">
  <i class="pi pi-icon-name text-sm"></i>
</button>
```

**Rules:**
- Always add `touch-target` class for WCAG 2.5.8 compliance (invisible 44px hit area)
- Always include `aria-label` on icon-only buttons
- Use `flex items-center justify-center` for centering

#### 6. Delete Confirmations

**Inline confirm (for list items/cards)** — use `DeleteConfirmButtonComponent`:

```html
@if (confirmingDelete()) {
  <app-delete-confirm-button
    ariaLabel="Confirm delete note"
    (onConfirm)="confirmDelete()"
    (click)="$event.stopPropagation()"
  />
} @else {
  <button type="button"
    class="touch-target p-1.5 text-foreground-muted hover:text-danger rounded transition-colors"
    (click)="startDeleteConfirm(); $event.stopPropagation()"
    aria-label="Delete note">
    <i class="pi pi-trash text-xs"></i>
  </button>
}
```

See `src/app/shared/components/delete-confirm-button.component.ts`. The component shows "Confirm?" with a shrinking progress bar countdown.

**Dialog confirm (for destructive actions with context)** — use a `p-dialog` with `width: '24rem'`:

```html
<div class="flex justify-end gap-2 mt-4">
  <button type="button"
    class="px-4 py-2 text-sm border border-border rounded-lg text-foreground-secondary hover:bg-surface-muted transition"
    (click)="cancelDelete()">Cancel</button>
  <button type="button"
    class="px-4 py-2 text-sm bg-danger text-white rounded-lg font-medium hover:opacity-90 transition"
    (click)="confirmDelete()">Delete</button>
</div>
```

**When to use which:**
- **Inline confirm**: Card/row items where deletion is quick and context is obvious
- **Dialog confirm**: When the user needs to see impact details (e.g., "This tag is used by 5 tasks and 3 notes")

#### 7. Hover-Reveal Pattern

Actions that appear on hover (desktop) must always be visible on mobile. Use the dual-element pattern:

```html
<!-- Parent must have `group` class -->
<div class="group ...">
  <!-- Mobile: always visible -->
  <div class="flex md:hidden items-center gap-1">
    <button class="touch-target p-1.5 ..." aria-label="Delete">
      <i class="pi pi-trash text-xs"></i>
    </button>
  </div>

  <!-- Desktop: hover/focus reveal -->
  <div class="hidden md:flex md:opacity-0 md:pointer-events-none
              md:group-hover:opacity-100 md:group-hover:pointer-events-auto
              md:group-focus-within:opacity-100 md:group-focus-within:pointer-events-auto
              items-center gap-1 transition-opacity">
    <button class="touch-target p-1.5 ..." aria-label="Delete">
      <i class="pi pi-trash text-xs"></i>
    </button>
  </div>
</div>
```

**Rules:**
- Mobile button: `flex md:hidden` (always visible on mobile, hidden on desktop)
- Desktop button: `hidden md:flex md:opacity-0 md:group-hover:opacity-100` (hidden on mobile, fade-in on desktop hover)
- Always include `md:group-focus-within:opacity-100` and `md:group-focus-within:pointer-events-auto` for keyboard accessibility
- Both elements render the same actions — this is intentional for layout consistency

#### 8. Context Menus

Use PrimeNG `p-menu` with `[popup]="true"` for all context/action menus.

```html
<!-- Trigger button -->
<button type="button"
  class="touch-target w-7 h-7 flex items-center justify-center rounded
         text-foreground-muted hover:bg-surface-muted transition"
  (click)="menu.toggle($event)"
  aria-label="Actions for {{ item.name }}">
  <i class="pi pi-ellipsis-v text-xs"></i>
</button>

<!-- Menu (rendered at body level) -->
<p-menu #menu [model]="menuItems()" [popup]="true" appendTo="body" />
```

**Trigger icon conventions:**
- `pi-ellipsis-v` (vertical dots) — row/card action menus
- `pi-ellipsis-h` (horizontal dots) — toolbar overflow menus

**Rules:**
- Always set `appendTo="body"` to avoid clipping issues
- Always include `aria-label` on the trigger button describing which item it acts on
- Menu items use PrimeNG `MenuItem[]` model with `label`, `icon`, and `command`

#### 9. Page Content Layout

All feature pages must use the shared `PageContentComponent` for their outermost content wrapper:

```html
<!-- Standard page (default max-w-6xl) -->
<app-page-content>
  <h1 class="sr-only">Page Title</h1>
  <!-- page content -->
</app-page-content>

<!-- Narrow page (max-w-3xl, e.g., Settings) -->
<app-page-content maxWidth="narrow">
  <!-- page content -->
</app-page-content>
```

**Rules:**
- Every routed page component must wrap its main layout/content in `<app-page-content>`
- Root-level overlay components (e.g., dialogs, popup menus) may be declared alongside `<app-page-content>` when they must exist at the page root
- Never duplicate the container classes manually (`max-w-6xl mx-auto px-6 md:px-8 py-8 md:py-10`)
- Use `maxWidth="narrow"` for focused/form pages like Settings

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

## Theming Conventions

PraxisNote uses a semantic token system for colors. **These rules are mandatory:**

- **ALWAYS use semantic tokens** from `styles.css` (e.g., `bg-surface`, `text-foreground`, `bg-todo`)
- **NEVER use hardcoded Tailwind colors** (e.g., `bg-gray-100`, `text-violet-600`)
- **NEVER use `dark:` prefix** - our CSS variable system handles dark mode automatically
- **Respect token semantics**: Use `-foreground` tokens for text, background tokens for backgrounds. Don't use a foreground color as a background.
- **New colors**: If no suitable token exists, add semantic tokens to `styles.css` in both `:root` (light) and `[data-theme="dark"]` (dark) blocks within `@layer theme`, then map them in `@theme inline`

### Component CSS Colors

When using colors in component `styles: []` (not Tailwind classes), use CSS variables:

```css
/* ✅ Good: Use semantic CSS variables */
background: var(--color-surface-subtle);
color: var(--color-foreground);
border-color: var(--color-border);

/* ❌ Bad: Hardcoded colors */
background: #f5f5f5;
color: rgb(51, 51, 51);

/* ❌ Bad: Using foreground token as background */
background: var(--color-todo-foreground);  /* This is a text color! */
```

**Before adding component CSS colors**: Check if a semantic token exists in `THEMING.md`. If not, add one to `styles.css` first.

**Reference**: See `src/PraxisNote.Web/ClientApp/THEMING.md` for the full token reference and usage guidelines.

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
// ✅ Good - Clear and descriptive
Create_WithValidContent_ReturnsComment()
Create_WithNullContent_ThrowsArgumentException()
Complete_WhenAlreadyDone_PreservesOriginalCompletedAt()

// ❌ Bad - Vague or meaningless
TestCreate()
Test1()
WorksCorrectly()
```

### Domain Unit Tests

**Target: ~100% coverage** for the Domain layer. Domain contains core business logic, aggregates, entities, and value objects. These are pure, deterministic, and easy to test—no excuse for gaps.

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

### Flaky Tests Are Not Acceptable

**Zero tolerance for flaky tests.** A flaky test is one that sometimes passes and sometimes fails without code changes. Flaky tests:
- Erode trust in the test suite
- Waste developer time investigating false failures
- Train developers to ignore test failures

**If a test is flaky, fix it immediately:**
1. Identify the root cause (timing, race conditions, state leakage)
2. Fix the underlying issue, don't just add retries
3. Run the test multiple times locally to verify reliability

**Common causes of flaky E2E tests:**
- Using `page.route()` for auth headers (use `page.setExtraHTTPHeaders()` instead - it's more reliable)
- Using `waitForLoadState('networkidle')` when SSE connections are open (they never complete)
- Shared test data without proper cleanup
- Hardcoded timeouts instead of proper waits

## Development

### Database Migrations

**Migrations merged to main are immutable.** Once merged, a migration may have been pulled by other developers or deployed to any environment.

| Phase | Can Edit? |
|-------|-----------|
| Local branch | ✅ Yes - review, customize, test freely |
| In PR (not merged) | ✅ Yes - edit based on review feedback |
| Merged to main | ❌ Never - create a new migration instead |

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

### PR Workflow

**ALWAYS** use the `/pr` skill when creating or updating a pull request. This ensures README is reviewed, tests are run, the PR is properly reviewed, and CI checks are monitored for warnings.

**Markdown-only PRs**: When a PR contains ONLY markdown file changes (`.md` files), merge immediately without waiting for CI or review comments. These are documentation-only changes with no runtime impact.

## Technical Debt / TODOs

_None currently tracked._
