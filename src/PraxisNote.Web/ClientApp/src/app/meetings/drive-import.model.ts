export type DriveImportDuplicateType = 'None' | 'ExactFile' | 'CalendarEvent' | 'FuzzyMatch';

export interface DriveFileImportDto {
  id: string;
  driveFileId: string;
  fileName: string;
  status: string;
  parsedResultJson: string | null;
  duplicateType: DriveImportDuplicateType;
  duplicateConfidence: number;
  matchedMeetingId: string | null;
  duplicateMatchTitle: string | null;
}

export interface ParsedResult {
  title: string | null;
  meetingDate: string | null;
  attendees: string | null;
  summary: string | null;
  keyPoints: string[] | null;
  decisions: string[] | null;
  actionItems: { description: string; assignee: string | null }[] | null;
  suggestedTags: string[];
  transcript: string | null;
}

export interface DriveImportPreviewFile {
  id: string;
  driveFileId: string;
  fileName: string;
  title: string | null;
  meetingDate: string | null;
  attendees: string | null;
  summary: string | null;
  keyPoints: string[] | null;
  decisions: string[] | null;
  actionItems: { description: string; assignee: string | null }[] | null;
  suggestedTags: string[];
  duplicateType: 'none' | 'definite' | 'possible';
  duplicateConfidence: number;
  matchedMeetingId: string | null;
  matchedMeetingTitle: string | null;
  status: string;
  selected: boolean;
  expanded: boolean;
  editedTags: string[];
}

export interface DriveImportConfirmResult {
  importedCount: number;
  totalActionItems: number;
  tagsCreated: number;
  skippedCount: number;
  failures: { driveFileImportId: string; fileName: string; error: string }[];
}
