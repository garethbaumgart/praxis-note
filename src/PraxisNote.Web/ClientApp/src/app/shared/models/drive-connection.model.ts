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
}

export interface DriveFolder {
  id: string;
  name: string;
  modifiedTime: string | null;
}
