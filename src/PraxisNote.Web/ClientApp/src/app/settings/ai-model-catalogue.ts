import { AiProvider } from './ai-key-provider.model';

export type AiModelTag = 'fast' | 'balanced' | 'powerful' | 'cheap' | 'free-tier';

export interface AiModelOption {
  value: string;
  label: string;
  description: string;
  tags: AiModelTag[];
  isDefault?: boolean;
}

export const AI_MODEL_CATALOGUE: Record<AiProvider, AiModelOption[]> = {
  Anthropic: [
    {
      value: 'claude-sonnet-4-6',
      label: 'Claude Sonnet 4.6',
      description: 'Best balance of speed and quality',
      tags: ['balanced'],
      isDefault: true,
    },
    {
      value: 'claude-haiku-4-5',
      label: 'Claude Haiku 4.5',
      description: 'Fastest and most affordable',
      tags: ['fast', 'cheap'],
    },
    {
      value: 'claude-opus-4-6',
      label: 'Claude Opus 4.6',
      description: 'Most capable for complex tasks',
      tags: ['powerful'],
    },
  ],
  OpenAI: [
    {
      value: 'gpt-4o-mini',
      label: 'GPT-4o Mini',
      description: 'Fast and affordable',
      tags: ['fast', 'cheap'],
      isDefault: true,
    },
    {
      value: 'gpt-4o',
      label: 'GPT-4o',
      description: 'Best balance of speed and quality',
      tags: ['balanced'],
    },
    {
      value: 'gpt-4.1',
      label: 'GPT-4.1',
      description: 'Most capable model',
      tags: ['powerful'],
    },
  ],
  Gemini: [
    {
      value: 'gemini-1.5-flash',
      label: 'Gemini 1.5 Flash',
      description: 'Fast and free tier eligible',
      tags: ['fast', 'free-tier'],
      isDefault: true,
    },
    {
      value: 'gemini-1.5-pro',
      label: 'Gemini 1.5 Pro',
      description: 'Best balance of speed and quality',
      tags: ['balanced'],
    },
    {
      value: 'gemini-2.0-flash',
      label: 'Gemini 2.0 Flash',
      description: 'Latest and fastest',
      tags: ['fast'],
    },
  ],
};
