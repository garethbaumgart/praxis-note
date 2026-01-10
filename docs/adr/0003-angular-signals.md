# ADR-0003: Angular Signals over RxJS

## Status

Accepted

## Context

Angular historically relied on RxJS for reactive state management. While powerful, RxJS has drawbacks:
- Steep learning curve (operators, subscription management)
- Easy to create memory leaks (forgotten subscriptions)
- Verbose for simple state management
- Zone.js change detection is inefficient

Angular 16+ introduced Signals as a simpler reactive primitive with zoneless change detection support.

## Decision

Use Angular Signals as the primary state management approach:

```typescript
// Mutable state
readonly editing = signal(false);

// Derived state
readonly taskCount = computed(() => this.tasks().length);

// Component inputs
readonly task = input.required<Task>();

// Component outputs
readonly onEdit = output<string>();
```

**Patterns:**
- Use `signal()` for mutable local state
- Use `computed()` for derived/calculated state
- Use `input()` / `input.required()` for component inputs
- Use `output()` for component outputs
- Use `.asReadonly()` to expose read-only signals publicly
- Mark all signal members as `readonly`

## Consequences

**Positive:**
- Simpler mental model than RxJS
- No subscription management or memory leak concerns
- Enables zoneless change detection (better performance)
- Less boilerplate code
- Built into Angular (no additional dependencies)

**Negative:**
- Less powerful than RxJS for complex async scenarios
- May still need RxJS for HTTP calls and complex streams
- Relatively new, fewer community examples
