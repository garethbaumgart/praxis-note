export interface InsightsSummary {
  meetingCount: number;
  participantName: string;
  headline: InsightsHeadlineMetric;
  questionRatio: InsightsSecondaryMetric;
  redFlags: InsightsSecondaryMetric;
  nudgeText: string | null;
  sparklineValues: number[];
}

export interface InsightsHeadlineMetric {
  label: string;
  value: number;
  change: number;
  unit: string;
}

export interface InsightsSecondaryMetric {
  label: string;
  value: number;
  change: number;
}
