export interface ApiKeyDto {
  id: string;
  name: string;
  prefix: string;
  profileId: string;
  createdAt: string;
  lastUsedAt: string | null;
  expiresAt: string | null;
  isRevoked: boolean;
}

export interface ApiKeyCreateResponse {
  id: string;
  rawKey: string;
  prefix: string;
}
