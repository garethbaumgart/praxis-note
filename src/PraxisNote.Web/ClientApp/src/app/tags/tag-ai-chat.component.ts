import { Component, ChangeDetectionStrategy, inject, signal, ViewChild, ElementRef, AfterViewChecked } from '@angular/core';
import { Skeleton } from 'primeng/skeleton';
import { TagAiChatService } from './tag-ai-chat.service';

@Component({
  selector: 'app-tag-ai-chat',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Skeleton],
  template: `
    @if (chat.isOpen()) {
      @if (chat.isCollapsed()) {
        <!-- Collapsed bar -->
        <button
          type="button"
          class="w-full flex items-center gap-2 px-4 py-2.5 rounded-xl bg-surface-muted text-sm text-foreground-secondary hover:bg-accent hover:text-accent-foreground transition-colors"
          (click)="chat.expand()"
          aria-label="Expand AI chat">
          <i class="pi pi-sparkles text-xs" aria-hidden="true"></i>
          <span class="font-medium">AI Chat</span>
          @if (chat.hasMessages()) {
            <span class="text-xs text-foreground-muted ml-auto">{{ chat.messages().length }} messages</span>
          }
          <i class="pi pi-chevron-up text-xs ml-auto" aria-hidden="true"></i>
        </button>
      } @else {
        <!-- Expanded chat panel -->
        <div class="rounded-xl border border-border bg-surface overflow-hidden">
          <!-- Header -->
          <div class="flex items-center gap-2 px-4 py-2.5 border-b border-border">
            <i class="pi pi-sparkles text-sm text-accent-foreground" aria-hidden="true"></i>
            <span class="text-sm font-medium text-foreground">Ask AI</span>
            <div class="ml-auto flex items-center gap-1">
              @if (chat.hasMessages()) {
                <button
                  type="button"
                  class="touch-target w-7 h-7 flex items-center justify-center rounded-lg text-foreground-muted hover:bg-surface-muted transition-colors"
                  (click)="chat.clearChat()"
                  aria-label="Clear chat">
                  <i class="pi pi-refresh text-xs" aria-hidden="true"></i>
                </button>
              }
              <button
                type="button"
                class="touch-target w-7 h-7 flex items-center justify-center rounded-lg text-foreground-muted hover:bg-surface-muted transition-colors"
                (click)="chat.collapse()"
                aria-label="Minimize chat">
                <i class="pi pi-chevron-down text-xs" aria-hidden="true"></i>
              </button>
              <button
                type="button"
                class="touch-target w-7 h-7 flex items-center justify-center rounded-lg text-foreground-muted hover:bg-surface-muted transition-colors"
                (click)="chat.close()"
                aria-label="Close chat">
                <i class="pi pi-times text-xs" aria-hidden="true"></i>
              </button>
            </div>
          </div>

          <!-- Chat body -->
          <div #chatBody class="px-4 py-3 max-h-80 overflow-y-auto space-y-3" role="log" aria-live="polite" aria-label="AI chat messages">
            @if (chat.state() === 'loading-starters') {
              <div role="status" aria-label="Loading suggestions">
                <span class="sr-only">Loading suggestions...</span>
                <div class="space-y-2">
                  <p-skeleton width="85%" height="2rem" styleClass="rounded-lg" />
                  <p-skeleton width="70%" height="2rem" styleClass="rounded-lg" />
                  <p-skeleton width="90%" height="2rem" styleClass="rounded-lg" />
                </div>
              </div>
            } @else if (!chat.hasMessages()) {
              <!-- Starter prompts -->
              @if (chat.starters().length > 0) {
                <div class="space-y-2">
                  <p class="text-xs text-foreground-muted mb-2">Try asking:</p>
                  @for (starter of chat.starters(); track starter) {
                    <button
                      type="button"
                      class="w-full text-left px-3 py-2 text-sm rounded-lg border border-border text-foreground-secondary hover:bg-surface-muted hover:text-foreground transition-colors"
                      (click)="sendMessage(starter)">
                      {{ starter }}
                    </button>
                  }
                </div>
              } @else {
                <p class="text-sm text-foreground-muted text-center py-4">
                  Ask a question about the content in this tag.
                </p>
              }
            } @else {
              <!-- Messages -->
              @for (msg of chat.messages(); track $index) {
                @if (msg.role === 'user') {
                  <div class="flex justify-end">
                    <div class="max-w-[85%] px-3 py-2 rounded-lg bg-accent text-accent-foreground text-sm">
                      {{ msg.content }}
                    </div>
                  </div>
                } @else {
                  <div class="flex justify-start">
                    <div class="max-w-[85%] px-3 py-2 rounded-lg bg-surface-muted text-foreground text-sm whitespace-pre-wrap break-words ai-response">
                      {{ msg.content }}
                      @if (chat.state() === 'streaming' && $last && !msg.content) {
                        <span class="inline-block w-2 h-4 bg-foreground-muted animate-pulse rounded-sm"></span>
                      }
                    </div>
                  </div>
                }
              }
            }

            @if (chat.error()) {
              <div class="flex items-center gap-2 px-3 py-2 rounded-lg bg-danger-bg text-danger text-sm">
                <i class="pi pi-exclamation-circle text-xs" aria-hidden="true"></i>
                <span>{{ chat.error() }}</span>
              </div>
            }
          </div>

          <!-- Input area -->
          <div class="border-t border-border px-4 py-3">
            <div class="flex items-center gap-2">
              <input
                #chatInput
                type="text"
                class="flex-1 px-3 py-2 text-sm border border-border rounded-lg bg-surface text-foreground placeholder-foreground-muted focus:outline-none focus:ring-2 focus:ring-accent"
                placeholder="Ask about this tag..."
                [value]="inputValue()"
                (input)="inputValue.set($any($event.target).value)"
                (keydown.enter)="onEnter()"
                (keydown.escape)="onEscape()"
                [disabled]="chat.state() === 'streaming'"
                aria-label="Chat message input"
              />
              @if (chat.state() === 'streaming') {
                <button
                  type="button"
                  class="w-9 h-9 flex items-center justify-center rounded-lg bg-danger text-white hover:opacity-90 transition-opacity shrink-0"
                  (click)="chat.stop()"
                  aria-label="Stop generating">
                  <i class="pi pi-stop text-xs" aria-hidden="true"></i>
                </button>
              } @else {
                <button
                  type="button"
                  class="w-9 h-9 flex items-center justify-center rounded-lg bg-accent-solid text-white hover:opacity-90 transition-opacity shrink-0 disabled:opacity-50"
                  (click)="onSend()"
                  [disabled]="!inputValue().trim()"
                  aria-label="Send message">
                  <i class="pi pi-send text-xs" aria-hidden="true"></i>
                </button>
              }
            </div>
          </div>
        </div>
      }
    }
  `,
})
export class TagAiChatComponent implements AfterViewChecked {
  readonly chat = inject(TagAiChatService);

  @ViewChild('chatBody') chatBody?: ElementRef<HTMLDivElement>;
  @ViewChild('chatInput') chatInput?: ElementRef<HTMLInputElement>;

  readonly inputValue = signal('');
  private shouldScrollToBottom = false;
  private lastMessageCount = 0;

  ngAfterViewChecked(): void {
    const currentCount = this.chat.messages().length;
    if (currentCount !== this.lastMessageCount || this.shouldScrollToBottom) {
      this.lastMessageCount = currentCount;
      this.shouldScrollToBottom = false;
      this.scrollToBottom();
    }
  }

  onEnter(): void {
    if (this.chat.state() === 'streaming') return;
    this.onSend();
  }

  onEscape(): void {
    if (this.chat.state() === 'streaming') {
      this.chat.stop();
    } else {
      this.chat.close();
    }
  }

  onSend(): void {
    const value = this.inputValue().trim();
    if (!value) return;
    this.inputValue.set('');
    this.shouldScrollToBottom = true;
    this.chat.send(value);
  }

  sendMessage(message: string): void {
    this.inputValue.set('');
    this.shouldScrollToBottom = true;
    this.chat.send(message);
  }

  private scrollToBottom(): void {
    if (this.chatBody?.nativeElement) {
      const el = this.chatBody.nativeElement;
      el.scrollTop = el.scrollHeight;
    }
  }
}
