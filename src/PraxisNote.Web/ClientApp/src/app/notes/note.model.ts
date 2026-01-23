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
  createdAt: string;
  updatedAt: string;
}
