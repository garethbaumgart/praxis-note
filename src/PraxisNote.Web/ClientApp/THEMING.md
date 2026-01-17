# PraxisNote Theming Guide

This document describes the color and theme management system used in PraxisNote.

## Architecture Overview

PraxisNote uses a **two-tier semantic token system**:

1. **CSS Variables** (`:root` and `.dark-mode`) - Define the actual color values
2. **Tailwind `@theme` Extensions** - Map CSS variables to Tailwind utility classes

This approach provides:
- Automatic dark mode support without `dark:` prefixes
- Centralized color management in `styles.css`
- Type-safe Tailwind classes with IDE autocompletion

## Color Token Reference

### Core Tokens

| Token | Light Mode | Dark Mode | Usage |
|-------|------------|-----------|-------|
| `bg-surface` | white | gray-900 | Main background |
| `bg-surface-subtle` | gray-50 | gray-800 | Secondary background |
| `bg-surface-muted` | gray-100 | gray-700 | Tertiary background |
| `text-foreground` | gray-900 | gray-100 | Primary text |
| `text-foreground-secondary` | gray-600 | gray-400 | Secondary text |
| `text-foreground-muted` | gray-400 | gray-500 | Muted/hint text |
| `border-border` | gray-200 | gray-700 | Default borders |

### Status Columns (Kanban)

| Token | Color | Usage |
|-------|-------|-------|
| `bg-todo` / `text-todo-foreground` | Slate | Todo column |
| `bg-inprogress` / `text-inprogress-foreground` | Sky | In Progress column |
| `bg-done` / `text-done-foreground` | Emerald | Done column |
| `bg-archive` / `text-archive-foreground` | Violet | Archive column |

### Notification Types

| Token | Color | Usage |
|-------|-------|-------|
| `bg-feature` / `text-feature-foreground` | Purple | Feature notifications |
| `bg-bugfix` / `text-bugfix-foreground` | Red | Bug fix notifications |
| `bg-improvement` / `text-improvement-foreground` | Blue | Improvement notifications |

### Date Urgency

| Token | Color | Usage |
|-------|-------|-------|
| `bg-overdue` / `text-overdue-foreground` | Rose | Overdue tasks |
| `bg-due-today` / `text-due-today-foreground` | Amber | Due today |
| `bg-due-soon` / `text-due-soon-foreground` | Amber (lighter) | Due tomorrow |
| `bg-due-later` / `text-due-later-foreground` | Slate | Due in 2-7 days |
| `bg-due-done` / `text-due-done-foreground` | Slate (muted) | Completed with due date |

### Interactive & Danger

| Token | Color | Usage |
|-------|-------|-------|
| `text-danger` | Rose | Destructive actions, priority flags |
| `text-danger-hover` | Rose (lighter) | Danger hover state |
| `bg-interactive` / `text-interactive-foreground` | Violet | Active state indicators (sort, toggles) |

### Brand

| Class | Usage |
|-------|-------|
| `.bg-brand-gradient` | Logo backgrounds, sidebar icons |

## Usage Guidelines

### DO: Use semantic tokens

```html
<!-- Good: Uses semantic token -->
<button class="bg-surface text-foreground border-border">

<!-- Good: Status-aware coloring -->
<div class="bg-todo text-todo-foreground">
```

### DON'T: Use hardcoded colors

```html
<!-- Bad: Hardcoded Tailwind colors -->
<button class="bg-gray-100 text-gray-900 border-gray-200">

<!-- Bad: Using dark: prefix -->
<span class="bg-amber-100 dark:bg-amber-900">
```

### DON'T: Use Tailwind's `dark:` prefix

PraxisNote uses a custom `.dark-mode` class system, not Tailwind's native dark mode. The CSS variables automatically switch values when `.dark-mode` is applied to the document root.

## Adding New Tokens

1. **Define CSS variables** in `:root` (light) and `.dark-mode` (dark) blocks in `styles.css`
2. **Add Tailwind mapping** in the `@theme` block
3. **Document** the new token in this file

Example:
```css
/* styles.css */
:root {
  --color-warning-bg: var(--color-yellow-100);
  --color-warning-text: var(--color-yellow-800);
}

.dark-mode {
  --color-warning-bg: var(--color-yellow-900);
  --color-warning-text: var(--color-yellow-200);
}

@theme {
  --color-warning: var(--color-warning-bg);
  --color-warning-foreground: var(--color-warning-text);
}
```

## Dark Mode Implementation

Dark mode is managed by `ThemeService`:
- Toggles `.dark-mode` class on `document.documentElement`
- Persists preference to localStorage
- Respects system preference (`prefers-color-scheme`) on first visit

To toggle theme programmatically:
```typescript
import { ThemeService } from './shared/theme.service';

themeService.toggleTheme();
// or
themeService.theme() // 'light' | 'dark'
```

## File Locations

- **Token definitions**: `src/styles.css`
- **Theme service**: `src/app/shared/theme.service.ts`
- **Status color pipe**: `src/app/shared/pipes/status-color.pipe.ts`
