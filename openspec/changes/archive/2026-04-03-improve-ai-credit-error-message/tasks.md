## 1. Domain Exception

- [x] 1.1 Add `AiInsufficientCreditsException` to `AiProviderException.cs` following the same pattern as `AiKeyInvalidException` (Provider property, descriptive message)

## 2. Anthropic Error Detection

- [x] 2.1 Add catch clause in `AnthropicTagAiChatService.StreamResponseAsync` (both pre-stream and enumeration catch blocks) to detect "credit balance" in `HttpRequestException.Message` and throw `AiInsufficientCreditsException`
- [x] 2.2 Add catch clause in `AnthropicMeetingAnalyzer` to detect "credit balance" in `HttpRequestException.Message` and throw `AiInsufficientCreditsException`

## 3. OpenAI Error Detection

- [x] 3.1 Add catch clause in `OpenAiTagAiChatService` (both pre-stream and enumeration catch blocks) before the existing 429 catch to detect "insufficient_quota" in `ClientResultException.Message` and throw `AiInsufficientCreditsException`
- [x] 3.2 Add catch clause in `OpenAiMeetingAnalyzer` before the existing 429 catch to detect "insufficient_quota" and throw `AiInsufficientCreditsException`

## 4. Gemini Error Detection

- [x] 4.1 Add catch clause in `GeminiTagAiChatService` before the existing 429 catch to detect "quota" or "RESOURCE_EXHAUSTED" in response body and throw `AiInsufficientCreditsException`
- [x] 4.2 Add catch clause in `GeminiMeetingAnalyzer` before the existing 429 catch to detect "quota" or "RESOURCE_EXHAUSTED" and throw `AiInsufficientCreditsException`

## 5. ValidateAiKey Handler

- [x] 5.1 Add `InsufficientCredits` field to `ValidateAiKey.Result` record (default `false`)
- [x] 5.2 Add catch clause for `AiInsufficientCreditsException` returning `new Result(false, InsufficientCredits: true)`

## 6. API Endpoint

- [x] 6.1 Update the validate-key endpoint to return a distinct message when `InsufficientCredits` is true

## 7. Tests

- [x] 7.1 Add unit tests for `AiInsufficientCreditsException` construction
- [x] 7.2 Add/update `ValidateAiKey` tests to cover the `AiInsufficientCreditsException` catch path
