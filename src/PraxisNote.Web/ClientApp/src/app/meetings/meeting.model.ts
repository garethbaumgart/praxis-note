export type MeetingStatus = 'Draft' | 'Processing' | 'Ready' | 'Reviewed' | 'Failed';

export interface Meeting {
  id: string;
  title: string | null;
  meetingDate: string | null;
  attendees: string | null;
  transcriptContent: string | null;
  status: MeetingStatus;
  summary: string | null;
  keyPoints: string | null;
  decisions: string | null;
  behavioralAnalysis: string | null;
  createdAt: string;
  updatedAt: string;
}

// Behavioral Analysis Types
export interface BehavioralAnalysis {
  speakingDynamics: SpeakingDynamics;
  sentimentTone: SentimentTone;
  communicationPatterns: CommunicationPatterns;
  redFlags: RedFlag[];
}

export interface SpeakingDynamics {
  talkTimeByParticipant: ParticipantTalkTime[];
  interruptionPatterns: InterruptionPattern[];
  questionVsStatementRatio: Record<string, number>;
}

export interface ParticipantTalkTime {
  participant: string;
  percentage: number;
  duration: string;
}

export interface InterruptionPattern {
  interrupter: string;
  interrupted: string;
  count: number;
}

export interface SentimentTone {
  participantSentiments: ParticipantSentiment[];
  toneShifts: ToneShift[];
  emotionalIndicators: string[];
}

export interface ParticipantSentiment {
  participant: string;
  sentiment: 'positive' | 'neutral' | 'negative';
  score: number;
}

export interface ToneShift {
  timestamp: string;
  description: string;
  from: string;
  to: string;
}

export interface CommunicationPatterns {
  overallClarity: number;
  followUpPatterns: FollowUpPattern[];
  engagementLevels: ParticipantEngagement[];
}

export interface FollowUpPattern {
  topic: string;
  wasFollowedUp: boolean;
  assignedTo: string | null;
}

export interface ParticipantEngagement {
  participant: string;
  level: 'high' | 'medium' | 'low';
  indicators: string[];
}

export interface RedFlag {
  type: 'evasive' | 'hedging' | 'defensive' | 'inconsistent';
  participant: string;
  description: string;
  context: string;
  severity: 'low' | 'medium' | 'high';
}

export function parseJsonArray(json: string | null): string[] {
  if (!json) return [];
  try {
    const parsed = JSON.parse(json);
    if (!Array.isArray(parsed)) return [];
    return parsed.filter((item): item is string => typeof item === 'string');
  } catch {
    return [];
  }
}

export function parseBehavioralAnalysis(json: string | null): BehavioralAnalysis | null {
  if (!json) return null;
  try {
    const parsed = JSON.parse(json) as BehavioralAnalysis;
    // Basic validation - check if it has the expected structure
    if (parsed && typeof parsed === 'object' && 'speakingDynamics' in parsed) {
      return parsed;
    }
    return null;
  } catch {
    return null;
  }
}

export interface MeetingGroup {
  label: string;
  subLabel: string;
  meetings: Meeting[];
}
