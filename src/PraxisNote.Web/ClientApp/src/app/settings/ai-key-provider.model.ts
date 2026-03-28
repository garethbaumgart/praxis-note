export type AiProvider = 'Anthropic' | 'OpenAI' | 'Gemini';

export interface AiKeyDto {
  provider: AiProvider;
  hasKey: boolean;
  keyHint: string | null;
  preferredModel: string | null;
  createdAt: string | null;
}

export interface UpsertAiKeyRequest {
  apiKey: string;
  preferredModel?: string;
}

export interface ValidateKeyResult {
  validated: boolean;
  rateLimited?: boolean;
}
