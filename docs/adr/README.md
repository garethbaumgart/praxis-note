# Architecture Decision Records

This directory contains Architecture Decision Records (ADRs) for the PraxisNote project.

## Index

| ADR | Title | Status |
|-----|-------|--------|
| [0001](0001-vertical-slice-architecture.md) | Vertical Slice Architecture | Accepted |
| [0002](0002-cqrs-pattern.md) | CQRS Pattern (without MediatR) | Accepted |
| [0003](0003-angular-signals.md) | Angular Signals over RxJS | Accepted |
| [0004](0004-primeng-tailwind-no-custom-css.md) | PrimeNG + Tailwind, No Custom CSS | Accepted |
| [0005](0005-sqlite-dev-postgresql-prod.md) | SQLite for Dev, PostgreSQL for Prod | Superseded by [0007](0007-postgresql-all-environments.md) |
| [0006](0006-domain-layer-purity.md) | Domain Layer Purity | Accepted |
| [0007](0007-postgresql-all-environments.md) | PostgreSQL for All Environments | Accepted |

## Template

When creating a new ADR, use this template:

```markdown
# ADR-NNNN: Title

## Status

Proposed | Accepted | Deprecated | Superseded by [ADR-NNNN](NNNN-title.md)

## Context

What is the issue that we're seeing that is motivating this decision or change?

## Decision

What is the change that we're proposing and/or doing?

## Consequences

What becomes easier or more difficult to do because of this change?
```

## Guidelines

- **When to write an ADR**: Choosing between multiple valid approaches, decisions that are hard to reverse, decisions others might question later
- **When NOT to write an ADR**: Obvious choices, small implementation details
- **Naming**: `NNNN-short-title.md` (e.g., `0001-vertical-slice-architecture.md`)
- **Immutability**: Don't modify accepted ADRs. Instead, create a new ADR that supersedes the old one.
