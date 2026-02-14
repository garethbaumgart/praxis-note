# PraxisNote

[![Deploy](https://img.shields.io/github/actions/workflow/status/garethbaumgart/praxis-note/deploy-cloud-run.yml?branch=main&label=Deploy)](https://github.com/garethbaumgart/praxis-note/actions/workflows/deploy-cloud-run.yml)
![Unit Tests](https://img.shields.io/endpoint?url=https://gist.githubusercontent.com/garethbaumgart/093c03c9b23736d0600e9eeb2e772063/raw/unit-tests.json)
![E2E Tests](https://img.shields.io/endpoint?url=https://gist.githubusercontent.com/garethbaumgart/093c03c9b23736d0600e9eeb2e772063/raw/e2e-tests.json)
![Coverage](https://img.shields.io/endpoint?url=https://gist.githubusercontent.com/garethbaumgart/093c03c9b23736d0600e9eeb2e772063/raw/coverage.json)

A task management system with a kanban board. Currently features drag-and-drop task management with three-state workflow (Todo → In Progress → Done).

**Live:** https://praxisnote-kv77ni5hzq-ts.a.run.app

## Overview

PraxisNote is evolving into a note-first task management system. The vision: write naturally in rich text notes, and any checkbox you create automatically becomes a trackable task on your board with bidirectional sync.

### Key Features

- ✅ **Three-State Tasks** - Todo → In Progress → Done lifecycle with timestamps
- ✅ **Kanban Board** - Drag-and-drop task management with inline creation
- ✅ **Due Dates** - Set due dates with visual urgency indicators (overdue, today, tomorrow, this week)
- ✅ **Task Comments** - Add comments to tasks with click-to-edit
- ✅ **Task Tags** - Organize tasks with reusable tags, displayed inline with expandable overflow
- ✅ **Note Tags** - Add tags to notes for organization and filtering
- ✅ **Google OAuth** - Secure authentication with user accounts
- ✅ **Notes** - Google Keep-style card grid with TipTap rich text editor and collapsible toggle sections
- ✅ **Checkbox-Task Sync** - Promote checkboxes to tasks with bidirectional sync (complete a task, checkbox is checked; check a checkbox, task is done)
- ✅ **Meetings** - Daily grouped meeting list with fire-and-forget capture workflow
- ✅ **AI Analysis** - Claude-powered meeting transcript analysis for summaries, key points, and decisions
- ✅ **Live Transcription** - Real-time speech-to-text via Deepgram Nova-3 during browser recording
- ✅ **Browser Recording** - Record meeting audio directly from the browser microphone with real-time level metering
- ✅ **Google Calendar Sync** - Connect Google Calendar via OAuth and manually sync upcoming events as meetings
- ✅ **Self-Reflection Prompts** - Post-meeting reflection with contextual prompts generated from behavioral analysis, blind spot insights comparing self-assessment to AI analysis
- ✅ **Speaker Identification** - Multichannel audio separation and voice diarization to label who said what in transcripts
- ✅ **Screenshot Import** - Paste or drop a calendar screenshot to extract and import meetings via Claude Vision with automatic timezone detection
- ✅ **Behavioral Insights Dashboard** - Compact 2-column sparkline grid with trend indicators for meeting behavioral metrics
- ✅ **Tag Hub** - Unified view of all notes, meetings, and tasks for a selected tag with date-grouped timeline, searchable tag selector, direct navigation to items, inline rename/delete with cascading removal, and two-step tag merge with overlap detection
- ✅ **Outstanding Action Items** - Home page widget showing incomplete meeting action items from the last 30 days, cross-referenced with linked task status
- ✅ **Multi-Profile Support** - Separate data contexts (e.g., Work, Personal) with sidebar profile switcher, per-profile data isolation, and account linking via temporary codes

✅ = Implemented | 🚧 = Planned

## Tech Stack

- .NET 10, Angular 21, PrimeNG, Tailwind CSS
- EF Core with PostgreSQL
- xUnit + Playwright for testing
- DDD (Domain-Driven Design)

## Getting Started

### Prerequisites

- Docker

### Run Locally

```bash
docker compose --profile dev-stack up
```

Open http://localhost:4200. Use the mock auth toolbar at the bottom to log in (no Google OAuth setup required).

#### Optional: AI Meeting Analysis

To enable AI-powered meeting transcript analysis, you need an Anthropic API key:

1. **Get an API key** from https://console.anthropic.com/ (under API Keys)
2. **Set via dotnet user secrets**:

```bash
cd src/PraxisNote.Web
dotnet user-secrets set "MeetingAnalysis:ApiKey" "sk-ant-your-key-here"
```

Without the API key, the app runs normally but clicking "Analyze" on meetings will show "Analysis failed".

#### Optional: Live Transcription (Deepgram)

To enable real-time speech-to-text during meeting recordings:

1. **Create a free account** at https://deepgram.com (includes $200 free credit)
2. **Create an API key** in the Deepgram console
3. **Set via dotnet user secrets**:

```bash
cd src/PraxisNote.Web
dotnet user-secrets set "Deepgram:ApiKey" "your-deepgram-api-key"
```

Without the API key, the app runs normally but recording won't produce live transcripts.

#### Optional: Google Calendar Sync

To enable importing meetings from Google Calendar, you need to configure a Google OAuth client:

1. **Create a Google Cloud project** at https://console.cloud.google.com/
2. **Enable the Google Calendar API**: Go to **APIs & Services** > **Library**, search for "Google Calendar API", and click **Enable**
3. **Configure the OAuth consent screen**: Go to **APIs & Services** > **OAuth consent screen**
   - Set the app to **Testing** mode
   - Add your Google account email under **Test users**
4. **Create OAuth credentials**: Go to **APIs & Services** > **Credentials** > **Create Credentials** > **OAuth client ID**
   - Application type: **Web application**
   - Add these **Authorized redirect URIs**:
     - `http://localhost:5002/api/calendar/callback/google` (local development)
     - `https://your-production-domain/api/calendar/callback/google` (production)
5. **Set the environment variables** before starting the dev stack:

```bash
export Authentication__Google__ClientId="your-client-id.apps.googleusercontent.com"
export Authentication__Google__ClientSecret="your-client-secret"
docker compose --profile dev-stack up
```

Without these credentials, the app runs normally but the Google Calendar connect button in Settings will show an error.

To stop:
```bash
docker compose --profile dev-stack down
```

### Run Tests

```bash
# Unit tests
dotnet test src/PraxisNote.slnx

# E2E tests
docker compose --profile e2e up -d --wait
cd tests/PraxisNote.E2E.Tests && npm test
docker compose --profile e2e down
```

## CI/CD

GitHub Actions runs on every push to `main` and on pull requests:

- **Unit Tests** - Runs domain tests
- **E2E Tests** - Spins up PostgreSQL, runs Playwright tests
- **AI Reviews** - CodeRabbit and Copilot provide code review on PRs
- **Deploy** - Auto-deploys to Google Cloud Run (Sydney) with Neon PostgreSQL

Dependency updates are automated via [Renovate](https://renovatebot.com/).

### Production Secrets

Secrets are stored in **Google Cloud Secret Manager** and injected into Cloud Run at deploy time.

To enable AI meeting analysis in production:

1. Create a secret in GCP Secret Manager named `ANTHROPIC_API_KEY`
2. Add your Anthropic API key (`sk-ant-...`) as the secret value
3. Grant the Cloud Run service account access to the secret

The deploy workflow automatically maps this to `MeetingAnalysis__ApiKey` in Cloud Run.

To enable live transcription in production:

1. Create a secret in GCP Secret Manager named `DEEPGRAM_API_KEY`
2. Add your Deepgram API key as the secret value
3. Grant the Cloud Run service account access to the secret

The deploy workflow automatically maps this to `Deepgram__ApiKey` in Cloud Run.

To enable Google Calendar sync in production:

1. Create secrets in GCP Secret Manager: `GOOGLE_CLIENT_ID` and `GOOGLE_CLIENT_SECRET`
2. Add your Google OAuth credentials as the secret values
3. Grant the Cloud Run service account access to the secrets
4. Ensure the production redirect URI (`https://your-domain/api/calendar/callback/google`) is added to the Google OAuth client's authorized redirect URIs

## Architecture

See [Architecture Decision Records](docs/adr/) for documented decisions on patterns and technology choices.

## Claude Code Skills

Custom slash commands in `.claude/skills/` that automate development workflows:

| Skill | Usage | Purpose |
|-------|-------|---------|
| `/refine <issue>` | `/refine 336` | Reads a GitHub issue, explores the codebase, creates UX mockups if needed, writes a step-by-step implementation plan, and updates the issue body |
| `/pr` | `/pr` | Creates a pull request with build, tests, self-review, browser validation, and AI review monitoring |
| `/broadcast` | `/broadcast` | Generates an EF Core migration to add a "What's New" notification for user-facing changes |
| `/execute-issues` | `/execute-issues 340 341 342` | Scans issues for readiness, then sequentially implements each one (refine, branch, implement, PR, broadcast, merge) |
