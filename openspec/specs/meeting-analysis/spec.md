# Meeting Analysis

## Purpose

Defines the AI-powered meeting analysis capability — what the system extracts from meeting transcripts and how analysis results are stored and exposed.

## Requirements

### Requirement: AI analysis output
The system SHALL generate meeting analysis containing summary, key points, decisions, and action items. The system SHALL NOT generate behavioral analysis (speaking dynamics, sentiment/tone, communication patterns, red flags).

#### Scenario: Successful analysis
- **WHEN** a user triggers AI analysis on a meeting with a transcript
- **THEN** the system returns summary, key points, decisions, and action items
- **AND** no behavioral analysis data is included in the response

#### Scenario: Analysis completion stored on meeting
- **WHEN** AI analysis completes successfully
- **THEN** the meeting stores summary, keyPoints, decisions, suggestedTags, and actionItems
- **AND** no behavioralAnalysis property exists on the meeting

### Requirement: Meeting domain model
The Meeting aggregate SHALL NOT contain properties or methods for behavioral analysis, reflection, or insights exclusion.

#### Scenario: Meeting creation
- **WHEN** a meeting is created
- **THEN** it has no behavioral analysis, reflection, or insights exclusion properties

#### Scenario: Analysis completion parameters
- **WHEN** `CompleteAnalysis()` is called
- **THEN** it accepts summary, keyPoints, decisions, suggestedTags, and actionItems
- **AND** it does not accept a behavioralAnalysis parameter

### Requirement: Meeting DTO
The MeetingDto SHALL NOT contain fields for behavioral analysis, reflection data, reflection timestamp, or insights exclusion.

#### Scenario: Meeting serialization
- **WHEN** a meeting is serialized to DTO
- **THEN** the DTO contains core fields (id, title, date, attendees, status, summary, keyPoints, decisions, suggestedTags, actionItems, tags, noteId, timestamps)
- **AND** no behavioral analysis, reflection, or insights exclusion fields are present
