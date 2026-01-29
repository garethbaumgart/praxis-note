export interface CalendarConnectionStatus {
  isConnected: boolean;
  provider: string | null;
  connectedAt: string | null;
  lastSyncedAt: string | null;
}

export interface SyncResult {
  importedCount: number;
  skippedCount: number;
}
