# UX Patterns Reference

This file contains detailed reference patterns for UI components. It is linked from `CLAUDE.md` to keep the main file under ~700 lines.

## Icon Button Sizes (Three Tiers)

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

## Delete Confirmations

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

## Hover-Reveal Pattern

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

## Context Menus

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

## Page Content Layout

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
