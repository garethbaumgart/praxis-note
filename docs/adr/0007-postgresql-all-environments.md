# ADR-0007: PostgreSQL for All Environments

## Status

Accepted

## Context

ADR-0005 established SQLite for development and PostgreSQL for production. This caused several issues:

- **SQL behavior differences**: `DateTimeOffset` ordering, case-sensitivity, and JSON functions behave differently between SQLite and PostgreSQL, leading to bugs that only surfaced in production.
- **Workarounds accumulating**: In-memory sorting, `.ToLower()` workarounds, and other hacks were needed to bridge the gap.
- **Docker Compose already required**: The dev stack uses Docker Compose for other services, so adding PostgreSQL adds no new dependency.
- **E2E tests already use PostgreSQL**: Tests run against PostgreSQL, so the dev database was the only remaining SQLite holdout.

## Decision

Use PostgreSQL for all environments:

- **Development**: PostgreSQL via Docker Compose (`docker compose --profile dev-stack up`)
- **Production**: PostgreSQL via Cloud SQL

This eliminates the SQLite/PostgreSQL abstraction gap entirely.

## Consequences

**Positive:**
- Zero SQL behavior differences between dev and prod
- No need for database-specific workarounds
- Migrations can use PostgreSQL-specific features (e.g., `jsonb` functions) without compatibility concerns
- Simpler mental model — one database engine everywhere

**Negative:**
- Docker is now required for local development (was already a practical requirement)
- Slightly slower initial setup compared to SQLite's zero-config approach

**Mitigation:**
- `docker compose --profile dev-stack up` is a single command to start everything
- PostgreSQL data persists across container restarts via Docker volumes
