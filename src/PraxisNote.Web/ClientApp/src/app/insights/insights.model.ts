export interface BehavioralTrends {
  participantName: string;
  availableParticipants: string[];
  meetingCount: number;
  summary: TrendSummary;
  talkTimeTrend: TrendSeries;
  questionRatioTrend: TrendSeries;
  interruptionTrend: TrendSeries;
  sentimentTrend: TrendSeries;
  redFlagTrend: RedFlagTrend;
  engagementTrend: TrendSeries;
}

export interface TrendSummary {
  averageTalkTimePercent: number;
  talkTimeChange: number;
  averageQuestionRatio: number;
  questionRatioChange: number;
  averageInterruptionCount: number;
  interruptionChange: number;
  averageSentimentScore: number;
  sentimentChange: number;
  totalRedFlags: number;
  redFlagChange: number;
  dominantEngagementLevel: string;
}

export interface TrendDataPoint {
  date: string;
  value: number;
  label?: string;
}

export interface TrendSeries {
  dataPoints: TrendDataPoint[];
}

export interface RedFlagTrend {
  totalByMeeting: TrendDataPoint[];
  byType: Record<string, TrendDataPoint[]>;
}

export type DateRange = '7d' | '30d' | '90d' | 'all';
