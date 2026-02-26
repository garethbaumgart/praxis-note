export interface DriveConnectionStatus {
  isConnected: boolean;
  provider: string | null;
  connectedAt: string | null;
  lastSyncedAt: string | null;
  folderName: string | null;
  folderId: string | null;
  initialImportCutoffDate: string | null;
  syncFrequencyMinutes: number | null;
  autoAcceptTags: boolean;
  isConfigured: boolean;
  // Sync tracking fields
  lastSyncAt: string | null;
  nextSyncAt: string | null;
  lastSyncFilesDiscovered: number;
  lastSyncFilesImported: number;
  lastSyncFilesPendingReview: number;
  lastSyncFilesErrored: number;
  lastSyncError: string | null;
  isSyncPaused: boolean;
  pendingReviewCount: number;
}

export interface DriveFolder {
  id: string;
  name: string;
  modifiedTime: string | null;
}

export interface DriveSyncResult {
  filesDiscovered: number;
  filesImported: number;
  filesPendingReview: number;
  filesErrored: number;
  error: string | null;
}
