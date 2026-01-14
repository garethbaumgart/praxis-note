# CLAUDE.md - Project Preferences

## Backend (.NET 10)

- **Architecture**: Vertical Slice Architecture (feature folders, not layer folders)
- **Pattern**: CQRS (Command Query Responsibility Segregation)
- **DDD Principles**: Repository interfaces live in Domain layer
- **Clean Architecture Layers**: Domain → Application → Infrastructure → Web
- **Style**: Primary constructors, Minimal APIs
- **Database**: Entity Framework Core with SQLite (dev) / PostgreSQL (prod)

## Frontend (Angular v21)

### Core Principles

- **Standalone components only** - No NgModules
- **Signals for state** - Zoneless change detection (Zone.js not included)
- **DI via `inject()`** - Not constructor injection

### Component Decision Hierarchy

When building UI, follow this order strictly:

1. **PrimeNG Component** - ALWAYS check https://primeng.org first
2. **Tailwind Utilities** - Style with utility classes only
3. **Custom CSS (LAST RESORT)** - Only for:
   - Library integration (CDK drag-drop, third-party)
   - Animation keyframes
   - CSS custom properties/theming
   - PrimeNG component overrides that can't be done via theming

**Before writing custom CSS, ask:** "Can this be achieved with Tailwind utilities or PrimeNG props?"

### PrimeNG First

Before creating ANY custom UI element, check if PrimeNG has it:

- Buttons, inputs, dialogs → PrimeNG
- Dropdowns, menus, tooltips → PrimeNG
- Tables, cards, panels → PrimeNG
- Date pickers, sliders, toggles → PrimeNG

**Reference:** https://primeng.org/installation (component showcase)

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

// Form inputs - use native event binding, NOT ngModel
[value]="mySignal()"
(input)="mySignal.set($any($event.target).value)"

// PrimeNG two-way binding
[visible]="showDialog()"
(visibleChange)="showDialog.set($event)"
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
  <app-skeleton />
} @else if (error()) {
  <p class="text-red-500">{{ error() }}</p>
} @else if (items().length === 0) {
  <p class="text-foreground-muted text-center py-8">No items found</p>
} @else {
  @for (item of items(); track item.id) {
    <app-item-card [item]="item" />
  }
}
```

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

### UI Design Resources

When designing UI components, reference these official documentation sites:

- **PrimeNG v21**: https://primeng.org - Component API, examples, theming
- **PrimeNG Showcase**: https://primeng.org/installation - Live demos of every component
- **Tailwind CSS v4**: https://tailwindcss.com/docs - Utility class reference
- **Angular 21**: https://angular.dev - Official Angular documentation

### Mockups (Required for UI Changes)

Before implementing UI features, create a mockup HTML file in `mockups/` to explore design options.

**Requirements:**
- Create a standalone HTML file in `mockups/`
- Include **at least 5 distinct design options** to thoroughly explore the design space
- Use the app's current styling (Tailwind classes, color tokens)
- Mark one option as "Recommended" with reasoning
- Include pros/cons or issues for each option

**Mockup Template:**

```html
<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <title>[Feature Name] Options</title>
  <script src="https://cdn.tailwindcss.com"></script>
  <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/primeicons@7.0.0/primeicons.css">
</head>
<body class="bg-gray-100 p-8">
  <h1 class="text-2xl font-bold mb-2">[Feature Name] Options</h1>
  <p class="text-gray-600 mb-8">[Brief description of what we're designing]</p>

  <div class="grid grid-cols-1 lg:grid-cols-2 gap-8">
    <!-- Option A (Recommended) -->
    <div class="bg-white rounded-xl p-6 shadow-sm border-2 border-emerald-400">
      <div class="flex items-center gap-2 mb-4">
        <h2 class="text-lg font-semibold">Option A: [Name]</h2>
        <span class="text-xs bg-emerald-100 text-emerald-700 px-2 py-0.5 rounded-full">Recommended</span>
      </div>
      <!-- Mock content here -->
    </div>

    <!-- Option B -->
    <div class="bg-white rounded-xl p-6 shadow-sm">
      <h2 class="text-lg font-semibold mb-4">Option B: [Name]</h2>
      <!-- Mock content here -->
    </div>
  </div>
</body>
</html>
```

**Examples:** See existing mockups in `mockups/` folder (e.g., `due-date-colors.html`, `comments-display-options.html`)

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

### E2E Tests

E2E tests are expensive to write, maintain, and run. Only add E2E tests for **critical flows that would break the business if they fail**.

**Current E2E coverage** (use `tasks.spec.ts` as the template):
- `health.spec.ts` - System health/startup verification
- `auth.spec.ts` - Authentication and access control
- `tasks.spec.ts` - Core task workflow (create, delete, kanban state transitions)

**DO write E2E tests for:**
- Authentication/authorization flows
- Core business operations (task CRUD, workflow state changes)
- Critical user journeys that span API + UI

**DO NOT write E2E tests for:**
- Individual UI components or styling changes
- New buttons, dialogs, or form fields (unit test these instead)
- Features already covered by existing workflow tests
- Non-critical enhancements

When adding a new feature, ask: "If this breaks, does the app become unusable?" If no, skip the E2E test.

## Development

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

## PR Workflow

**ALWAYS** use the `/pr` skill when creating or updating a pull request. This ensures README is reviewed, tests are run, the PR is properly reviewed, and CI checks are monitored for warnings.

**Markdown-only PRs**: When a PR contains ONLY markdown file changes (`.md` files), merge immediately without waiting for CI or review comments. These are documentation-only changes with no runtime impact.

## Technical Debt / TODOs

When switching from SQLite to PostgreSQL:
- [ ] `TaskRepository.GetByUserIdAsync` - Move `OrderByDescending(t => t.CreatedAt)` back to the EF query (currently sorting in memory due to SQLite DateTimeOffset limitation)
