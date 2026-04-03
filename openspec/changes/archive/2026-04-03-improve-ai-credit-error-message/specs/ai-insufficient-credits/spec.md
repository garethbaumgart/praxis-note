## ADDED Requirements

### Requirement: Insufficient credits exception

The system SHALL provide an `AiInsufficientCreditsException` domain exception that is distinct from `AiKeyInvalidException`, `AiRateLimitedException`, and `AiProviderException`.

#### Scenario: Exception carries provider name

- **WHEN** an `AiInsufficientCreditsException` is constructed with provider name "Anthropic"
- **THEN** the `Provider` property SHALL equal "Anthropic"
- **AND** the `Message` SHALL contain "insufficient credits"

### Requirement: Anthropic credit error detection

The system SHALL detect Anthropic's "credit balance is too low" error and throw `AiInsufficientCreditsException` instead of `AiProviderException`.

#### Scenario: Anthropic returns credit balance error during streaming

- **WHEN** the Anthropic SDK throws `HttpRequestException` with message containing "credit balance is too low"
- **THEN** the system SHALL throw `AiInsufficientCreditsException` with provider "Anthropic"
- **AND** the system SHALL NOT throw `AiProviderException` with "Could not reach Anthropic"

#### Scenario: Anthropic returns credit balance error during meeting analysis

- **WHEN** the Anthropic API returns HTTP 400 with message containing "credit balance is too low" during meeting analysis
- **THEN** the system SHALL throw `AiInsufficientCreditsException` with provider "Anthropic"

### Requirement: OpenAI credit error detection

The system SHALL detect OpenAI's "insufficient_quota" error and throw `AiInsufficientCreditsException` instead of `AiRateLimitedException`.

#### Scenario: OpenAI returns insufficient quota during streaming

- **WHEN** the OpenAI SDK throws `ClientResultException` with status 429 and message containing "insufficient_quota"
- **THEN** the system SHALL throw `AiInsufficientCreditsException` with provider "OpenAI"
- **AND** the system SHALL NOT throw `AiRateLimitedException`

#### Scenario: OpenAI returns insufficient quota during meeting analysis

- **WHEN** the OpenAI SDK throws `ClientResultException` with status 429 and message containing "insufficient_quota"
- **THEN** the system SHALL throw `AiInsufficientCreditsException` with provider "OpenAI"

### Requirement: Gemini credit error detection

The system SHALL detect Gemini's quota exhaustion error and throw `AiInsufficientCreditsException` instead of `AiRateLimitedException`.

#### Scenario: Gemini returns quota exhaustion during streaming

- **WHEN** the Gemini API returns HTTP 429 with response body containing "quota" or "RESOURCE_EXHAUSTED"
- **THEN** the system SHALL throw `AiInsufficientCreditsException` with provider "Gemini"
- **AND** the system SHALL NOT throw `AiRateLimitedException`

#### Scenario: Gemini returns quota exhaustion during meeting analysis

- **WHEN** the Gemini API returns HTTP 429 with response body containing "quota" or "RESOURCE_EXHAUSTED"
- **THEN** the system SHALL throw `AiInsufficientCreditsException` with provider "Gemini"

### Requirement: ValidateAiKey handles insufficient credits

The `ValidateAiKey` handler SHALL catch `AiInsufficientCreditsException` and return a result that distinguishes it from invalid keys and rate limits.

#### Scenario: Validation catches insufficient credits

- **WHEN** key validation triggers `AiInsufficientCreditsException`
- **THEN** the result SHALL have `Validated = false`
- **AND** the result SHALL have `InsufficientCredits = true`

#### Scenario: Validation does not confuse credits with invalid key

- **WHEN** key validation triggers `AiKeyInvalidException`
- **THEN** the result SHALL have `Validated = false`
- **AND** the result SHALL have `InsufficientCredits = false`

### Requirement: API surfaces insufficient credits to frontend

The API endpoint for key validation SHALL return a distinct error message when the key is valid but has insufficient credits.

#### Scenario: Endpoint returns insufficient credits response

- **WHEN** the validation result has `InsufficientCredits = true`
- **THEN** the API SHALL return HTTP 422 with a message indicating the key is valid but the account has insufficient credits
