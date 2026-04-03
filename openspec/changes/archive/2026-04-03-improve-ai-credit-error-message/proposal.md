## Why

When an AI provider rejects a request due to insufficient credits/quota, the error is misreported to the user. Anthropic's "credit balance too low" (HTTP 400) is shown as "Could not reach Anthropic." OpenAI and Gemini return insufficient quota as HTTP 429, which we currently report as "rate limited" — telling users their key is valid when their account is actually empty.

## What Changes

- Add a new `AiInsufficientCreditsException` domain exception for billing/credit errors
- **Anthropic**: Detect `"credit balance is too low"` in the `HttpRequestException.Message` (HTTP 400, no status code from SDK) and throw `AiInsufficientCreditsException` instead of generic provider error
- **OpenAI**: Detect `"insufficient_quota"` in the `ClientResultException.Message` (HTTP 429) and throw `AiInsufficientCreditsException` instead of rate-limited
- **Gemini**: Detect `"quota"` or billing-related keywords in `HttpRequestException.Message` (HTTP 429) and throw `AiInsufficientCreditsException` instead of rate-limited
- Handle the new exception in `ValidateAiKey` so validation returns a clear "insufficient credits" result
- Surface a user-friendly message in the API response/UI

## Capabilities

### New Capabilities
- `ai-insufficient-credits`: Detect and surface provider billing/credit errors distinctly from auth failures, rate limits, and network errors across all three AI providers (Anthropic, OpenAI, Gemini)

### Modified Capabilities
_(none)_

## Impact

- `src/PraxisNote.Application/Features/UserAiKeys/AiProviderException.cs` — new `AiInsufficientCreditsException` class
- `src/PraxisNote.Application/Features/UserAiKeys/ValidateAiKey.cs` — new catch clause + result field
- `src/PraxisNote.Infrastructure/External/AnthropicTagAiChatService.cs` — detect credit error in catch chain
- `src/PraxisNote.Infrastructure/External/AnthropicMeetingAnalyzer.cs` — detect credit error in catch chain
- `src/PraxisNote.Infrastructure/External/OpenAiTagAiChatService.cs` — detect credit error (429 + insufficient_quota)
- `src/PraxisNote.Infrastructure/External/OpenAiMeetingAnalyzer.cs` — detect credit error
- `src/PraxisNote.Infrastructure/External/GeminiTagAiChatService.cs` — detect credit error (429 + quota keywords)
- `src/PraxisNote.Infrastructure/External/GeminiMeetingAnalyzer.cs` — detect credit error
- `tests/` — unit tests for the new exception paths
