export type MeetingStatus = 'Draft' | 'Processing' | 'Ready' | 'Reviewed' | 'Failed';

export interface Meeting {
  id: string;
  title: string | null;
  meetingDate: string | null;
  attendees: string | null;
  status: MeetingStatus;
  createdAt: string;
  updatedAt: string;
}

export interface MeetingGroup {
  label: string;
  subLabel: string;
  meetings: Meeting[];
}
