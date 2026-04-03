## 1. Backend — Domain Layer

- [x] 1.1 Remove `BehavioralAnalysis`, `ExcludeFromInsights`, `ReflectionData`, `ReflectionSubmittedAt` properties from `Meeting.cs`
- [x] 1.2 Remove `HasReflection` computed property from `Meeting.cs`
- [x] 1.3 Remove `SubmitReflection()` method from `Meeting.cs`
- [x] 1.4 Remove `SetExcludeFromInsights()` method from `Meeting.cs`
- [x] 1.5 Remove `behavioralAnalysis` parameter from `CompleteAnalysis()` and its storage logic in `Meeting.cs`
- [x] 1.6 Remove behavioral analysis clearing from `ClearAnalysis()` in `Meeting.cs`
- [x] 1.7 Remove reflection and behavioral tests from `MeetingTests.cs`

## 2. Backend — Application Layer

- [x] 2.1 Delete `GenerateReflectionPrompts.cs` handler
- [x] 2.2 Delete `SubmitReflection.cs` handler
- [x] 2.3 Delete `GetMeetingReflection.cs` handler
- [x] 2.4 Delete `UpdateMeetingExcludeFromInsights.cs` handler
- [x] 2.5 Remove `BehavioralAnalysis`, `ExcludeFromInsights`, `ReflectionData`, `ReflectionSubmittedAt` from `MeetingDto.cs` and delete related DTOs (`ReflectionPromptDto`, `ReflectionDto`, `PromptResponseDto`)
- [x] 2.6 Remove behavioral/reflection fields from DTO mapping in `GetMeetingById.cs`
- [x] 2.7 Remove `BehavioralAnalysisData` and related type definitions from `IMeetingAnalyzer.cs`
- [x] 2.8 Remove behavioral analysis section from AI prompt and response handling in `AnthropicMeetingAnalyzer.cs`
- [x] 2.9 Remove behavioral analysis serialization from `AnalyzeMeeting.cs`
- [x] 2.10 Delete all Insights use cases, Goals, Nudges, domain aggregates, repositories, configurations, MCP tools
- [x] 2.11 Remove DI registrations for deleted handlers from `DependencyInjection.cs` (Application + Infrastructure)
- [x] 2.12 Delete reflection and insights application tests, update account linking tests

## 3. Backend — Infrastructure & Web Layer

- [x] 3.1 Remove `BehavioralAnalysis`, `ExcludeFromInsights`, `ReflectionData`, `ReflectionSubmittedAt` from `MeetingConfiguration.cs`
- [x] 3.2 Remove reflection endpoints (3 routes) from `MeetingEndpoints.cs` and their handler methods + request records
- [x] 3.3 Remove ExcludeFromInsights endpoint from `MeetingEndpoints.cs` and its handler method + request record
- [x] 3.4 Delete `InsightEndpoints.cs` entirely
- [x] 3.5 Remove InsightEndpoints registration from `Program.cs`

## 4. Database Migration

- [x] 4.1 Create EF migration to drop columns + BehavioralGoals/BlindSpotNudges tables
- [ ] 4.2 Create data migration to remove the self-reflection feature notification row

## 5. Frontend — Remove Insights Feature

- [x] 5.1 Delete the entire `insights/` directory (16 files)
- [x] 5.2 Remove `/insights` route from `app.routes.ts`
- [x] 5.3 Remove Insights nav link from `sidebar.component.ts`
- [x] 5.4 Delete home page insights widget, service, and model
- [x] 5.5 Remove insights widget usage from home page component

## 6. Frontend — Simplify Meetings

- [x] 6.1 Delete `meeting-reflection.component.ts`
- [x] 6.2 Delete `meeting-behavioral-analysis.component.ts`
- [x] 6.3 Remove behavioral analysis types and interfaces from `meeting.model.ts`
- [x] 6.4 Remove reflection methods and `toggleExcludeFromInsights` from `meeting.service.ts`
- [x] 6.5 Remove reflection section, behavioral analysis conditional, and ExcludeFromInsights toggle from `meeting-editor.page.ts`
- [x] 6.6 Remove behavioral analysis conditional rendering from `meeting-analysis.component.ts`
- [x] 6.7 Remove ExcludeFromInsights toggle from `meeting-details-section.component.ts`
- [x] 6.8 Remove `reflectionData`, `reflectionSubmittedAt`, `excludeFromInsights`, `behavioralAnalysis` from Meeting interface in `meeting.model.ts`

## 7. Verification

- [x] 7.1 Run backend tests: 1,058 passed (689 domain + 357 application + 12 integration)
- [x] 7.2 Run frontend build: successful
- [x] 7.3 Verify no orphaned imports or references to deleted code
