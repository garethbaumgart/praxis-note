## Context

The app supports three AI providers (Anthropic, OpenAI, Gemini). Each has catch chains in both their `*TagAiChatService` and `*MeetingAnalyzer` classes that translate provider-specific exceptions into domain exceptions (`AiKeyInvalidException`, `AiRateLimitedException`, `AiProviderException`).

Currently there is no domain exception for "key is valid but account has no credits." Each provider returns this condition differently:

| Provider | Status | Detection signal | Current (wrong) mapping |
|----------|--------|-----------------|------------------------|
| Anthropic | 400 | `"credit balance is too low"` in message | `AiProviderException` ("Could not reach") |
| OpenAI | 429 | `"insufficient_quota"` in message | `AiRateLimitedException` |
| Gemini | 429 | `"quota"` / `"RESOURCE_EXHAUSTED"` in body | `AiRateLimitedException` |

## Goals / Non-Goals

**Goals:**
- Users see a clear "insufficient credits" message instead of misleading network/rate-limit errors
- All three providers are handled consistently
- `ValidateAiKey` distinguishes "no credits" from "bad key" so the UI can guide users to their billing page

**Non-Goals:**
- Deep-linking to each provider's billing page (nice-to-have, not this change)
- Distinguishing rate limits from quota exhaustion on Gemini (both are 429 + RESOURCE_EXHAUSTED — we'll treat all Gemini 429s with quota keywords as insufficient credits, since genuine rate limits are transient and retrying will clarify)
- Changing the frontend UI beyond surfacing the new result field

## Decisions

### Decision 1: Message-sniffing in catch guards

Detect insufficient credits by inspecting `ex.Message` (or `ex.InnerException.Message`) for known substrings. This is fragile if providers change their error messages, but:
- Anthropic gives us no status code distinction (400 for both bad request and no credits)
- OpenAI uses 429 for both rate limits and quota — only the message body differs
- The alternative (parsing JSON error response bodies) would require catching the response before the SDK processes it, which is far more invasive

**Mitigation:** Use broad substring matches (`"credit balance"`, `"insufficient_quota"`, `"quota"`) and log the full exception so we can update if providers change wording.

### Decision 2: Catch order — credits before rate limits

For OpenAI and Gemini, the insufficient-credits catch clause MUST appear **before** the existing rate-limit catch, since both share HTTP 429. The `when` guard on the credits clause will check the message substring; if it doesn't match, execution falls through to the rate-limit clause.

```
catch (ClientResultException ex) when (ex.Status == 429 && IsInsufficientQuota(ex))
    → AiInsufficientCreditsException

catch (ClientResultException ex) when (ex.Status == 429)
    → AiRateLimitedException  (genuine rate limit)
```

### Decision 3: New exception lives in Application layer

`AiInsufficientCreditsException` follows the same pattern as `AiKeyInvalidException` — it lives in `src/PraxisNote.Application/Features/UserAiKeys/AiProviderException.cs` alongside the other AI domain exceptions.

### Decision 4: ValidateAiKey result gets `InsufficientCredits` field

Add `bool InsufficientCredits = false` to the existing `Result` record. The endpoint already returns the result — the frontend can read the new field to show a targeted message. This is backward-compatible (defaults to `false`).

## Risks / Trade-offs

- **Message strings may change** → Mitigation: Use broad substrings, log full exceptions, add tests that document expected messages so breakage is caught early.
- **Gemini ambiguity** → Gemini uses `RESOURCE_EXHAUSTED` for both rate limits and billing quota. We'll treat 429 + quota keywords as insufficient credits. If it's actually a transient rate limit, the user retries and it works — acceptable UX.
- **Six files touched** → The same pattern is repeated across `*TagAiChatService` and `*MeetingAnalyzer` for each provider. This is mechanical but needs care to get catch ordering right in all six files.

## Open Questions

_(none — all resolved during exploration)_
