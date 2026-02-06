export interface DailySummary {
  date: string;
  stats: DailySummaryStats;
  meetings: MeetingSummaryItem[];
  outstandingActionItems: OutstandingActionItem[];
  completedTasks: CompletedTaskItem[];
  inProgressTasks: InProgressTaskItem[];
  notesUpdated: NoteActivityItem[];
}

export interface DailySummaryStats {
  meetingCount: number;
  tasksCompleted: number;
  tasksStarted: number;
  actionItemsOpen: number;
  notesUpdated: number;
}

export interface MeetingSummaryItem {
  id: string;
  title: string | null;
  meetingDate: string | null;
  attendees: string | null;
  status: string;
  summary: string | null;
  actionItemCount: number;
  decisionCount: number;
  completedActionItemCount: number;
}

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

export interface CompletedTaskItem {
  id: string;
  title: string;
  isPriority: boolean;
  completedAt: string | null;
}

export interface InProgressTaskItem {
  id: string;
  title: string;
  isPriority: boolean;
  startedAt: string | null;
}

export interface NoteActivityItem {
  id: string;
  title: string;
  updatedAt: string;
  isNew: boolean;
}
