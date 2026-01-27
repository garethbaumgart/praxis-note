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
  createdAt: string;
  updatedAt: string;
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

export interface MeetingGroup {
  label: string;
  subLabel: string;
  meetings: Meeting[];
}
