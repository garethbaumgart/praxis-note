export interface Comment {
  id: string;
  content: string;
  createdAt: string;
  updatedAt: string;
}

export interface Task {
  id: string;
  title: string;
  status: 'Todo' | 'InProgress' | 'Done';
  position: number;
  isPriority: boolean;
  createdAt: string;
  startedAt: string | null;
  completedAt: string | null;
  comments: Comment[];
  dueDate: string | null;
}
