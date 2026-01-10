# ADR-0004: PrimeNG + Tailwind, No Custom CSS

## Status

Accepted

## Context

Building a consistent, maintainable UI requires clear guidelines. Common problems include:
- Inconsistent styling across components
- CSS specificity battles
- Difficulty theming
- Growing, unmaintainable CSS files
- Reinventing common UI patterns

We needed a UI approach that provides consistency while remaining flexible.

## Decision

Use PrimeNG components + Tailwind CSS utilities exclusively. **No custom CSS files.**

**PrimeNG**: Provides pre-built, accessible, themeable components (dialogs, buttons, inputs, etc.)

**Tailwind CSS**: Provides utility classes for layout, spacing, colors, and responsive design

**Rules:**
- Use PrimeNG for complex interactive components
- Use Tailwind utilities for layout and styling
- Never write custom CSS files or `<style>` blocks
- Use Tailwind's theming for custom colors/design tokens

## Consequences

**Positive:**
- Consistent UI out of the box
- No CSS specificity issues
- Easy to understand styling (all in the template)
- PrimeNG handles accessibility
- Rapid prototyping with utility classes
- Easy to maintain and refactor

**Negative:**
- Long class strings in templates
- Limited to what PrimeNG + Tailwind provide (rarely an issue)
- Learning curve for Tailwind utility classes
- Some unique designs may be harder to achieve
