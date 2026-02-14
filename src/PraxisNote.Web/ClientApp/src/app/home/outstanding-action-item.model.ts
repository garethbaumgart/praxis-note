export interface OutstandingActionItem {
  actionItemId: string;
  description: string;
  assignee: string | null;
  meetingId: string;
  meetingTitle: string | null;
  meetingDate: string | null;
  isLinkedToTask: boolean;
  linkedTaskId: string | null;
  linkedTaskStatus: string | null;
}
