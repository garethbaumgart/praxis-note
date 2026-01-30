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

export type MetricType =
  | 'TalkTimePercentage'
  | 'QuestionRatio'
  | 'InterruptionCount'
  | 'SentimentScore'
  | 'RedFlagCount';

export type GoalOperator =
  | 'LessThan'
  | 'LessThanOrEqual'
  | 'GreaterThan'
  | 'GreaterThanOrEqual'
  | 'Between';

export interface BehavioralGoal {
  id: string;
  metricType: string;
  operator: string;
  targetValue: number;
  targetValueUpper: number | null;
  title: string;
  isActive: boolean;
  createdAt: string;
}

export interface GoalProgress {
  goalId: string;
  title: string;
  metricType: string;
  operator: string;
  targetValue: number;
  targetValueUpper: number | null;
  isActive: boolean;
  currentValue: number | null;
  isMet: boolean;
  streak: number;
  meetingsEvaluated: number;
  recentResults: boolean[];
}

export interface GoalPreset {
  title: string;
  metricType: MetricType;
  operator: GoalOperator;
  targetValue: number;
  targetValueUpper: number | null;
  description: string;
}

export const GOAL_PRESETS: GoalPreset[] = [
  {
    title: 'Keep talk time under 50%',
    metricType: 'TalkTimePercentage',
    operator: 'LessThan',
    targetValue: 50,
    targetValueUpper: null,
    description: 'Leave room for others to contribute',
  },
  {
    title: 'Ask more questions',
    metricType: 'QuestionRatio',
    operator: 'GreaterThanOrEqual',
    targetValue: 0.3,
    targetValueUpper: null,
    description: 'Aim for at least 30% questions vs statements',
  },
  {
    title: 'Zero red flags',
    metricType: 'RedFlagCount',
    operator: 'LessThanOrEqual',
    targetValue: 0,
    targetValueUpper: null,
    description: 'No evasive, hedging, or defensive language',
  },
  {
    title: 'Stay positive',
    metricType: 'SentimentScore',
    operator: 'GreaterThanOrEqual',
    targetValue: 0.6,
    targetValueUpper: null,
    description: 'Maintain a constructive, positive tone',
  },
  {
    title: 'Limit interruptions',
    metricType: 'InterruptionCount',
    operator: 'LessThanOrEqual',
    targetValue: 2,
    targetValueUpper: null,
    description: 'Interrupt others no more than twice per meeting',
  },
];
