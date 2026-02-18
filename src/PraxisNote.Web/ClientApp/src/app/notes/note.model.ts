export interface Checkbox {
  id: string;
  text: string;
  isChecked: boolean;
}

export interface NoteTag {
  id: string;
  name: string;
}

export interface Note {
  id: string;
  content: string;
  checkboxes: Checkbox[];
  tags: NoteTag[];
  meetingId?: string;
  meetingTitle?: string;
  createdAt: string;
  updatedAt: string;
}

export interface CheckboxStatus {
  checkboxId: string;
  isLinked: boolean;
  taskId: string | null;
  taskStatus: 'Todo' | 'InProgress' | 'Done' | null;
}

export interface PromoteCheckboxResult {
  taskId: string;
  title: string;
  status: string;
}
