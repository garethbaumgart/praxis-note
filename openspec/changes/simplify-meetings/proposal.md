## Why

The meetings feature has accumulated significant complexity through reflection, behavioral analysis, and insights subsystems. These features are unpolished, tightly coupled, and make the core meeting experience harder to iterate on. Stripping them out simplifies the codebase (~4,500 LOC removal across ~60 files) while preserving the core value loop: capture meeting → AI summary + action items → track follow-ups. These capabilities can be refined and re-introduced later as standalone, well-designed features.

## What Changes

- **BREAKING** Remove the entire Insights feature (dashboard, behavioral trends, communication profile, Johari window, nudges, goals)
- **BREAKING** Remove meeting reflection (prompts, submission, self-assessment, blind spot insights)
- **BREAKING** Remove behavioral analysis from AI meeting analysis (speaking dynamics, sentiment/tone, communication patterns, red flags)
- **BREAKING** Remove ExcludeFromInsights toggle from meetings
- **BREAKING** Drop 4 database columns: `BehavioralAnalysis`, `ExcludeFromInsights`, `ReflectionData`, `ReflectionSubmittedAt`
- Remove Insights nav link from sidebar and `/insights` route
- Remove home page insights widget
- Simplify AI analysis prompt to only request summary, key points, decisions, and action items
- Remove all related DTOs, type definitions, application handlers, and API endpoints

## Capabilities

### New Capabilities

_None — this is a removal/simplification change._

### Modified Capabilities

- `meeting-analysis`: Remove behavioral analysis from AI analysis output; keep summary, key points, decisions, action items

## Impact

- **Database**: Migration to drop 4 columns from Meetings table
- **API**: Remove 15+ endpoints (3 reflection, 12 insights). Modify meeting DTO (remove 4 fields + related sub-DTOs)
- **Frontend**: Delete `insights/` directory (16 files), reflection component, behavioral analysis component, home insights widget. Modify meeting editor, analysis component, details section, service, model
- **Backend**: Delete 3 reflection handlers, 4+ insight use cases, ExcludeFromInsights handler. Modify Meeting aggregate, AI analyzer prompt, DI registrations
- **Tests**: Remove ~25 domain/application tests. Update remaining meeting tests
