export interface DriveConnectionStatus {
  isConnected: boolean;
  provider: string | null;
  connectedAt: string | null;
  lastSyncedAt: string | null;
  folderName: string | null;
}
