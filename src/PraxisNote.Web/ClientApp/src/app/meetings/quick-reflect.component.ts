import { Component, computed, inject, input, output, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DialogModule } from 'primeng/dialog';
import {
  Meeting,
  MeetingReflection,
  QUICK_REFLECT_DIMENSIONS,
  QuickReflectionDimension,
  QuickReflectionValue,
  EmojiLevel,
  mapQuickReflectToJohari,
} from './meeting.model';
import { MeetingService } from './meeting.service';

@Component({
  selector: 'app-quick-reflect',
  imports: [CommonModule, DialogModule],
  template: `
    <p-dialog
      [visible]="visible()"
      (visibleChange)="visibleChange.emit($event)"
      [modal]="true"
      [draggable]="false"
      [resizable]="false"
      [dismissableMask]="true"
      [closable]="true"
      [style]="{ width: '30rem' }"
      [breakpoints]="{ '640px': '95vw' }"
      header="Quick Reflect"
      [styleClass]="'quick-reflect-dialog'"
    >
      @if (completed()) {
        <div class="p-6 text-center">
          <div class="w-12 h-12 rounded-full mx-auto mb-3 flex items-center justify-center" style="background: rgba(163, 190, 140, 0.15);">
            <i class="pi pi-check text-lg" style="color: #a3be8c;"></i>
          </div>
          <div class="text-sm font-medium" style="color: #a3be8c;">Reflected</div>
          <div class="text-xs text-foreground-muted mt-1">Your insight was saved</div>
        </div>
      } @else {
        <div class="space-y-4">
          @for (dimension of dimensions; track dimension.id) {
            <div>
              <div class="text-xs font-medium text-foreground-secondary mb-2">{{ dimension.label }}</div>
              <div class="flex gap-3">
                @for (level of levels; track level) {
                  <div class="flex flex-col items-center gap-1">
                    <button
                      type="button"
                      class="emoji-btn"
                      [class.selected]="values()[dimension.id] === level"
                      (click)="selectEmoji(dimension.id, level)"
                      [attr.aria-label]="dimension.label + ': ' + dimension.emojis[level].label"
                    >
                      {{ dimension.emojis[level].emoji }}
                    </button>
                    <span class="text-[10px] text-foreground-muted">{{ dimension.emojis[level].label }}</span>
                  </div>
                }
              </div>
            </div>
          }

          @if (!noteExpanded()) {
            <button
              type="button"
              class="text-xs text-foreground-muted hover:text-foreground-secondary flex items-center gap-1"
              (click)="toggleNoteExpanded()"
            >
              <i class="pi pi-plus text-[10px]"></i> Add a note (optional)
            </button>
          } @else {
            <div>
              <label class="text-xs font-medium text-foreground-secondary mb-1 block">Note (optional)</label>
              <textarea
                class="w-full text-sm p-2 rounded-md border border-border bg-surface"
                [rows]="2"
                [value]="freeformNote()"
                (input)="freeformNote.set($any($event.target).value)"
                placeholder="Additional thoughts..."
              ></textarea>
            </div>
          }
        </div>

        <div class="flex items-center justify-between pt-4 border-t border-border mt-4">
          <button
            type="button"
            class="text-xs text-foreground-muted hover:text-accent-foreground"
            (click)="expandToFull()"
          >
            Expand to full reflection
          </button>
          <button
            type="button"
            class="px-4 py-1.5 text-sm text-white rounded-md"
            style="background: var(--color-meeting-reflection-border);"
            (click)="save()"
          >
            Save
          </button>
        </div>
      }
    </p-dialog>
  `,
  styles: [`
    :host ::ng-deep .quick-reflect-dialog .p-dialog-content {
      padding: 1.25rem;
    }

    .emoji-btn {
      width: 48px;
      height: 48px;
      border-radius: 12px;
      border: 2px solid var(--color-border);
      display: flex;
      align-items: center;
      justify-content: center;
      font-size: 24px;
      cursor: pointer;
      transition: all 0.15s;
      background: var(--color-bg-subtle);
    }

    .emoji-btn:hover {
      border-color: var(--color-meeting-reflection-border);
      transform: scale(1.08);
    }

    .emoji-btn.selected {
      border-color: var(--color-meeting-reflection-border);
      background: rgba(180, 142, 173, 0.15);
      box-shadow: 0 0 0 2px rgba(180, 142, 173, 0.2);
    }
  `],
})
export class QuickReflectComponent {
  readonly meeting = input.required<Meeting>();
  readonly visible = input.required<boolean>();
  readonly visibleChange = output<boolean>();
  readonly onSave = output<void>();

  private readonly meetingService = inject(MeetingService);

  readonly dimensions = QUICK_REFLECT_DIMENSIONS;
  readonly levels: EmojiLevel[] = ['low', 'medium', 'high'];
  readonly values = signal<QuickReflectionValue>({
    talkTime: null,
    engagement: null,
    tone: null,
    interruptions: null,
    freeformNote: null,
  });
  readonly freeformNote = signal('');
  readonly noteExpanded = signal(false);
  readonly completed = signal(false);

  selectEmoji(dimensionId: string, level: EmojiLevel): void {
    this.values.update(v => ({
      ...v,
      [dimensionId]: v[dimensionId as keyof QuickReflectionValue] === level ? null : level,
    }));
  }

  toggleNoteExpanded(): void {
    this.noteExpanded.update(v => !v);
  }

  expandToFull(): void {
    this.visibleChange.emit(false);
    // The meeting editor page will handle showing the full reflection component
  }

  save(): void {
    const johariMapped = mapQuickReflectToJohari({
      ...this.values(),
      freeformNote: this.freeformNote().trim() || null,
    });

    const reflection: MeetingReflection = {
      selfAssessedTalkTime: johariMapped.selfAssessedTalkTime,
      selfAssessedEngagement: johariMapped.selfAssessedEngagement,
      selfAssessedTone: johariMapped.selfAssessedTone,
      interruptionAwareness: johariMapped.interruptionAwareness,
      freeformReflection: this.freeformNote().trim() || null,
      promptResponses: [],
    };

    this.meetingService.submitReflection(this.meeting().id, reflection);
    this.completed.set(true);

    setTimeout(() => {
      this.visibleChange.emit(false);
      this.onSave.emit();
      this.completed.set(false);
      this.values.set({
        talkTime: null,
        engagement: null,
        tone: null,
        interruptions: null,
        freeformNote: null,
      });
      this.freeformNote.set('');
      this.noteExpanded.set(false);
    }, 1500);
  }
}
