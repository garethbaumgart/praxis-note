export interface Notification {
  id: string;
  type: 'Feature' | 'BugFix' | 'Improvement';
  title: string;
  summary: string;
  issueUrl: string | null;
  createdAt: string;
  isSeen: boolean;
}
