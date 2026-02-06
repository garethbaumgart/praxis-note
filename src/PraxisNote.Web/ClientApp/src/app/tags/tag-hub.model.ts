export interface TagItemDto {
  id: string;
  title: string;
  type: 'Meeting' | 'Note' | 'Task';
  date: string;
  meetingDate?: string;
  attendeeCount?: number;
  updatedAt?: string;
  status?: string;
  isPriority?: boolean;
  dueDate?: string;
}

export interface TagItemsResponse {
  items: TagItemDto[];
  meetingCount: number;
  noteCount: number;
  taskCount: number;
  totalCount: number;
}
