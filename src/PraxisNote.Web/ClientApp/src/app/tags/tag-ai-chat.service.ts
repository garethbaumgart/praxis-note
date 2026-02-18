import { Injectable, inject, signal, computed, isDevMode } from '@angular/core';
import { MockAuthService } from '../auth/mock-auth.service';
import { ProfileService } from '../profiles/profile.service';
import { ChatMessageItem, TagAiChatState, TagChatHistoryItem, TagChatRequest } from './tag-ai-chat.model';

@Injectable({ providedIn: 'root' })
export class TagAiChatService {
  private readonly mockAuth = inject(MockAuthService);
  private readonly profileService = inject(ProfileService);

  private readonly _messages = signal<ChatMessageItem[]>([]);
  private readonly _starters = signal<string[]>([]);
  private readonly _state = signal<TagAiChatState>('idle');
  private readonly _isOpen = signal(false);
  private readonly _isCollapsed = signal(false);
  private readonly _error = signal<string | null>(null);

  private abortController: AbortController | null = null;
  private currentTagId: string | null = null;

  readonly messages = this._messages.asReadonly();
  readonly starters = this._starters.asReadonly();
  readonly state = this._state.asReadonly();
  readonly isOpen = this._isOpen.asReadonly();
  readonly isCollapsed = this._isCollapsed.asReadonly();
  readonly error = this._error.asReadonly();
  readonly hasMessages = computed(() => this._messages().length > 0);

  open(tagId: string): void {
    if (this._isOpen() && this.currentTagId === tagId) {
      // Already open for this tag — just expand if collapsed
      this._isCollapsed.set(false);
      return;
    }

    this.currentTagId = tagId;
    this._isOpen.set(true);
    this._isCollapsed.set(false);
    this._messages.set([]);
    this._starters.set([]);
    this._error.set(null);
    this._state.set('loading-starters');

    this.loadStarters(tagId);
  }

  close(): void {
    this.stop();
    this._isOpen.set(false);
    this._isCollapsed.set(false);
    this._messages.set([]);
    this._starters.set([]);
    this._error.set(null);
    this._state.set('idle');
    this.currentTagId = null;
  }

  collapse(): void {
    this._isCollapsed.set(true);
  }

  expand(): void {
    this._isCollapsed.set(false);
  }

  stop(): void {
    if (this.abortController) {
      this.abortController.abort();
      this.abortController = null;
    }
    if (this._state() === 'streaming') {
      this._state.set('ready');
    }
  }

  clearChat(): void {
    this.stop();
    this._messages.set([]);
    this._error.set(null);
    this._state.set('loading-starters');
    if (this.currentTagId) {
      this.loadStarters(this.currentTagId);
    }
  }

  async send(message: string): Promise<void> {
    if (!this.currentTagId || !message.trim()) return;

    const tagId = this.currentTagId;
    this._error.set(null);
    this.stop();

    // Add user message
    this._messages.update(msgs => [...msgs, { role: 'user' as const, content: message }]);
    this._state.set('streaming');

    // Build history from previous messages (excluding the one we just added)
    const allMessages = this._messages();
    const history: TagChatHistoryItem[] = allMessages.slice(0, -1).map(m => ({
      role: m.role,
      content: m.content,
    }));

    const request: TagChatRequest = {
      message,
      history: history.length > 0 ? history : undefined,
    };

    // Add empty assistant message that we'll stream into
    this._messages.update(msgs => [...msgs, { role: 'assistant' as const, content: '' }]);

    this.abortController = new AbortController();

    try {
      const headers: Record<string, string> = {
        'Content-Type': 'application/json',
      };

      // Add auth headers for fetch (interceptors only work with HttpClient)
      if (isDevMode()) {
        const mockHeader = this.mockAuth.getMockHeader();
        if (mockHeader) {
          headers['X-Mock-User'] = mockHeader;
        }
      }

      const profileId = this.profileService.activeProfileId();
      if (profileId) {
        headers['X-Profile-Id'] = profileId;
      }

      const response = await fetch(`/api/tags/${tagId}/chat`, {
        method: 'POST',
        headers,
        credentials: 'include',
        body: JSON.stringify(request),
        signal: this.abortController.signal,
      });

      if (!response.ok) {
        throw new Error(`HTTP ${response.status}`);
      }

      const reader = response.body?.getReader();
      if (!reader) throw new Error('No response body');

      const decoder = new TextDecoder();
      let buffer = '';
      let streamDone = false;

      while (true) {
        const { done, value } = await reader.read();
        if (done) break;

        buffer += decoder.decode(value, { stream: true });
        const lines = buffer.split('\n');
        buffer = lines.pop() ?? '';

        for (const line of lines) {
          if (line.startsWith('event: done')) {
            streamDone = true;
            continue;
          }

          if (line.startsWith('event: error')) {
            continue; // The error data comes on the next data: line
          }

          if (line.startsWith('data: ')) {
            const dataStr = line.slice(6);
            try {
              const data = JSON.parse(dataStr);
              if (data.token) {
                this._messages.update(msgs => {
                  const updated = [...msgs];
                  const last = updated[updated.length - 1];
                  if (last?.role === 'assistant') {
                    updated[updated.length - 1] = { ...last, content: last.content + data.token };
                  }
                  return updated;
                });
              }
              if (data.error) {
                // Remove the empty assistant message we added for streaming
                this._messages.update(msgs => {
                  const last = msgs[msgs.length - 1];
                  return last?.role === 'assistant' && !last.content
                    ? msgs.slice(0, -1)
                    : msgs;
                });
                this._error.set(data.error);
                this._state.set('error');
                return;
              }
            } catch {
              // Ignore unparseable SSE data
            }
          }
        }

        if (streamDone) {
          this._state.set('ready');
          return;
        }
      }

      // If we exited the read loop without a done event, set to ready
      if (this._state() === 'streaming') {
        this._state.set('ready');
      }
    } catch (err: unknown) {
      if (err instanceof DOMException && err.name === 'AbortError') {
        // User cancelled — keep ready state
        if (this._state() === 'streaming') {
          this._state.set('ready');
        }
        return;
      }
      this._error.set('Failed to get a response. Please try again.');
      this._state.set('error');
    } finally {
      this.abortController = null;
    }
  }

  private async loadStarters(tagId: string): Promise<void> {
    try {
      const headers: Record<string, string> = { 'Content-Type': 'application/json' };

      if (isDevMode()) {
        const mockHeader = this.mockAuth.getMockHeader();
        if (mockHeader) {
          headers['X-Mock-User'] = mockHeader;
        }
      }

      const profileId = this.profileService.activeProfileId();
      if (profileId) {
        headers['X-Profile-Id'] = profileId;
      }

      const response = await fetch(`/api/tags/${tagId}/starters`, {
        method: 'POST',
        headers,
        credentials: 'include',
        body: '{}',
      });

      if (this.currentTagId !== tagId) return;

      if (response.ok) {
        const data = await response.json();
        this._starters.set(data.starters ?? []);
      } else {
        this._starters.set([]);
      }
    } catch {
      if (this.currentTagId !== tagId) return;
      this._starters.set([]);
    } finally {
      if (this.currentTagId === tagId) {
        this._state.set('ready');
      }
    }
  }
}
