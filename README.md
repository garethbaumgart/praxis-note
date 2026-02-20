# PraxisNote

[![Deploy](https://img.shields.io/github/actions/workflow/status/garethbaumgart/praxis-note/deploy-cloud-run.yml?branch=main&label=Deploy)](https://github.com/garethbaumgart/praxis-note/actions/workflows/deploy-cloud-run.yml)
![Unit Tests](https://img.shields.io/endpoint?url=https://gist.githubusercontent.com/garethbaumgart/093c03c9b23736d0600e9eeb2e772063/raw/unit-tests.json)
![E2E Tests](https://img.shields.io/endpoint?url=https://gist.githubusercontent.com/garethbaumgart/093c03c9b23736d0600e9eeb2e772063/raw/e2e-tests.json)
![Coverage](https://img.shields.io/endpoint?url=https://gist.githubusercontent.com/garethbaumgart/093c03c9b23736d0600e9eeb2e772063/raw/coverage.json)

**Live:** https://praxisnote-kv77ni5hzq-ts.a.run.app

## What Is This?

PraxisNote started with a simple idea: write naturally in rich text notes, create checkboxes inline, and watch them sync to a kanban board. No context switching, no forced workflows — just write what you need and let the system track it.

It's grown into a productivity tool for people who think in notes, meet with humans, and occasionally need to remember what they agreed to do. Built by a human and his AI assistant using Claude Code — this isn't a corporate wiki, it's a passion project exploring what's possible when AI helps you ship.

## What Can It Do?

### Notes & Tasks
- **Rich Text Notes** with TipTap editor, collapsible sections, and tag organization
- **Checkbox-Task Sync** — check a box in your note, it becomes a task on the board; complete the task, the checkbox is done
- **Kanban Board** with three-state workflow (Todo → In Progress → Done), drag-and-drop, inline creation
- **Due Dates** with visual urgency (overdue, today, tomorrow, this week)
- **Task Comments** and **Task Tags** for organization
- **Tag Hub** — unified view of everything tagged, with searchable selector, AI chat, and conversational Q&A

### Meetings & AI
- **Meeting Capture** with daily grouping and fire-and-forget workflow
- **Live Transcription** via Deepgram Nova-3 during browser recording
- **Speaker Identification** with multichannel audio separation and diarization
- **AI Analysis** powered by Claude for summaries, key points, decisions, and action items
- **Self-Reflection Prompts** with blind spot insights comparing your self-assessment to AI analysis
- **Behavioral Insights Dashboard** tracking meeting patterns over time
- **Meeting Notes** with embedded TipTap editor, auto-save, and tag sync

### Integrations
- **Google OAuth** for secure authentication
- **Google Calendar Sync** to import upcoming events as meetings
- **Screenshot Import** — paste a calendar screenshot, Claude Vision extracts and imports meetings
- **Jira Integration** — paste issue URLs in notes to render rich inline chips with status badges
- **MCP Server** at `/mcp` for OpenClaw and other MCP clients, with personal API key auth and full read/write access

### Power Features
- **Multi-Profile Support** — separate data contexts (Work, Personal) with sidebar switcher and account linking
- **Outstanding Action Items** widget on home page showing incomplete meeting action items from the last 30 days

## Tech Stack

- **.NET 10** + **Angular 21** + **PrimeNG** + **Tailwind CSS**
- **PostgreSQL** via EF Core
- **xUnit** + **Playwright** for testing
- **Domain-Driven Design** architecture

## Quick Start

**Prerequisites:** Docker

```bash
docker compose --profile dev-stack up
```

Open http://localhost:4200. Use the mock auth toolbar at the bottom to log in (no Google OAuth setup required).

**Optional API keys:**
- **Anthropic** (AI meeting analysis): `dotnet user-secrets set "MeetingAnalysis:ApiKey" "sk-ant-..." --project src/PraxisNote.Web`
- **Deepgram** (live transcription): `dotnet user-secrets set "Deepgram:ApiKey" "your-key" --project src/PraxisNote.Web`
- **Google OAuth** (calendar sync): The dev Docker stack uses mock Google credentials by default. To enable real OAuth/calendar sync, update the `Authentication__Google__ClientId` and `Authentication__Google__ClientSecret` values for the `api` service in `docker-compose.yml` (or a compose override/env file)

Without these keys, the app runs normally — you just won't get AI analysis, live transcripts, or calendar sync.

To stop:
```bash
docker compose --profile dev-stack down
```

## Run Tests

```bash
# Unit tests
dotnet test src/PraxisNote.slnx

# E2E tests
docker compose --profile e2e up -d --wait
cd tests/PraxisNote.E2E.Tests && npm test
docker compose --profile e2e down
```

## Learn More

- **[CONTRIBUTING.md](CONTRIBUTING.md)** — Dev workflow, architecture, CI/CD, and how AI agents contribute
- **[CLAUDE.md](CLAUDE.md)** — Project preferences and coding conventions for AI agents
- **[User Docs](https://praxisnote-docs.vercel.app/)** — Feature guides and keyboard shortcuts

## License

MIT
