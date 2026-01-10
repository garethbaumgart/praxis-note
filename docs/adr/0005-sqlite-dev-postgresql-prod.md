# ADR-0005: SQLite for Dev, PostgreSQL for Prod

## Status

Accepted

## Context

Development and production environments have different requirements:

**Development:**
- Fast setup, no external dependencies
- Easy to reset/recreate database
- Works offline
- Minimal configuration

**Production:**
- Reliable, scalable database
- Concurrent connections
- Full feature support
- Backup and recovery

## Decision

Use SQLite for development and PostgreSQL for production.

**Development (SQLite):**
- Zero configuration, file-based
- Automatic creation on first run
- Easy to delete and recreate

**Production (PostgreSQL):**
- Industry-standard reliability
- Full SQL feature support
- Scales with application needs

EF Core abstracts the database differences, allowing the same code to work with both.

## Consequences

**Positive:**
- Fast, friction-free local development
- No Docker/database setup required for development
- Production uses battle-tested PostgreSQL
- EF Core handles most differences transparently

**Negative:**
- Some SQL features differ between SQLite and PostgreSQL
- Must test with PostgreSQL before production deployments
- Some queries may need adjustments (see Technical Debt in CLAUDE.md)
  - Example: `DateTimeOffset` ordering doesn't work in SQLite, requires in-memory sorting

**Mitigation:**
- E2E tests run against PostgreSQL to catch compatibility issues
- Document known differences in CLAUDE.md Technical Debt section
