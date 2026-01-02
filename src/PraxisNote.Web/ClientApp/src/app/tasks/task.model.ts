export interface Task {
  id: string;
  title: string;
  status: 'Todo' | 'InProgress' | 'Done';
  position: number;
  createdAt: string;
  completedAt: string | null;
}
