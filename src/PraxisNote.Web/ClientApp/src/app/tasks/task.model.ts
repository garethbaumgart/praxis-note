export interface Task {
  id: string;
  title: string;
  status: 'Todo' | 'InProgress' | 'Done';
  createdAt: string;
  completedAt: string | null;
}
