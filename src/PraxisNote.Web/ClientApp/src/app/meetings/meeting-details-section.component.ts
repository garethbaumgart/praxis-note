import {
  Component,
  ChangeDetectionStrategy,
  input,
  output,
  signal,
  computed,
  inject,
  viewChild,
  ElementRef,
  Injector,
  afterNextRender,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DatePickerModule } from 'primeng/datepicker';
import { Select, SelectModule } from 'primeng/select';
import { MeetingTag } from './meeting.model';
import { TagService } from '../tags/tag.service';
import { ALL_TIME_OPTIONS } from './meeting-time.utils';

interface DateOption {
  label: string;
  getValue: () => Date;
}

@Component({
  selector: 'app-meeting-details-section',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, DatePickerModule, SelectModule],
  template: `
    <div class="details-stack">
      <!-- Row 1: Date chips + Time picker -->
      <div>
        <label class="field-label">Date & Time</label>
        <div class="date-time-row">
          <div class="date-chips">
            @for (option of dateOptions; track option.label) {
              <button
                type="button"
                class="date-chip"
                [class.active]="selectedDateChip() === option.label"
                [attr.aria-pressed]="selectedDateChip() === option.label"
                (click)="selectDateOption(option)"
              >
                {{ option.label }}
              </button>
            }
            @if (selectedDateChip() === 'custom' && customDateLabel()) {
              <button type="button" class="date-chip active" [attr.aria-label]="'Selected date: ' + customDateLabel()">
                {{ customDateLabel() }}
              </button>
            }
            <button
              type="button"
              class="date-chip"
              [attr.aria-expanded]="showDatePicker()"
              aria-label="Pick a date"
              (click)="toggleDatePicker()"
            >
              <i class="pi pi-calendar text-[10px]"></i>
              {{ isNewMeeting() ? 'Pick' : 'Change' }}
            </button>
          </div>
          <!-- Time picker (editable combobox) -->
          <div class="time-picker-wrapper">
            <p-select
              #timeSelect
              [options]="allTimeOptions"
              [ngModel]="selectedTimeLabel()"
              (ngModelChange)="onTimeChange($event)"
              (onShow)="onTimeSelectOpen()"
              (onHide)="timeSelectOpen.set(false)"
              [editable]="true"
              [filter]="true"
              filterPlaceholder="Type time..."
              placeholder="Type or pick time..."
              [style]="{ width: '170px' }"
              [class.time-invalid]="timeInputInvalid()"
              appendTo="body"
              ariaLabel="Meeting time"
            />
            @if (timeInputInvalid()) {
              <small class="text-danger text-[10px] mt-0.5 block">Invalid time format</small>
            }
          </div>
        </div>
        @if (showDatePicker()) {
          <div class="mt-2">
            <p-datepicker
              [inline]="true"
              [ngModel]="meetingDate()"
              (ngModelChange)="onDatePickerChange($event)"
              dateFormat="dd M yy"
            />
          </div>
        }
      </div>
      <!-- Row 2: Attendees (full width) -->
      <div>
        <label class="field-label">Attendees</label>
        <input
          class="field-input"
          type="text"
          placeholder="Comma separated names..."
          [value]="attendees()"
          (input)="onAttendeesInput(asInput($event).value)"
          aria-label="Attendees"
        >
      </div>
      <!-- Row 3: Tags -->
      @if (!isNewMeeting()) {
        <div>
          <label class="field-label">Tags</label>
          <div class="tags-section">
            @for (tag of visibleTags(); track tag.id) {
              <span class="tag-badge">
                {{ tag.name }}
                <button
                  type="button"
                  class="tag-badge-remove"
                  (click)="onRemoveTag.emit(tag.id)"
                  [attr.aria-label]="'Remove tag ' + tag.name"
                >
                  <i class="pi pi-times"></i>
                </button>
              </span>
            }
            @if (overflowCount() > 0 && !showTagPicker()) {
              <button
                type="button"
                class="overflow-btn"
                (click)="inlineTagsExpanded.set(true)"
                [attr.aria-label]="'Show ' + overflowCount() + ' more tags'"
              >
                +{{ overflowCount() }}
              </button>
            }
            <!-- Suggested tags inline (sparkle icon + dashed border) -->
            @for (tagName of pendingSuggestedTags(); track tagName) {
              <span class="suggested-tag">
                <i class="pi pi-sparkles" style="font-size: 9px;"></i>
                {{ tagName }}
                <button
                  type="button"
                  class="suggested-tag-accept"
                  (click)="onAcceptSuggestedTag.emit(tagName)"
                  [attr.aria-label]="'Accept tag ' + tagName"
                >
                  <i class="pi pi-check"></i>
                </button>
                <button
                  type="button"
                  class="suggested-tag-dismiss"
                  (click)="onDismissSuggestedTag.emit(tagName)"
                  [attr.aria-label]="'Dismiss tag ' + tagName"
                >
                  <i class="pi pi-times"></i>
                </button>
              </span>
            }
            <!-- Divider if tags exist -->
            @if (meetingTags().length > 0 || pendingSuggestedTags().length > 0) {
              <span class="w-px h-3.5 bg-border shrink-0 mx-0.5"></span>
            }
            <!-- Add tag input/button -->
            @if (showTagPicker()) {
              <div class="tag-input-wrapper">
                <input
                  #tagInput
                  type="text"
                  [placeholder]="meetingTags().length > 0 ? 'Add tag...' : 'Add first tag...'"
                  [value]="tagSearch()"
                  (input)="tagSearch.set(asInput($event).value)"
                  (keydown.enter)="onTagEnter(); $event.preventDefault()"
                  (keydown.escape)="showTagPicker.set(false)"
                  class="tag-input"
                  aria-label="Search or create tag"
                >
                @if (tagSuggestions().length > 0 || canCreateTag()) {
                  <div class="tag-dropdown">
                    @for (tag of tagSuggestions(); track tag.id) {
                      <button
                        type="button"
                        class="tag-dropdown-item"
                        (click)="onAddTag.emit({ id: tag.id, name: tag.name }); closeTagPicker()"
                      >
                        <span [innerHTML]="highlightMatch(tag.name)"></span>
                        <span class="text-foreground-muted">{{ tag.usageCount }}</span>
                      </button>
                    }
                    @if (canCreateTag()) {
                      @if (tagSuggestions().length > 0) {
                        <div class="tag-dropdown-divider"></div>
                      }
                      <button
                        type="button"
                        class="tag-dropdown-item create"
                        (click)="onCreateAndAddTag.emit(tagSearch().trim()); closeTagPicker()"
                      >
                        <i class="pi pi-plus text-[10px] mr-1"></i>
                        Create "{{ tagSearch().trim() }}"
                      </button>
                    }
                  </div>
                }
              </div>
            } @else {
              <button
                type="button"
                class="add-tag-btn"
                (click)="openTagInput()"
                aria-label="Add tag"
              >
                <i class="pi pi-tag text-[9px]"></i>
                <span>Add tag</span>
              </button>
            }
            @if (inlineTagsExpanded() && meetingTags().length > 3 && !showTagPicker()) {
              <button
                type="button"
                class="collapse-btn"
                (click)="inlineTagsExpanded.set(false)"
                aria-label="Show fewer tags"
              >
                <i class="pi pi-chevron-up text-[8px]"></i>
                <span>Less</span>
              </button>
            }
          </div>
        </div>
      }
    </div>
    @if (!isNewMeeting()) {
      <div class="flex items-center gap-2 mt-3 pt-3 border-t border-border">
        <button
          type="button"
          class="flex items-center gap-2 px-3 py-1.5 text-xs rounded-md transition-colors"
          [class.bg-surface-muted]="includeInInsights()"
          [class.text-foreground-secondary]="includeInInsights()"
          [class.bg-warning/10]="!includeInInsights()"
          [class.text-warning]="!includeInInsights()"
          (click)="onToggleExcludeFromInsights.emit()"
          [attr.aria-pressed]="includeInInsights()"
          aria-label="Include in behavioral insights"
        >
          <i class="pi" [class.pi-eye]="includeInInsights()" [class.pi-eye-slash]="!includeInInsights()"></i>
          {{ includeInInsights() ? 'Included in Insights' : 'Excluded from Insights' }}
        </button>
        @if (!includeInInsights()) {
          <span class="text-xs text-foreground-muted">This meeting won't affect your behavioral trends</span>
        }
      </div>
    }
  `,
  styles: [`
    :host { display: block; }

    .details-stack { display: flex; flex-direction: column; gap: 12px; }
    .date-time-row { display: flex; align-items: center; gap: 8px; flex-wrap: wrap; }
    .time-picker-wrapper { flex-shrink: 0; }

    .field-label { font-size: 11px; color: var(--color-text-muted); display: block; margin-bottom: 4px; }
    .field-input {
      width: 100%; background: var(--color-bg-muted); border: none; border-radius: 6px;
      padding: 8px 10px; font-size: 13px; color: var(--color-text-primary); outline: none; box-sizing: border-box;
    }
    .field-input::placeholder { color: var(--color-text-muted); }

    .date-chips { display: flex; flex-wrap: wrap; gap: 6px; flex: 1; }

    .date-chip {
      padding: 4px 10px; font-size: 11px; border-radius: 9999px; border: none;
      cursor: pointer; transition: all .15s; background: var(--color-bg-muted); color: var(--color-text-secondary);
    }
    .date-chip:hover, .date-chip.active { background: var(--color-primary-bg); color: var(--color-primary-text); }

    :host ::ng-deep .p-select.p-select-editable .p-select-label { font-size: 13px; }
    :host ::ng-deep .time-invalid .p-select-label { color: var(--color-danger-base); }
    :host ::ng-deep .p-datepicker { border: none; background: transparent; }

    .tags-section { display: flex; flex-wrap: wrap; align-items: center; gap: 4px; }

    .tag-badge { display: inline-flex; align-items: center; gap: 4px; background: var(--color-tag-bg); color: var(--color-tag-text); font-size: 10px; font-weight: 500; padding: 2px 8px; border-radius: 9999px; height: 18px; }
    .tag-badge-remove { all: unset; position: relative; display: inline-flex; align-items: center; justify-content: center; cursor: pointer; color: var(--color-tag-text); opacity: .6; transition: opacity .1s; }
    .tag-badge-remove::before { content: ''; position: absolute; top: 50%; left: 50%; transform: translate(-50%, -50%); width: 44px; height: 44px; }
    .tag-badge-remove:hover { opacity: 1; }
    .overflow-btn { padding: 2px 6px; border-radius: 9999px; font-size: 10px; background: var(--color-tags-section-bg); color: var(--color-text-muted); border: none; cursor: pointer; transition: all .15s; }
    .overflow-btn:hover { background: var(--color-tags-badge-bg); color: var(--color-tag-text); }
    .tag-input-wrapper { position: relative; flex: 1; min-width: 100px; }
    .tag-input { width: 100%; height: 24px; padding: 0 8px; font-size: 12px; background: var(--color-bg-muted); border-radius: 9999px; border: none; outline: none; color: var(--color-text-primary); }
    .tag-dropdown {
      position: absolute; left: 0; top: calc(100% + 4px); width: 192px;
      background: var(--color-bg-base); border-radius: 8px;
      box-shadow: 0 4px 12px rgba(0,0,0,.15); border: 1px solid var(--color-border-default);
      padding: 4px 0; z-index: 50;
    }

    .tag-dropdown-item {
      all: unset; display: flex; align-items: center; justify-content: space-between;
      width: 100%; padding: 6px 12px; font-size: 12px;
      cursor: pointer; transition: background .15s; box-sizing: border-box;
    }
    .tag-dropdown-item:hover { background: var(--color-bg-subtle); }
    .tag-dropdown-item.create { color: var(--color-primary-solid); }
    .tag-dropdown-divider { border-top: 1px solid var(--color-border-default); margin: 4px 0; }

    .add-tag-btn {
      all: unset; display: inline-flex; align-items: center; gap: 4px; height: 18px;
      padding: 2px 8px; border-radius: 9999px; font-size: 10px; font-weight: 500;
      color: var(--color-text-muted); background: var(--color-bg-muted); cursor: pointer; transition: all .15s;
    }
    .add-tag-btn:hover { color: var(--color-tag-text); background: var(--color-tags-badge-bg); }

    .collapse-btn { all: unset; display: flex; align-items: center; gap: 2px; margin-left: auto; padding: 2px 6px; border-radius: 9999px; font-size: 10px; background: var(--color-tags-section-bg); color: var(--color-text-muted); cursor: pointer; transition: all .15s; }
    .collapse-btn:hover { background: var(--color-tags-collapsed-bg); }

    .suggested-tag {
      display: inline-flex; align-items: center; gap: 4px;
      background: var(--color-tag-bg); color: var(--color-tag-text);
      font-size: 11px; font-weight: 500; padding: 2px 8px;
      border-radius: 9999px; border: 1px dashed var(--color-border-default); height: 18px;
    }
    .suggested-tag-accept, .suggested-tag-dismiss {
      all: unset; display: inline-flex; align-items: center; justify-content: center;
      cursor: pointer; font-size: 10px; padding: 1px; border-radius: 50%;
    }
    .suggested-tag-accept { color: var(--color-done-text); }
    .suggested-tag-dismiss { color: var(--color-text-muted); }
    .suggested-tag-accept:hover { background: var(--color-done-bg); }
    .suggested-tag-dismiss:hover { color: var(--color-danger-base); }
  `],
})
export class MeetingDetailsSectionComponent {
  private readonly tagService = inject(TagService);
  private readonly injector = inject(Injector);

  // Inputs from parent
  readonly isNewMeeting = input.required<boolean>();
  readonly meetingDate = input.required<Date | null>();
  readonly attendees = input.required<string>();
  readonly meetingTags = input.required<MeetingTag[]>();
  readonly pendingSuggestedTags = input.required<string[]>();
  readonly includeInInsights = input.required<boolean>();
  readonly selectedDateChip = input.required<string | null>();
  readonly customDateLabel = input.required<string | null>();
  readonly selectedTimeLabel = input.required<string>();
  readonly timeInputInvalid = input.required<boolean>();
  readonly showDatePicker = input.required<boolean>();

  // Outputs to parent
  readonly onAttendeesChange = output<string>();
  readonly onDateOptionSelect = output<DateOption>();
  readonly onDatePickerToggle = output<void>();
  readonly onDatePickerSelect = output<Date>();
  readonly onTimeChanged = output<string>();
  readonly onRemoveTag = output<string>();
  readonly onAddTag = output<MeetingTag>();
  readonly onCreateAndAddTag = output<string>();
  readonly onAcceptSuggestedTag = output<string>();
  readonly onDismissSuggestedTag = output<string>();
  readonly onToggleExcludeFromInsights = output<void>();
  readonly onCloseDatePicker = output<void>();

  // Local state
  readonly showTagPicker = signal(false);
  readonly inlineTagsExpanded = signal(false);
  readonly tagSearch = signal('');
  readonly timeSelectOpen = signal(false);

  readonly tagInput = viewChild<ElementRef<HTMLInputElement>>('tagInput');
  readonly timeSelect = viewChild<Select>('timeSelect');

  readonly allTimeOptions = ALL_TIME_OPTIONS;

  readonly dateOptions: DateOption[] = [
    { label: 'Today', getValue: () => new Date() },
    { label: 'Tomorrow', getValue: () => this.addDays(new Date(), 1) },
    { label: 'Next Week', getValue: () => this.addDays(new Date(), 7) },
  ];

  private readonly MAX_VISIBLE_TAGS = 3;

  readonly visibleTags = computed(() => {
    const tags = this.meetingTags();
    const expanded = this.inlineTagsExpanded();
    const adding = this.showTagPicker();
    if (expanded || adding) return tags;
    return tags.slice(0, this.MAX_VISIBLE_TAGS);
  });

  readonly overflowCount = computed(() => {
    const total = this.meetingTags().length;
    const expanded = this.inlineTagsExpanded();
    const adding = this.showTagPicker();
    if (expanded || adding) return 0;
    return Math.max(0, total - this.MAX_VISIBLE_TAGS);
  });

  readonly existingTagIds = computed(() => this.meetingTags().map(t => t.id));

  readonly tagSuggestions = computed(() => {
    const query = this.tagSearch().toLowerCase().trim();
    const existingIds = this.existingTagIds();
    const allTags = this.tagService.tags();
    if (!query) return allTags.filter(t => !existingIds.includes(t.id));
    return allTags
      .filter(t => !existingIds.includes(t.id) && t.name.toLowerCase().includes(query))
      .sort((a, b) => b.usageCount - a.usageCount);
  });

  readonly canCreateTag = computed(() => {
    const query = this.tagSearch().trim();
    const suggestions = this.tagSuggestions();
    return query.length >= 2 && !suggestions.some(t => t.name.toLowerCase() === query.toLowerCase());
  });

  onTimeSelectOpen(): void {
    this.onCloseDatePicker.emit();
    this.timeSelectOpen.set(true);
  }

  selectDateOption(option: DateOption): void {
    this.onDateOptionSelect.emit(option);
  }

  toggleDatePicker(): void {
    this.timeSelect()?.hide();
    this.onDatePickerToggle.emit();
  }

  onDatePickerChange(date: Date | null): void {
    if (!date) return;
    this.onDatePickerSelect.emit(date);
  }

  onTimeChange(value: string): void {
    this.onTimeChanged.emit(value);
  }

  onAttendeesInput(value: string): void {
    this.onAttendeesChange.emit(value);
  }

  openTagInput(): void {
    this.showTagPicker.set(true);
    this.tagSearch.set('');
    afterNextRender(() => {
      this.tagInput()?.nativeElement.focus();
    }, { injector: this.injector });
  }

  closeTagPicker(): void {
    this.showTagPicker.set(false);
    this.tagSearch.set('');
  }

  onTagEnter(): void {
    const query = this.tagSearch().trim();
    const suggestions = this.tagSuggestions();

    const exactMatch = suggestions.find(t => t.name.toLowerCase() === query.toLowerCase());
    if (exactMatch) {
      this.onAddTag.emit({ id: exactMatch.id, name: exactMatch.name });
      this.closeTagPicker();
      return;
    }
    if (this.canCreateTag()) {
      this.onCreateAndAddTag.emit(query);
      this.closeTagPicker();
      return;
    }
    if (suggestions.length === 1) {
      this.onAddTag.emit({ id: suggestions[0].id, name: suggestions[0].name });
      this.closeTagPicker();
    }
  }

  highlightMatch(tagName: string): string {
    const query = this.tagSearch().toLowerCase().trim();
    if (!query) return this.escapeHtml(tagName);
    const lowerName = tagName.toLowerCase();
    const index = lowerName.indexOf(query);
    if (index === -1) return this.escapeHtml(tagName);
    const before = tagName.slice(0, index);
    const match = tagName.slice(index, index + query.length);
    const after = tagName.slice(index + query.length);
    return `${this.escapeHtml(before)}<mark class="search-highlight">${this.escapeHtml(match)}</mark>${this.escapeHtml(after)}`;
  }

  /** Whether the tag picker overlay is currently open */
  isTagPickerOpen(): boolean {
    return this.showTagPicker();
  }

  /** Whether the time select overlay is currently open */
  isTimeSelectOpen(): boolean {
    return this.timeSelectOpen();
  }

  /** Close the time select overlay */
  hideTimeSelect(): void {
    this.timeSelect()?.hide();
  }

  /** Close the tag picker */
  closeTagPickerOverlay(): void {
    this.showTagPicker.set(false);
  }

  asInput(event: Event): HTMLInputElement {
    return event.target as HTMLInputElement;
  }

  private addDays(date: Date, days: number): Date {
    const result = new Date(date);
    result.setDate(result.getDate() + days);
    return result;
  }

  private escapeHtml(text: string): string {
    const map: { [key: string]: string } = {
      '&': '&amp;',
      '<': '&lt;',
      '>': '&gt;',
      '"': '&quot;',
      "'": '&#39;',
      '/': '&#x2F;',
    };
    return text.replace(/[&<>"'/]/g, (char) => map[char]);
  }
}
