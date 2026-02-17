export interface JiraConnectionStatus {
  isConnected: boolean;
  siteUrl: string | null;
  connectedAt: string | null;
}

export interface JiraIssue {
  key: string;
  summary: string;
  status: string;
  statusCategory: string;
  issueType: string;
  url: string;
}
