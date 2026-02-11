export interface LinkedIdentity {
  id: string;
  provider: string;
  email: string;
  name: string;
  avatarUrl: string | null;
  defaultProfileId: string | null;
  linkedAt: string;
}
