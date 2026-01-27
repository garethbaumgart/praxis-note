# Meeting Notes Feature - Vision & Roadmap

## Overview

Meeting Notes is a comprehensive feature for capturing, transcribing, analyzing, and acting on meetings. The goal is to reduce manual note-taking and automatically surface action items.

**Key Design Principle:** Optimized for back-to-back meetings. Capture fast, process in background, review later.

---

## UX Decisions (Confirmed)

### List View: Daily Grouped List
Meetings are grouped by day with sticky date headers. Time is prominently displayed. Clear distinction between:
- Recording (active)
- Processing (transcribing/analyzing)
- Ready (needs review)
- Reviewed (done)

See: `mockups/meeting-list-ux-options.html` (Option 1)

### Workflow: Fire & Forget
Designed for back-to-back meetings where you end one call and immediately hop on the next.

**Capture:**
- One-click record (big button, always visible)
- Title is optional - can add later
- Stop recording → immediately move on to next meeting

**Processing (async, ~2-3 min):**
1. Audio uploads in chunks during recording
2. Whisper transcribes audio → text
3. Claude analyzes → Summary, Key Points, Decisions, Action Items
4. **Audio file deleted** (no storage)
5. Meeting marked "Ready for review"

**Review (when you have time):**
- Badge shows unreviewed count
- Add/edit title, attendees
- Review summary, key points, decisions
- Create tasks from action items
- Mark as reviewed

See: `mockups/meeting-workflow-back-to-back.html`

### No Audio Storage
Audio is ephemeral:
- Chunked upload during recording (prevents data loss)
- Transcribed immediately after recording stops
- Audio file deleted after successful transcription
- Only transcript and analysis are stored

---

## Current Issues

| Issue | Title | Description |
|-------|-------|-------------|
| #228 | Meeting CRUD | Basic meeting CRUD (title, date, attendees, status) |
| #229 | Paste Transcript | Add transcript textarea, store raw text |
| #230 | AI Analysis | Claude generates summary, key points, decisions |
| #231 | Action Item Extraction | Claude extracts structured action items |
| #232 | Promote Actions to Tasks | Convert action items to kanban tasks, bidirectional sync |
| #233 | Audio Upload | Upload audio files (temporary storage for processing) |
| #234 | Whisper Transcription | Transcribe audio, delete after |
| #235 | Browser Recording | Record in-app with microphone |
| #236 | Calendar Integration | Google/Outlook sync, auto-create meetings |
| #237 | AI Attendee Extraction | Infer attendees from transcript |
| #238 | Daily Summary | Aggregate meetings, tasks, notes into daily view |
| #240 | Meeting Tags | Add tags to meetings (same tag system as tasks) |
| #242 | Unified Tag Search | Search across Tasks, Notes, Meetings by tag |
| #243 | Behavioral Analysis | AI analysis of communication patterns, sentiment, red flags |

---

## User Workflow

```
┌─────────────────────────────────────────────────────────────────┐
│                      BACK-TO-BACK WORKFLOW                      │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  CAPTURE (during meeting)                                       │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │ [🔴 Record] Sprint Planning (optional title)            │   │
│  │                                                         │   │
│  │ ← One click to start, one click to stop. Move on.       │   │
│  └─────────────────────────────────────────────────────────┘   │
│                              ↓                                  │
│  PROCESS (background, ~2-3 min)                                 │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │ Upload → Transcribe → Analyze → Delete Audio            │   │
│  │                                                         │   │
│  │ You're already in your next meeting. Don't wait.        │   │
│  └─────────────────────────────────────────────────────────┘   │
│                              ↓                                  │
│  REVIEW (when free)                                             │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │ • Edit title, add attendees                             │   │
│  │ • Review summary, key points, decisions                 │   │
│  │ • Create tasks from action items                        │   │
│  │ • Mark as reviewed                                      │   │
│  └─────────────────────────────────────────────────────────┘   │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

---

## Meeting States

| Status | Meaning | Badge Color |
|--------|---------|-------------|
| `Recording` | Currently being recorded | Orange (pulsing) |
| `Processing` | Transcribing or analyzing | Orange |
| `Ready` | Processed, needs review | Blue |
| `Reviewed` | User has reviewed | Green |
| `Failed` | Processing error | Red |

---

## Data Model

```
Meeting
├── Id (Guid)
├── UserId (Guid)
├── Title (string, nullable until reviewed)
├── MeetingDate (DateTimeOffset?)
├── Attendees (string?, nullable)
├── TranscriptContent (string)
├── Summary (string?, AI-generated)
├── KeyPoints (string?, JSON array)
├── Decisions (string?, JSON array)
├── ActionItems (JSONB, structured list)
├── Status (enum: Recording, Processing, Ready, Reviewed, Failed)
├── IsReviewed (bool)
├── CreatedAt (DateTimeOffset)
├── UpdatedAt (DateTimeOffset)
└── Duration (TimeSpan?)
```

**Note:** No `AudioFilePath` - audio is not stored.

---

## Processing Pipeline

```
Recording Stop
      ↓
┌─────────────────────────────┐
│ 1. Upload audio chunks      │  (already uploaded during recording)
└─────────────────────────────┘
      ↓
┌─────────────────────────────┐
│ 2. Whisper Transcription    │  ~$0.006/min (~$0.36/hour)
│    - Send to OpenAI API     │
│    - Receive transcript     │
└─────────────────────────────┘
      ↓
┌─────────────────────────────┐
│ 3. Delete Audio File        │  Immediately after transcription
└─────────────────────────────┘
      ↓
┌─────────────────────────────┐
│ 4. Claude Analysis          │  ~$0.02/meeting
│    - Summary                │
│    - Key Points             │
│    - Decisions              │
│    - Action Items           │
│    - Suggested Attendees    │
└─────────────────────────────┘
      ↓
┌─────────────────────────────┐
│ 5. Mark Ready               │  Notify user (badge, notification)
└─────────────────────────────┘
```

**Total processing time:** ~2-3 minutes for a 1-hour meeting

---

## Notification Options

When a meeting finishes processing:

1. **In-App Badge:** Count on Meetings nav item showing unreviewed meetings
2. **Browser Notification:** "Sprint Planning ready - 3 action items" (optional)

---

## Future Enhancements

### Calendar Integration (#236)
- Connect Google Calendar / Outlook
- Auto-create meeting when event starts
- Pre-fill title and attendees from calendar
- Notification: "Sprint Planning starting - Record?"

### Daily Summary (#238)
- **On-demand** - Generate whenever user wants, can request multiple times
- Aggregate all meetings, tasks, notes for the day (or selected date)
- Highlight outstanding action items across all meetings
- AI-generated narrative summary
- Can regenerate to get updated view as more meetings complete
- Not scheduled/automatic - user controls when to generate

### AI Attendee Extraction (#237)
- Claude extracts names from transcript
- "John said...", speaker labels, introductions
- Suggest attendees for user to confirm

### Meeting Tags (#240)
- Add tags to meetings (same tag system as tasks)
- Filter meetings by tag
- Reuse existing tag picker component

### Unified Tag Search (#242)
- Search across Tasks, Notes, and Meetings by tag
- Tags correlate content across all features
- Single search brings together everything related to a topic/project
- See also: Notes Tag Support (#220)

### Behavioral Analysis (#243)
AI-powered analysis of meeting transcripts to surface communication insights. Runs post-meeting using the transcript (not real-time).

**Goals:**
- **Self-improvement** - Understand your own communication patterns (talk time, interruptions, clarity)
- **Team Dynamics** - Understand group interaction patterns (who dominates, are all voices heard)
- **Relationship Coaching** - Improve working relationships (friction points, conversation improvements)
- **Red Flag Detection** - Identify concerning patterns (passive-aggressive language, commitment avoidance, evasion)

**Analysis Dimensions:**
- Speaking dynamics (talk time, interruptions, question vs statement ratio)
- Sentiment & tone (per-participant, shifts during meeting)
- Communication patterns (clarity, follow-up, engagement)
- Red flags when detected (with appropriate caveats)

**Privacy:**
- Opt-in per meeting or globally
- Clear disclaimers about AI interpretation limitations
- For self-improvement, not surveillance

---

## Cost Estimates

| Service | Cost | Per |
|---------|------|-----|
| Whisper transcription | $0.006/min | ~$0.36/hour meeting |
| Claude analysis | ~$0.02 | per meeting |
| Storage (transcript only) | ~$0.001/meeting | per month |

**Typical daily cost (5 meetings, 1hr each):** ~$2.00

**No audio storage cost** - audio is deleted after transcription.

---

## Implementation Order

1. **#228 - Meeting CRUD** ← Start here
2. **#229 - Paste Transcript**
3. **#230 - AI Analysis**
4. **#231 - Action Item Extraction**
5. **#232 - Promote to Tasks**
6. **#233 - Audio Upload**
7. **#234 - Whisper Transcription**
8. **#235 - Browser Recording**
9. **#236 - Calendar Integration**
10. **#237 - AI Attendee Extraction**
11. **#238 - Daily Summary**
12. **#243 - Behavioral Analysis**
