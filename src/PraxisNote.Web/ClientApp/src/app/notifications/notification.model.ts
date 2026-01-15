export interface Notification {
  id: number;
  type: 'Feature' | 'BugFix' | 'Improvement';
  title: string;
  summary: string;
  issueUrl: string | null;
  createdAt: string;
  isSeen: boolean;
}
