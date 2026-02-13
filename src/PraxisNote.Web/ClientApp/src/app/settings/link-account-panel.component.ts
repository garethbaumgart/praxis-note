import { Component, ChangeDetectionStrategy, input, output, signal, OnDestroy, inject } from '@angular/core';
import { Profile } from '../profiles/profile.model';
import { LinkedAccountsService } from './linked-accounts.service';
import { AuthService } from '../auth';

type Tab = 'generate' | 'enter';
type GenerateState = 'idle' | 'loading' | 'active' | 'expired';
type EnterState = 'idle' | 'loading' | 'success' | 'error' | 'choose';

@Component({
  selector: 'app-link-account-panel',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="mt-4 border border-border rounded-lg overflow-hidden">
      <!-- Tab header -->
      <div class="flex border-b border-border bg-surface-muted/50">
        <button
          type="button"
          class="flex-1 py-2.5 text-sm font-medium transition-colors"
          [class.text-foreground]="activeTab() === 'generate'"
          [class.border-b-2]="activeTab() === 'generate'"
          [class.border-accent-foreground]="activeTab() === 'generate'"
          [class.text-foreground-muted]="activeTab() !== 'generate'"
          (click)="activeTab.set('generate')"
        >
          Generate Code
        </button>
        <button
          type="button"
          class="flex-1 py-2.5 text-sm font-medium transition-colors"
          [class.text-foreground]="activeTab() === 'enter'"
          [class.border-b-2]="activeTab() === 'enter'"
          [class.border-accent-foreground]="activeTab() === 'enter'"
          [class.text-foreground-muted]="activeTab() !== 'enter'"
          (click)="activeTab.set('enter')"
        >
          Enter Code
        </button>
      </div>

      <!-- Tab content -->
      <div class="p-4">
        @if (activeTab() === 'generate') {
          <!-- Generate Code Tab -->
          @switch (generateState()) {
            @case ('idle') {
              <div class="text-center py-4">
                <p class="text-sm text-foreground-secondary mb-4">
                  Generate a temporary code to link another device or browser to this account.
                </p>
                <button
                  type="button"
                  class="px-4 py-2 text-sm text-white bg-accent-solid hover:opacity-90 rounded-md transition-opacity"
                  (click)="generateCode()"
                >
                  Generate Link Code
                </button>
              </div>
            }
            @case ('loading') {
              <div class="flex items-center justify-center py-6" role="status" aria-label="Generating link code">
                <i class="pi pi-spin pi-spinner text-lg text-foreground-muted" aria-hidden="true"></i>
                <span class="sr-only">Generating link code...</span>
              </div>
            }
            @case ('active') {
              <div class="space-y-4">
                <!-- Code display -->
                <div class="flex items-center justify-center gap-3 py-3 px-4 border-2 border-dashed border-border rounded-lg bg-surface-muted/50">
                  <code class="text-lg font-mono font-bold text-foreground tracking-wider">{{ generatedCode() }}</code>
                  <button
                    type="button"
                    class="touch-target w-7 h-7 flex items-center justify-center rounded text-foreground-muted hover:text-foreground hover:bg-surface-muted transition"
                    (click)="copyCode()"
                    aria-label="Copy link code"
                  >
                    <i class="pi {{ copiedCode() ? 'pi-check' : 'pi-copy' }} text-sm" aria-hidden="true"></i>
                  </button>
                </div>

                <!-- Countdown -->
                <div class="flex items-center justify-center gap-2 text-sm text-foreground-muted">
                  <i class="pi pi-clock text-xs" aria-hidden="true"></i>
                  <span>Expires in {{ remainingTime() }}</span>
                </div>

                <!-- Info box -->
                <div class="flex gap-2 p-3 bg-accent/10 border border-accent/20 rounded-lg">
                  <i class="pi pi-info-circle text-sm text-accent-foreground shrink-0 mt-0.5" aria-hidden="true"></i>
                  <p class="text-xs text-foreground-secondary">
                    Sign in on your other device, go to Settings, and enter this code in the "Enter Code" tab.
                  </p>
                </div>
              </div>
            }
            @case ('expired') {
              <div class="text-center py-4">
                <div class="flex items-center justify-center gap-2 mb-3 text-foreground-muted">
                  <i class="pi pi-clock text-sm" aria-hidden="true"></i>
                  <span class="text-sm">Code expired</span>
                </div>
                <button
                  type="button"
                  class="px-4 py-2 text-sm text-white bg-accent-solid hover:opacity-90 rounded-md transition-opacity"
                  (click)="generateCode()"
                >
                  Generate New Code
                </button>
              </div>
            }
          }
        } @else {
          <!-- Enter Code Tab -->
          @switch (enterState()) {
            @case ('idle') {
              <div class="space-y-4">
                <p class="text-sm text-foreground-secondary">
                  Enter the link code from your other account to connect them.
                </p>
                <div class="flex gap-2">
                  <input
                    type="text"
                    class="flex-1 px-3 py-2 text-sm font-mono bg-surface border border-border rounded-lg text-foreground focus:outline-none focus:ring-2 focus:ring-accent-foreground/30 focus:border-accent-foreground transition"
                    placeholder="PRAXIS-XXXX-XXXX"
                    [value]="enteredCode()"
                    (input)="enteredCode.set($any($event.target).value)"
                    (keydown.enter)="redeemCode()"
                  />
                  <button
                    type="button"
                    class="px-4 py-2 text-sm text-white bg-accent-solid hover:opacity-90 rounded-md transition-opacity"
                    [class.opacity-50]="!enteredCode().trim()"
                    [disabled]="!enteredCode().trim()"
                    (click)="redeemCode()"
                  >
                    Link
                  </button>
                </div>
              </div>
            }
            @case ('loading') {
              <div class="flex items-center justify-center py-6" role="status" aria-label="Linking accounts">
                <i class="pi pi-spin pi-spinner text-lg text-foreground-muted" aria-hidden="true"></i>
                <span class="sr-only">Linking accounts...</span>
              </div>
            }
            @case ('choose') {
              <div class="space-y-4">
                <p class="text-sm text-foreground-secondary">
                  The other account has existing data. How would you like to proceed?
                </p>
                <div class="space-y-2">
                  <button
                    type="button"
                    class="w-full flex items-center gap-3 p-3 border border-border rounded-lg hover:bg-surface-muted transition-colors text-left"
                    (click)="completeLinking('MergeIntoExisting')"
                  >
                    <i class="pi pi-arrow-right-arrow-left text-accent-foreground" aria-hidden="true"></i>
                    <div>
                      <p class="text-sm font-medium text-foreground">Merge into existing profile</p>
                      <p class="text-xs text-foreground-muted">Add the data to your current default profile</p>
                    </div>
                  </button>
                  <button
                    type="button"
                    class="w-full flex items-center gap-3 p-3 border border-border rounded-lg hover:bg-surface-muted transition-colors text-left"
                    (click)="completeLinking('CreateNewProfile')"
                  >
                    <i class="pi pi-plus text-accent-foreground" aria-hidden="true"></i>
                    <div>
                      <p class="text-sm font-medium text-foreground">Create new profile</p>
                      <p class="text-xs text-foreground-muted">Keep the data in a separate profile named after the email</p>
                    </div>
                  </button>
                  <button
                    type="button"
                    class="w-full flex items-center gap-3 p-3 border border-border rounded-lg hover:bg-surface-muted transition-colors text-left"
                    (click)="enterState.set('idle')"
                  >
                    <i class="pi pi-times text-foreground-muted" aria-hidden="true"></i>
                    <div>
                      <p class="text-sm font-medium text-foreground">Cancel</p>
                      <p class="text-xs text-foreground-muted">Don't link these accounts</p>
                    </div>
                  </button>
                </div>
              </div>
            }
            @case ('success') {
              <div class="flex items-center gap-3 p-3 bg-done/20 border border-done/30 rounded-lg">
                <i class="pi pi-check-circle text-done-foreground" aria-hidden="true"></i>
                <div>
                  <p class="text-sm font-medium text-done-foreground">Accounts linked successfully</p>
                  <p class="text-xs text-foreground-muted">You can now sign in from either account.</p>
                </div>
              </div>
            }
            @case ('error') {
              <div class="space-y-4">
                <div class="flex items-center gap-3 p-3 bg-danger/10 border border-danger/30 rounded-lg">
                  <i class="pi pi-times-circle text-danger" aria-hidden="true"></i>
                  <p class="text-sm text-danger">{{ enterError() }}</p>
                </div>
                <button
                  type="button"
                  class="px-4 py-2 text-sm text-foreground-secondary hover:text-foreground transition-colors"
                  (click)="enterState.set('idle')"
                >
                  Try again
                </button>
              </div>
            }
          }
        }

        <!-- Close button -->
        <div class="flex justify-end mt-4 pt-3 border-t border-border">
          <button
            type="button"
            class="px-3 py-1.5 text-xs text-foreground-muted hover:text-foreground transition-colors"
            (click)="onClose.emit()"
          >
            Close
          </button>
        </div>
      </div>
    </div>
  `,
})
export class LinkAccountPanelComponent implements OnDestroy {
  private readonly linkedAccountsService = inject(LinkedAccountsService);
  private readonly authService = inject(AuthService);

  readonly profiles = input<Profile[]>([]);

  readonly onClose = output<void>();
  readonly onLinked = output<void>();

  readonly activeTab = signal<Tab>('generate');

  // Generate tab state
  readonly generateState = signal<GenerateState>('idle');
  readonly generatedCode = signal('');
  readonly copiedCode = signal(false);
  readonly remainingTime = signal('');
  private countdownInterval: ReturnType<typeof setInterval> | null = null;
  private expiresAt: Date | null = null;

  // Enter tab state
  readonly enterState = signal<EnterState>('idle');
  readonly enteredCode = signal('');
  readonly enterError = signal('');
  private pendingCode = '';

  ngOnDestroy(): void {
    this.clearCountdown();
  }

  async generateCode(): Promise<void> {
    this.generateState.set('loading');
    try {
      const result = await this.linkedAccountsService.generateLinkCode();
      this.generatedCode.set(result.code);
      this.expiresAt = new Date(result.expiresAt);
      this.generateState.set('active');
      this.startCountdown();
    } catch {
      this.generateState.set('idle');
    }
  }

  copyCode(): void {
    navigator.clipboard.writeText(this.generatedCode());
    this.copiedCode.set(true);
    setTimeout(() => this.copiedCode.set(false), 2000);
  }

  async redeemCode(): Promise<void> {
    const code = this.enteredCode().trim();
    if (!code) return;

    this.pendingCode = code;
    this.enterState.set('loading');

    try {
      await this.linkedAccountsService.redeemLinkCode(code, 'MergeIntoExisting');
      this.enterState.set('success');
      this.authService.recheckAuth();
      setTimeout(() => this.onLinked.emit(), 1500);
    } catch (err: unknown) {
      const error = err as { error?: { error?: string } };
      const message = error?.error?.error ?? '';

      // Check if merge strategy choice is needed (when user has existing data)
      if (message.includes('has existing data') || message.includes('merge strategy')) {
        this.enterState.set('choose');
      } else {
        this.enterError.set(message || 'Failed to link accounts. Please check the code and try again.');
        this.enterState.set('error');
      }
    }
  }

  async completeLinking(strategy: string): Promise<void> {
    this.enterState.set('loading');
    try {
      await this.linkedAccountsService.redeemLinkCode(this.pendingCode, strategy);
      this.enterState.set('success');
      this.authService.recheckAuth();
      setTimeout(() => this.onLinked.emit(), 1500);
    } catch (err: unknown) {
      const error = err as { error?: { error?: string } };
      this.enterError.set(error?.error?.error || 'Failed to link accounts');
      this.enterState.set('error');
    }
  }

  private startCountdown(): void {
    this.clearCountdown();
    this.updateRemainingTime();
    this.countdownInterval = setInterval(() => this.updateRemainingTime(), 1000);
  }

  private clearCountdown(): void {
    if (this.countdownInterval) {
      clearInterval(this.countdownInterval);
      this.countdownInterval = null;
    }
  }

  private updateRemainingTime(): void {
    if (!this.expiresAt) return;

    const now = new Date();
    const diff = this.expiresAt.getTime() - now.getTime();

    if (diff <= 0) {
      this.generateState.set('expired');
      this.clearCountdown();
      this.remainingTime.set('0:00');
      return;
    }

    const minutes = Math.floor(diff / 60000);
    const seconds = Math.floor((diff % 60000) / 1000);
    this.remainingTime.set(`${minutes}:${seconds.toString().padStart(2, '0')}`);
  }
}
