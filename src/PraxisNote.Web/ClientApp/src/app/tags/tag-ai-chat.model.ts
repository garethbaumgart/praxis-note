/** State of the AI chat panel */
export type TagAiChatState = 'idle' | 'loading-starters' | 'ready' | 'streaming' | 'error';

/** Role for chat messages */
export type ChatRole = 'user' | 'assistant';

/** A single chat message */
export interface ChatMessageItem {
  role: ChatRole;
  content: string;
}

/** Request payload for the chat endpoint */
export interface TagChatRequest {
  message: string;
  history?: TagChatHistoryItem[];
}

/** History item sent to the backend */
export interface TagChatHistoryItem {
  role: string;
  content: string;
}
