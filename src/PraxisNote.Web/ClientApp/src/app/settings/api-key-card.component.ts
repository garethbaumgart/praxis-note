import { Component, ChangeDetectionStrategy, input, output, computed } from '@angular/core';
import { DatePipe } from '@angular/common';
import { ApiKeyDto } from './api-key.model';
import { Profile } from '../profiles/profile.model';

@Component({
  selector: 'app-api-key-card',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DatePipe],
  template: `
    <div class="flex items-center justify-between py-3 px-4 bg-surface-subtle rounded-lg">
      <div class="flex-1 min-w-0">
        <div class="flex items-center gap-2">
          <span class="text-sm font-medium text-foreground truncate">{{ apiKey().name }}</span>
          <code class="text-xs text-foreground-muted bg-surface-muted px-1.5 py-0.5 rounded font-mono">{{ apiKey().prefix }}...</code>
        </div>
        <div class="flex flex-wrap items-center gap-x-3 gap-y-1 mt-1 text-xs text-foreground-muted">
          <span>Created {{ apiKey().createdAt | date:'mediumDate' }}</span>
          @if (apiKey().lastUsedAt) {
            <span>Last used {{ apiKey().lastUsedAt | date:'medium' }}</span>
          } @else {
            <span>Never used</span>
          }
          @if (apiKey().expiresAt) {
            <span>Expires {{ apiKey().expiresAt | date:'mediumDate' }}</span>
          }
          @if (profileName()) {
            <span class="flex items-center gap-1">
              <i class="pi pi-user text-[10px]" aria-hidden="true"></i>
              {{ profileName() }}
            </span>
          }
        </div>
      </div>
      <button
        type="button"
        class="p-2 text-foreground-muted hover:text-danger transition-colors rounded-md"
        (click)="onRevoke.emit(apiKey().id)"
        aria-label="Revoke API key"
      >
        <i class="pi pi-trash text-sm" aria-hidden="true"></i>
      </button>
    </div>
  `,
})
export class ApiKeyCardComponent {
  readonly apiKey = input.required<ApiKeyDto>();
  readonly profiles = input.required<Profile[]>();
  readonly onRevoke = output<string>();

  readonly profileName = computed(() => {
    const key = this.apiKey();
    const profiles = this.profiles();
    const profile = profiles.find(p => p.id === key.profileId);
    return profile?.name ?? null;
  });
}
