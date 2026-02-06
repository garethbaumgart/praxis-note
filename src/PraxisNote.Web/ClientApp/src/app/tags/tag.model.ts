/** Full tag with usage statistics for listing. */
export interface Tag {
  id: string;
  name: string;
  usageCount: number;
  taskCount: number;
  noteCount: number;
  meetingCount: number;
}

/** Minimal tag info for embedding in task responses. */
export interface TaskTag {
  id: string;
  name: string;
}

/** Preview data for a tag merge operation. */
export interface MergePreview {
  sourceTagName: string;
  sourceTaskCount: number;
  sourceNoteCount: number;
  sourceMeetingCount: number;
  targetTagName: string;
  targetTaskCount: number;
  targetNoteCount: number;
  targetMeetingCount: number;
  resultTaskCount: number;
  resultNoteCount: number;
  resultMeetingCount: number;
  overlapCount: number;
}

/** Result of a completed tag merge operation. */
export interface MergeResult {
  taskCount: number;
  noteCount: number;
  meetingCount: number;
  totalCount: number;
}
