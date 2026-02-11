import { Component, ChangeDetectionStrategy, input, output, signal, effect, untracked } from '@angular/core';
import { Dialog } from 'primeng/dialog';

const ICON_OPTIONS = [
  'pi-briefcase',
  'pi-home',
  'pi-bolt',
  'pi-heart',
  'pi-star',
  'pi-code',
  'pi-book',
  'pi-globe',
  'pi-graduation-cap',
  'pi-palette',
  'pi-wrench',
  'pi-flag',
  'pi-shield',
  'pi-map',
  'pi-building',
  'pi-users',
];

@Component({
  selector: 'app-create-profile-dialog',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Dialog],
  template: `
    <p-dialog
      [visible]="visible()"
      (visibleChange)="visible.set($event)"
      [modal]="true"
      [draggable]="false"
      [resizable]="false"
      [dismissableMask]="true"
      [closable]="true"
      [style]="{ width: '24rem' }"
      [breakpoints]="{ '640px': '95vw' }"
      [header]="editProfile() ? 'Edit Profile' : 'New Profile'"
    >
      <div class="space-y-4 px-1">
        <!-- Name input -->
        <div>
          <label for="profileName" class="block text-sm font-medium text-foreground mb-1">Name</label>
          <input
            id="profileName"
            type="text"
            class="w-full px-3 py-2 text-sm bg-surface border border-border rounded-lg text-foreground focus:outline-none focus:ring-2 focus:ring-accent-foreground/30 focus:border-accent-foreground transition"
            placeholder="e.g., Work, Personal"
            maxlength="50"
            [value]="name()"
            (input)="name.set($any($event.target).value)"
            (keydown.enter)="save()"
          />
        </div>

        <!-- Icon picker -->
        <div>
          <label class="block text-sm font-medium text-foreground mb-2">Icon</label>
          <div class="grid grid-cols-8 gap-1.5">
            @for (icon of iconOptions; track icon) {
              <button
                type="button"
                class="w-8 h-8 flex items-center justify-center rounded-lg transition-colors"
                [class.bg-accent]="selectedIcon() === icon"
                [class.text-accent-foreground]="selectedIcon() === icon"
                [class.text-foreground-muted]="selectedIcon() !== icon"
                [class.hover:bg-surface-muted]="selectedIcon() !== icon"
                (click)="selectedIcon.set(icon)"
                [attr.aria-label]="'Select icon ' + icon"
              >
                <i class="pi {{ icon }} text-sm" aria-hidden="true"></i>
              </button>
            }
            <!-- No icon option -->
            <button
              type="button"
              class="w-8 h-8 flex items-center justify-center rounded-lg transition-colors"
              [class.bg-accent]="selectedIcon() === null"
              [class.text-accent-foreground]="selectedIcon() === null"
              [class.text-foreground-muted]="selectedIcon() !== null"
              [class.hover:bg-surface-muted]="selectedIcon() !== null"
              (click)="selectedIcon.set(null)"
              aria-label="No icon"
            >
              <i class="pi pi-user text-sm" aria-hidden="true"></i>
            </button>
          </div>
        </div>
      </div>

      <!-- Footer -->
      <div class="flex justify-end gap-3 px-5 py-4 border-t border-border mt-4 -mx-5 -mb-5">
        <button
          type="button"
          class="px-4 py-1.5 text-sm text-foreground-secondary hover:text-foreground transition-colors"
          (click)="visible.set(false)"
        >
          Cancel
        </button>
        <button
          type="button"
          class="px-4 py-1.5 text-sm text-white bg-accent-solid hover:opacity-90 rounded-md transition-opacity"
          [class.opacity-50]="!name().trim()"
          [disabled]="!name().trim()"
          (click)="save()"
        >
          {{ editProfile() ? 'Save' : 'Create' }}
        </button>
      </div>
    </p-dialog>
  `,
})
export class CreateProfileDialogComponent {
  readonly visible = signal(false);
  readonly editProfile = signal<{ id: string; name: string; icon: string | null } | null>(null);

  readonly name = signal('');
  readonly selectedIcon = signal<string | null>(null);

  readonly onSave = output<{ name: string; icon: string | null; editId?: string }>();

  readonly iconOptions = ICON_OPTIONS;

  constructor() {
    // Reset form when dialog opens/closes
    effect(() => {
      const isVisible = this.visible();
      if (isVisible) {
        untracked(() => {
          const edit = this.editProfile();
          if (edit) {
            this.name.set(edit.name);
            this.selectedIcon.set(edit.icon);
          } else {
            this.name.set('');
            this.selectedIcon.set(null);
          }
        });
      }
    });
  }

  open(edit?: { id: string; name: string; icon: string | null }): void {
    this.editProfile.set(edit ?? null);
    this.visible.set(true);
  }

  save(): void {
    const trimmedName = this.name().trim();
    if (!trimmedName) return;

    const editId = this.editProfile()?.id;
    this.onSave.emit({ name: trimmedName, icon: this.selectedIcon(), editId });
    this.visible.set(false);
  }
}
