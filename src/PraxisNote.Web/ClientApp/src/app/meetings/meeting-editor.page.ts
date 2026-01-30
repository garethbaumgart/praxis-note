import {
  Component,
  ChangeDetectionStrategy,
  inject,
  signal,
  computed,
  effect,
  OnInit,
  OnDestroy,
  HostListener,
  ElementRef,
  viewChild,
  afterNextRender,
  Injector,
} from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { Subject, debounceTime, takeUntil } from 'rxjs';
import { DatePickerModule } from 'primeng/datepicker';
import { SelectModule } from 'primeng/select';
import { Meeting, MeetingTag, ActionItemStatus } from './meeting.model';
import { MeetingService } from './meeting.service';
import { MeetingAnalysisComponent } from './meeting-analysis.component';
import { AudioRecorderService } from './audio-recorder.service';
import { ToastService } from '../shared/services/toast.service';
import { TagService } from '../tasks/tag.service';
import { Tag } from '../tasks/tag.model';

interface DateOption {
  label: string;
  getValue: () => Date;
}

interface TimeOption {
  label: string;
  value: number;
}

@Component({
  selector: 'app-meeting-editor-page',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, DatePickerModule, SelectModule, MeetingAnalysisComponent],
  template: `
    <div class="meeting-editor-page">
      <!-- Header bar -->
      <header class="header">
        <div class="breadcrumb">
          <button
            type="button"
            class="back-link"
            (click)="navigateBack()"
            aria-label="Back to meetings"
          >
            <i class="pi pi-arrow-left"></i>
            <span>Meetings</span>
          </button>
          <span class="separator">/</span>
          <span class="current-meeting">{{ displayTitle() }}</span>
        </div>
        <div class="actions">
          <span class="save-status" [class.saving]="isSaving()">
            @if (isSaving()) {
              <i class="pi pi-spin pi-spinner"></i>
              <span>Saving...</span>
            } @else if (lastSaved()) {
              <i class="pi pi-check"></i>
              <span>Saved</span>
            }
          </span>
          @if (meetingId()) {
            <button
              type="button"
              class="action-btn delete-btn"
              (click)="deleteMeeting()"
              aria-label="Delete meeting"
              title="Delete meeting"
            >
              <i class="pi pi-trash"></i>
            </button>
          }
        </div>
      </header>

      <!-- Scrollable content -->
      <main class="editor-container">
        @if (loading()) {
          <div class="loading">
            <i class="pi pi-spin pi-spinner text-2xl text-foreground-muted"></i>
          </div>
        } @else if (notFound()) {
          <div class="not-found">
            <i class="pi pi-exclamation-triangle text-4xl text-foreground-muted mb-4"></i>
            <p class="text-foreground-secondary">Meeting not found</p>
            <button
              type="button"
              class="mt-4 px-4 py-2 text-sm bg-accent-solid text-white rounded-md"
              (click)="navigateBack()"
            >
              Back to Meetings
            </button>
          </div>
        } @else {
          <div class="editor-wrapper">
            <!-- Title -->
            <input
              class="title-input"
              type="text"
              placeholder="Meeting title..."
              [value]="title()"
              (input)="onTitleChange(asInput($event).value)"
              aria-label="Meeting title"
            >

            <!-- Details Section - Cyan border -->
            <div class="section-card details-card">
              <div class="section-header details-header">
                <span><i class="pi pi-info-circle"></i> Details</span>
              </div>
              <div class="details-grid">
                <div>
                  <label class="field-label">Date & Time</label>
                  <!-- Date chips -->
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
                      (click)="showDatePicker.set(!showDatePicker())"
                    >
                      <i class="pi pi-calendar text-[10px]"></i>
                      {{ isNewMeeting() ? 'Pick' : 'Change' }}
                    </button>
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
                  <!-- Time selectors -->
                  <div class="time-selectors">
                    <p-select
                      [options]="hourOptions"
                      [ngModel]="selectedHour()"
                      (ngModelChange)="selectedHour.set($event)"
                      optionLabel="label"
                      optionValue="value"
                      [style]="{ width: '80px' }"
                      appendTo="body"
                      ariaLabel="Meeting hour"
                    />
                    <span class="text-foreground-muted font-medium">:</span>
                    <p-select
                      [options]="minuteOptions()"
                      [ngModel]="selectedMinute()"
                      (ngModelChange)="selectedMinute.set($event)"
                      optionLabel="label"
                      optionValue="value"
                      [style]="{ width: '80px' }"
                      appendTo="body"
                      ariaLabel="Meeting minute"
                    />
                    <p-select
                      [options]="periodOptions"
                      [ngModel]="selectedPeriod()"
                      (ngModelChange)="selectedPeriod.set($event)"
                      [style]="{ width: '78px', minWidth: '78px' }"
                      appendTo="body"
                      ariaLabel="AM or PM"
                    />
                  </div>
                </div>
                <div>
                  <label class="field-label">Attendees</label>
                  <input
                    class="field-input"
                    type="text"
                    placeholder="Comma separated names..."
                    [value]="attendees()"
                    (input)="onAttendeesChange(asInput($event).value)"
                    aria-label="Attendees"
                  >
                </div>
              </div>
            </div>

            <!-- Transcript Section - Blue border -->
            @if (!isNewMeeting()) {
              <div class="section-card transcript-card">
                <div class="section-header transcript-header">
                  <span><i class="pi pi-file-edit"></i> Transcript</span>
                  <div class="section-actions">
                    @if (isTranscribing()) {
                      <span class="flex items-center gap-2 text-xs text-foreground-muted">
                        <i class="pi pi-spin pi-spinner text-xs"></i>
                        Transcribing...
                      </span>
                    } @else if (!recorder.isActive()) {
                      <input
                        #audioFileInput
                        type="file"
                        accept=".mp3,.mp4,.mpeg,.mpga,.m4a,.wav,.webm"
                        class="hidden"
                        (change)="onAudioFileSelected($event)"
                        aria-label="Upload audio file for transcription"
                      />
                      <button
                        type="button"
                        class="record-btn"
                        (click)="startRecording()"
                        aria-label="Record audio from microphone"
                      >
                        <i class="pi pi-microphone"></i> Record
                      </button>
                      <button
                        type="button"
                        class="upload-btn"
                        (click)="audioFileInput.click()"
                        aria-label="Upload audio file"
                      >
                        <i class="pi pi-upload"></i> Upload
                      </button>
                    }
                  </div>
                </div>

                <!-- Audio Recording UI -->
                @if (recorder.isActive()) {
                  <div class="recording-area">
                    <div class="flex items-center justify-between mb-3">
                      <div class="flex items-center gap-2">
                        <span class="w-3 h-3 bg-danger rounded-full recording-pulse" aria-hidden="true"></span>
                        <span class="text-sm font-medium text-foreground">
                          {{ recorder.isPaused() ? 'Paused' : 'Recording' }}
                        </span>
                      </div>
                      <span class="text-sm text-foreground-muted font-mono" aria-label="Recording duration">{{ recorder.formattedTime() }}</span>
                    </div>
                    <!-- Audio level bars -->
                    <div class="flex items-end gap-0.5 h-8 mb-3" aria-hidden="true">
                      @for (level of recorder.audioLevels(); track $index) {
                        <div
                          class="audio-bar flex-1 rounded-sm"
                          [class.bg-accent-solid]="level > 0.05"
                          [class.bg-surface-muted]="level <= 0.05"
                          [style.height.%]="Math.max(level * 100, 10)"
                        ></div>
                      }
                    </div>
                    <div class="flex justify-center gap-2">
                      @if (recorder.isRecording()) {
                        <button
                          type="button"
                          class="px-3 py-1.5 text-xs bg-surface-muted text-foreground-secondary rounded-md hover:bg-surface-muted/80 transition-colors"
                          (click)="recorder.pause()"
                          aria-label="Pause recording"
                        >
                          <i class="pi pi-pause mr-1"></i>Pause
                        </button>
                      } @else {
                        <button
                          type="button"
                          class="px-3 py-1.5 text-xs bg-surface-muted text-foreground-secondary rounded-md hover:bg-surface-muted/80 transition-colors"
                          (click)="recorder.resume()"
                          aria-label="Resume recording"
                        >
                          <i class="pi pi-play mr-1"></i>Resume
                        </button>
                      }
                      <button
                        type="button"
                        class="px-3 py-1.5 text-xs bg-danger text-white rounded-md hover:opacity-90 transition-opacity"
                        (click)="stopRecording()"
                        aria-label="Stop recording"
                      >
                        <i class="pi pi-stop-circle mr-1"></i>Stop
                      </button>
                    </div>
                  </div>
                }

                @if (recorder.error()) {
                  <p class="text-xs text-danger mt-2">{{ recorder.error() }}</p>
                }

                @if (showTabWarning()) {
                  <div class="flex items-center gap-2 text-xs text-foreground-muted bg-surface-muted rounded px-3 py-1.5 mt-2">
                    <i class="pi pi-info-circle text-xs"></i>
                    <span>Keep this tab active for best recording quality.</span>
                  </div>
                }

                <!-- Transcript textarea -->
                <textarea
                  class="transcript-textarea"
                  placeholder="Paste transcript here..."
                  [value]="transcript()"
                  (input)="onTranscriptChange(asTextarea($event).value)"
                  aria-label="Meeting transcript"
                  rows="6"
                ></textarea>
                <div class="flex justify-between items-center mt-1">
                  <div class="text-xs text-foreground-muted">
                    @if (audioFileName()) {
                      {{ audioFileName() }}
                    }
                  </div>
                  <span class="text-xs text-foreground-muted">{{ transcript().length }} characters</span>
                </div>
              </div>

              <!-- AI Analysis Section - Green border -->
              <div class="section-card analysis-card">
                <div class="section-header analysis-header">
                  <span><i class="pi pi-sparkles"></i> AI Analysis</span>
                </div>
                @if (currentMeeting()) {
                  <app-meeting-analysis
                    [meeting]="currentMeeting()!"
                    [actionItemStatuses]="actionItemStatuses()"
                    [promotingIds]="promotingIds()"
                    (onAnalyze)="analyze()"
                    (onToggleActionItem)="toggleActionItem($event)"
                    (onPromoteActionItem)="promoteActionItem($event)"
                    (onNavigateToTask)="navigateToTask($event)"
                  />
                } @else {
                  <div class="empty-analysis">
                    Click "Generate" to create an AI summary of this meeting
                  </div>
                }
              </div>
            }
          </div>
        }
      </main>

      <!-- Footer with tags -->
      <footer class="footer">
        @if (currentMeeting()) {
          <span class="text-xs text-foreground-muted">
            Last edited {{ formatDate(currentMeeting()!.updatedAt) }}
          </span>
        }
        <span class="flex-1"></span>
        @if (currentMeeting()) {
          <div class="tags-section">
            @for (tag of visibleTags(); track tag.id) {
              <span class="tag-badge">
                {{ tag.name }}
                <button
                  type="button"
                  class="tag-badge-remove"
                  (click)="removeTag(tag.id)"
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
                        (click)="addTag({ id: tag.id, name: tag.name })"
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
                        (click)="createAndAddTag(tagSearch().trim())"
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
                <i class="pi pi-plus text-[9px]"></i>
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
        }
      </footer>
    </div>
  `,
  styles: [`
    .meeting-editor-page {
      display: flex;
      flex-direction: column;
      height: 100%;
      background: var(--color-bg-base);
    }

    :host {
      display: block;
      height: 100%;
    }

    /* Header */
    .header {
      display: flex;
      align-items: center;
      justify-content: space-between;
      padding: 0.75rem 1.5rem;
      border-bottom: 1px solid var(--color-border-default);
      background: var(--color-bg-subtle);
    }

    .breadcrumb {
      display: flex;
      align-items: center;
      gap: 0.5rem;
      font-size: 0.8125rem;
    }

    .back-link {
      display: flex;
      align-items: center;
      gap: 0.375rem;
      color: var(--color-primary-solid);
      background: none;
      border: none;
      cursor: pointer;
      font-size: 0.8125rem;
      padding: 0.25rem 0.5rem;
      margin: -0.25rem -0.5rem;
      border-radius: 0.25rem;
      transition: background 0.15s;
    }

    .back-link:hover {
      background: var(--color-bg-subtle);
    }

    .separator {
      color: var(--color-text-muted);
    }

    .current-meeting {
      font-weight: 600;
      color: var(--color-text-secondary);
      max-width: 300px;
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
    }

    .actions {
      display: flex;
      align-items: center;
      gap: 0.5rem;
    }

    .save-status {
      display: flex;
      align-items: center;
      gap: 0.375rem;
      font-size: 0.75rem;
      color: var(--color-done-text);
      padding-right: 0.75rem;
    }

    .save-status.saving {
      color: var(--color-primary-solid);
    }

    .action-btn {
      display: flex;
      align-items: center;
      justify-content: center;
      width: 2rem;
      height: 2rem;
      border: 1px solid var(--color-border-default);
      background: var(--color-bg-subtle);
      border-radius: 0.375rem;
      cursor: pointer;
      transition: all 0.15s;
    }

    .delete-btn {
      color: var(--color-danger-base);
    }

    .action-btn:hover {
      background: var(--color-bg-subtle);
    }

    /* Main content */
    .editor-container {
      flex: 1;
      overflow: auto;
    }

    .loading,
    .not-found {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      height: 100%;
    }

    .editor-wrapper {
      max-width: 800px;
      margin: 0 auto;
      width: 100%;
      padding: 1.5rem;
      box-sizing: border-box;
    }

    @media (max-width: 768px) {
      .editor-wrapper {
        padding: 1rem;
      }
    }

    /* Title */
    .title-input {
      font-size: 22px;
      font-weight: 700;
      border: none;
      background: transparent;
      padding: 0;
      width: 100%;
      outline: none;
      color: var(--color-text-primary);
      margin-bottom: 20px;
    }

    .title-input::placeholder {
      color: var(--color-text-muted);
    }

    /* Section cards */
    .section-card {
      background: var(--color-bg-subtle);
      border: 1px solid var(--color-border-default);
      border-radius: 8px;
      padding: 16px;
      margin-bottom: 12px;
    }

    .details-card {
      border-left: 3px solid var(--color-meeting-details-border);
    }

    .transcript-card {
      border-left: 3px solid var(--color-meeting-transcript-border);
    }

    .analysis-card {
      border-left: 3px solid var(--color-meeting-analysis-border);
    }

    /* Section headers */
    .section-header {
      font-size: 12px;
      font-weight: 600;
      text-transform: uppercase;
      margin-bottom: 12px;
      display: flex;
      justify-content: space-between;
      align-items: center;
    }

    .details-header {
      color: var(--color-todo-text);
    }

    .transcript-header {
      color: var(--color-primary-text);
    }

    .analysis-header {
      color: var(--color-done-text);
    }

    .section-actions {
      display: flex;
      gap: 6px;
    }

    /* Record & Upload buttons */
    .record-btn {
      background: var(--color-todo-bg);
      color: var(--color-todo-text);
      border: 1px solid var(--color-todo-border);
      border-radius: 5px;
      padding: 4px 10px;
      font-size: 11px;
      cursor: pointer;
      transition: all 0.15s;
    }

    .record-btn:hover {
      background: var(--color-todo-bg-hover);
    }

    .upload-btn {
      background: var(--color-bg-subtle);
      color: var(--color-text-secondary);
      border: 1px solid var(--color-border-default);
      border-radius: 5px;
      padding: 4px 10px;
      font-size: 11px;
      cursor: pointer;
      transition: all 0.15s;
    }

    .upload-btn:hover {
      background: var(--color-bg-muted);
    }

    /* Details grid */
    .details-grid {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 12px;
    }

    @media (max-width: 640px) {
      .details-grid {
        grid-template-columns: 1fr;
      }
    }

    .field-label {
      font-size: 11px;
      color: var(--color-text-muted);
      display: block;
      margin-bottom: 4px;
    }

    .field-input {
      width: 100%;
      background: var(--color-bg-muted);
      border: none;
      border-radius: 6px;
      padding: 8px 10px;
      font-size: 13px;
      color: var(--color-text-primary);
      outline: none;
      box-sizing: border-box;
    }

    .field-input::placeholder {
      color: var(--color-text-muted);
    }

    /* Date chips */
    .date-chips {
      display: flex;
      flex-wrap: wrap;
      gap: 6px;
      margin-bottom: 8px;
    }

    .date-chip {
      padding: 4px 10px;
      font-size: 11px;
      border-radius: 9999px;
      border: none;
      cursor: pointer;
      transition: all 0.15s;
      background: var(--color-bg-muted);
      color: var(--color-text-secondary);
    }

    .date-chip:hover,
    .date-chip.active {
      background: var(--color-primary-bg);
      color: var(--color-primary-text);
    }

    /* Time selectors */
    .time-selectors {
      display: flex;
      align-items: center;
      gap: 4px;
    }

    /* Transcript textarea */
    .transcript-textarea {
      width: 100%;
      background: var(--color-bg-muted);
      border: none;
      border-radius: 6px;
      padding: 12px;
      font-size: 13px;
      line-height: 1.7;
      color: var(--color-text-primary);
      resize: vertical;
      outline: none;
      box-sizing: border-box;
      min-height: 80px;
    }

    .transcript-textarea::placeholder {
      color: var(--color-text-muted);
    }

    /* Recording area */
    .recording-area {
      background: var(--color-bg-muted);
      border-radius: 8px;
      padding: 16px;
      margin-bottom: 12px;
    }

    @keyframes pulse-recording {
      0%, 100% { opacity: 1; }
      50% { opacity: 0.4; }
    }

    .recording-pulse {
      animation: pulse-recording 1.5s ease-in-out infinite;
    }

    .audio-bar {
      transition: height 0.1s ease-out;
    }

    /* Empty analysis */
    .empty-analysis {
      background: var(--color-bg-muted);
      border-radius: 6px;
      padding: 12px;
      font-size: 13px;
      color: var(--color-text-muted);
      text-align: center;
    }

    /* Date picker override */
    :host ::ng-deep .p-datepicker {
      border: none;
      background: transparent;
    }

    /* Footer */
    .footer {
      display: flex;
      align-items: center;
      padding: 0.5rem 1.5rem;
      border-top: 1px solid var(--color-border-default);
      background: var(--color-bg-base);
    }

    .tags-section {
      display: flex;
      flex-wrap: wrap;
      align-items: center;
      gap: 0.25rem;
    }

    .tag-badge {
      display: inline-flex;
      align-items: center;
      gap: 0.25rem;
      background: var(--color-tag-bg);
      color: var(--color-tag-text);
      font-size: 10px;
      font-weight: 500;
      padding: 2px 8px;
      border-radius: 9999px;
      height: 18px;
    }

    .tag-badge-remove {
      all: unset;
      display: inline-flex;
      align-items: center;
      justify-content: center;
      cursor: pointer;
      color: var(--color-tag-text);
      opacity: 0.6;
      transition: opacity 0.15s;
    }

    .tag-badge-remove:hover {
      opacity: 1;
    }

    .overflow-btn {
      padding: 2px 6px;
      border-radius: 9999px;
      font-size: 10px;
      background: var(--color-tags-section-bg);
      color: var(--color-text-muted);
      border: none;
      cursor: pointer;
      transition: all 0.15s;
    }

    .overflow-btn:hover {
      background: var(--color-tags-badge-bg);
      color: var(--color-tag-text);
    }

    .tag-input-wrapper {
      position: relative;
      flex: 1;
      min-width: 100px;
    }

    .tag-input {
      width: 100%;
      height: 24px;
      padding: 0 8px;
      font-size: 12px;
      background: var(--color-bg-muted);
      border-radius: 9999px;
      border: none;
      outline: none;
      color: var(--color-text-primary);
    }

    .tag-dropdown {
      position: absolute;
      left: 0;
      bottom: calc(100% + 4px);
      width: 192px;
      background: var(--color-bg-base);
      border-radius: 8px;
      box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
      border: 1px solid var(--color-border-default);
      padding: 4px 0;
      z-index: 50;
    }

    .tag-dropdown-item {
      all: unset;
      display: flex;
      align-items: center;
      justify-content: space-between;
      width: 100%;
      padding: 6px 12px;
      font-size: 12px;
      cursor: pointer;
      transition: background 0.15s;
      box-sizing: border-box;
    }

    .tag-dropdown-item:hover {
      background: var(--color-bg-subtle);
    }

    .tag-dropdown-item.create {
      color: var(--color-primary-solid);
    }

    .tag-dropdown-divider {
      border-top: 1px solid var(--color-border-default);
      margin: 4px 0;
    }

    .add-tag-btn {
      all: unset;
      display: flex;
      align-items: center;
      justify-content: center;
      width: 20px;
      height: 20px;
      border-radius: 9999px;
      color: var(--color-text-muted);
      opacity: 0.3;
      cursor: pointer;
      transition: all 0.15s;
    }

    .add-tag-btn:hover {
      color: var(--color-tag-text);
      background: var(--color-tags-badge-bg);
      opacity: 1;
    }

    .collapse-btn {
      all: unset;
      display: flex;
      align-items: center;
      gap: 2px;
      margin-left: auto;
      padding: 2px 6px;
      border-radius: 9999px;
      font-size: 10px;
      background: var(--color-tags-section-bg);
      color: var(--color-text-muted);
      cursor: pointer;
      transition: all 0.15s;
    }

    .collapse-btn:hover {
      background: var(--color-tags-collapsed-bg);
    }
  `],
})
export class MeetingEditorPage implements OnInit, OnDestroy {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly meetingService = inject(MeetingService);
  private readonly tagService = inject(TagService);
  private readonly toast = inject(ToastService);
  private readonly injector = inject(Injector);
  readonly recorder = inject(AudioRecorderService);

  /** Expose Math for template */
  readonly Math = Math;

  private readonly destroy$ = new Subject<void>();
  private readonly metadataChange$ = new Subject<void>();
  private readonly transcriptChange$ = new Subject<void>();

  // Core state
  readonly loading = signal(true);
  readonly notFound = signal(false);
  readonly isNewMeeting = signal(false);
  readonly isSaving = signal(false);
  readonly lastSaved = signal(false);
  readonly meetingId = signal<string | null>(null);

  // Form state
  readonly title = signal('');
  readonly meetingDate = signal<Date | null>(null);
  readonly attendees = signal('');
  readonly transcript = signal('');
  readonly audioFileName = signal<string | null>(null);
  readonly isTranscribing = signal(false);
  readonly showTabWarning = signal(false);

  // Date selection
  readonly selectedDateChip = signal<string | null>('Tomorrow');
  readonly customDateLabel = signal<string | null>(null);
  readonly showDatePicker = signal(false);

  // Time selection
  readonly selectedHour = signal(10);
  readonly selectedMinute = signal(0);
  readonly selectedPeriod = signal<'AM' | 'PM'>('AM');

  // Analysis state
  readonly actionItemStatuses = signal<ActionItemStatus[]>([]);
  readonly promotingIds = signal<Set<string>>(new Set());

  // Tag state
  readonly showTagPicker = signal(false);
  readonly inlineTagsExpanded = signal(false);
  readonly tagSearch = signal('');
  readonly tagInput = viewChild<ElementRef<HTMLInputElement>>('tagInput');

  private isDestroyed = false;
  private pollingTimeoutId: ReturnType<typeof setTimeout> | null = null;

  readonly currentMeeting = computed(() => {
    const id = this.meetingId();
    if (!id) return null;
    return this.meetingService.meetings().find(m => m.id === id) ?? null;
  });

  readonly displayTitle = computed(() => this.title() || 'Untitled');

  // Track action items count for status loading
  private readonly actionItemsCount = computed(() =>
    this.currentMeeting()?.actionItems.length ?? 0
  );

  // Date options
  readonly dateOptions: DateOption[] = [
    { label: 'Today', getValue: () => new Date() },
    { label: 'Tomorrow', getValue: () => this.addDays(new Date(), 1) },
    { label: 'Next Week', getValue: () => this.addDays(new Date(), 7) },
  ];

  // Time options
  readonly hourOptions: TimeOption[] = [
    { label: '12', value: 12 },
    { label: '1', value: 1 },
    { label: '2', value: 2 },
    { label: '3', value: 3 },
    { label: '4', value: 4 },
    { label: '5', value: 5 },
    { label: '6', value: 6 },
    { label: '7', value: 7 },
    { label: '8', value: 8 },
    { label: '9', value: 9 },
    { label: '10', value: 10 },
    { label: '11', value: 11 },
  ];

  readonly defaultMinuteOptions: TimeOption[] = [
    { label: '00', value: 0 },
    { label: '05', value: 5 },
    { label: '10', value: 10 },
    { label: '15', value: 15 },
    { label: '20', value: 20 },
    { label: '25', value: 25 },
    { label: '30', value: 30 },
    { label: '35', value: 35 },
    { label: '40', value: 40 },
    { label: '45', value: 45 },
    { label: '50', value: 50 },
    { label: '55', value: 55 },
  ];

  readonly minuteOptions = signal<TimeOption[]>(this.defaultMinuteOptions);
  readonly periodOptions = ['AM', 'PM'];

  // Tag computed properties
  readonly meetingTags = computed(() => this.currentMeeting()?.tags ?? []);

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

  constructor() {
    // Reload action item statuses when action items count changes
    let lastActionItemCount = -1;
    effect(() => {
      const count = this.actionItemsCount();
      const id = this.meetingId();
      if (id && count > 0 && count !== lastActionItemCount) {
        lastActionItemCount = count;
        this.loadActionItemStatuses();
      }
    });

    // Sync transcript when transcription completes
    effect(() => {
      const meeting = this.currentMeeting();
      if (meeting && meeting.status !== 'Processing' && this.isTranscribing()) {
        this.isTranscribing.set(false);
        if (meeting.transcriptContent) {
          this.transcript.set(meeting.transcriptContent);
        }
      }
    });

    // Update meetingDate when time selection changes
    effect(() => {
      const hour = this.selectedHour();
      const minute = this.selectedMinute();
      const period = this.selectedPeriod();
      const currentDate = this.meetingDate();

      if (currentDate) {
        const newDate = new Date(currentDate);
        const hour24 = this.toHour24(hour, period);
        newDate.setHours(hour24, minute, 0, 0);
        if (newDate.getTime() !== currentDate.getTime()) {
          this.meetingDate.set(newDate);
          this.lastSaved.set(false);
          this.metadataChange$.next();
        }
      }
    });
  }

  ngOnInit(): void {
    // Load tags
    if (this.tagService.tags().length === 0) {
      this.tagService.loadTags();
    }

    // Ensure meetings are loaded
    if (!this.meetingService.initialLoadComplete()) {
      this.meetingService.loadMeetings();
    }

    // Auto-save metadata with debounce
    this.metadataChange$
      .pipe(debounceTime(1000), takeUntil(this.destroy$))
      .subscribe(() => this.saveMetadata());

    // Auto-save transcript with debounce (longer delay for larger content)
    this.transcriptChange$
      .pipe(debounceTime(2000), takeUntil(this.destroy$))
      .subscribe(() => this.saveTranscript());

    // Get meeting ID from route
    this.route.paramMap.pipe(takeUntil(this.destroy$)).subscribe(params => {
      const id = params.get('id');
      if (id === 'new') {
        this.initNewMeeting();
      } else if (id) {
        this.loadMeeting(id);
      } else {
        this.notFound.set(true);
        this.loading.set(false);
      }
    });
  }

  ngOnDestroy(): void {
    this.isDestroyed = true;
    this.cancelPolling();
    this.recorder.discard();
    this.metadataChange$.complete();
    this.transcriptChange$.complete();
    this.destroy$.next();
    this.destroy$.complete();
  }

  private cancelPolling(): void {
    if (this.pollingTimeoutId !== null) {
      clearTimeout(this.pollingTimeoutId);
      this.pollingTimeoutId = null;
    }
  }

  @HostListener('document:keydown', ['$event'])
  onKeydown(event: KeyboardEvent): void {
    // Save with Cmd/Ctrl+S
    if ((event.metaKey || event.ctrlKey) && event.key === 's') {
      event.preventDefault();
      this.saveMetadata();
      this.saveTranscript();
    }

    // Go back with Escape when not typing
    if (event.key === 'Escape' && !this.isInEditableElement(event)) {
      this.navigateBack();
    }
  }

  @HostListener('window:beforeunload', ['$event'])
  onBeforeUnload(event: BeforeUnloadEvent): void {
    if (this.recorder.isActive()) {
      event.preventDefault();
      event.returnValue = '';
    }
  }

  private isInEditableElement(event: KeyboardEvent): boolean {
    const target = event.target as HTMLElement | null;
    if (!target) return false;
    const editableElement = target.closest(
      'input, textarea, [contenteditable=""], [contenteditable="true"]'
    ) as HTMLElement | null;
    if (editableElement) return true;
    return target.isContentEditable;
  }

  private initNewMeeting(): void {
    this.isNewMeeting.set(true);
    this.loading.set(false);
    this.notFound.set(false);
    this.meetingId.set(null);
    this.lastSaved.set(false);
    this.isSaving.set(false);
    this.title.set('');
    this.attendees.set('');
    this.transcript.set('');
    this.audioFileName.set(null);
    this.isTranscribing.set(false);
    this.showTabWarning.set(false);
    this.actionItemStatuses.set([]);
    this.promotingIds.set(new Set());

    // Default to tomorrow at 10 AM
    const tomorrow = this.addDays(new Date(), 1);
    tomorrow.setHours(10, 0, 0, 0);
    this.meetingDate.set(tomorrow);
    this.selectedDateChip.set('Tomorrow');
    this.customDateLabel.set(null);
    this.selectedHour.set(10);
    this.selectedMinute.set(0);
    this.minuteOptions.set(this.defaultMinuteOptions);
    this.selectedPeriod.set('AM');
  }

  private loadMeeting(id: string): void {
    this.cancelPolling();
    this.meetingId.set(id);
    this.loading.set(true);
    this.notFound.set(false);
    this.actionItemStatuses.set([]);
    this.promotingIds.set(new Set());
    this.audioFileName.set(null);
    this.isTranscribing.set(false);
    this.showTabWarning.set(false);
    this.recorder.discard();

    let attempts = 0;
    const maxAttempts = 100;

    const checkForMeeting = () => {
      if (this.isDestroyed) return;
      attempts++;

      const meetings = this.meetingService.meetings();
      const meeting = meetings.find(m => m.id === id);

      if (meeting) {
        this.populateFromMeeting(meeting);
        this.loading.set(false);
      } else if (this.meetingService.initialLoadComplete()) {
        this.notFound.set(true);
        this.loading.set(false);
      } else if (attempts < maxAttempts) {
        this.pollingTimeoutId = setTimeout(checkForMeeting, 100);
      } else {
        this.notFound.set(true);
        this.loading.set(false);
      }
    };

    checkForMeeting();
  }

  private populateFromMeeting(meeting: Meeting): void {
    this.isNewMeeting.set(false);
    this.title.set(meeting.title ?? '');
    this.attendees.set(meeting.attendees ?? '');
    this.transcript.set(meeting.transcriptContent ?? '');

    const meetingDate = meeting.meetingDate ? new Date(meeting.meetingDate) : new Date();
    this.meetingDate.set(meetingDate);
    this.extractTimeFromDate(meetingDate);
    this.determineInitialDateChip(meetingDate);
  }

  // --- Form change handlers with debounced save ---

  onTitleChange(value: string): void {
    this.title.set(value);
    this.lastSaved.set(false);
    this.metadataChange$.next();
  }

  onAttendeesChange(value: string): void {
    this.attendees.set(value);
    this.lastSaved.set(false);
    this.metadataChange$.next();
  }

  onTranscriptChange(value: string): void {
    this.transcript.set(value);
    this.lastSaved.set(false);
    this.transcriptChange$.next();
  }

  private saveMetadata(): void {
    if (this.isNewMeeting()) {
      this.createNewMeeting();
      return;
    }

    const id = this.meetingId();
    if (!id) return;

    this.isSaving.set(true);
    this.meetingService.updateMeeting(
      id,
      this.title() || undefined,
      this.meetingDate()?.toISOString(),
      this.attendees() || undefined,
    );

    setTimeout(() => {
      this.isSaving.set(false);
      this.lastSaved.set(true);
    }, 300);
  }

  private saveTranscript(): void {
    const id = this.meetingId();
    if (!id || this.isNewMeeting()) return;

    const meeting = this.currentMeeting();
    const currentTranscript = this.transcript();

    if (currentTranscript && currentTranscript !== meeting?.transcriptContent) {
      this.meetingService.submitTranscript(id, currentTranscript);
    } else if (!currentTranscript && meeting?.transcriptContent) {
      this.meetingService.clearTranscript(id);
    }
  }

  private isCreating = false;

  private createNewMeeting(): void {
    if (this.isCreating) return;
    this.isCreating = true;
    this.isNewMeeting.set(false);
    this.isSaving.set(true);

    this.meetingService.createMeeting(
      this.title() || undefined,
      this.meetingDate()?.toISOString(),
      this.attendees() || undefined,
      (realId) => {
        this.isCreating = false;
        if (this.isDestroyed) return;
        this.meetingId.set(realId);
        this.router.navigate(['/meetings', realId], { replaceUrl: true });
        this.isSaving.set(false);
        this.lastSaved.set(true);
      },
      () => {
        this.isCreating = false;
        if (!this.isDestroyed) {
          this.isNewMeeting.set(true);
          this.isSaving.set(false);
        }
      },
    );
  }

  // --- Date/time helpers ---

  private addDays(date: Date, days: number): Date {
    const result = new Date(date);
    result.setDate(result.getDate() + days);
    return result;
  }

  private toHour24(hour12: number, period: 'AM' | 'PM'): number {
    if (period === 'PM' && hour12 !== 12) return hour12 + 12;
    if (period === 'AM' && hour12 === 12) return 0;
    return hour12;
  }

  selectDateOption(option: DateOption): void {
    this.selectedDateChip.set(option.label);
    this.customDateLabel.set(null);
    this.showDatePicker.set(false);

    const newDate = option.getValue();
    const currentDate = this.meetingDate();
    if (currentDate) {
      newDate.setHours(currentDate.getHours(), currentDate.getMinutes(), currentDate.getSeconds(), currentDate.getMilliseconds());
    } else {
      const hour24 = this.toHour24(this.selectedHour(), this.selectedPeriod());
      newDate.setHours(hour24, this.selectedMinute(), 0, 0);
    }
    this.meetingDate.set(newDate);
    this.lastSaved.set(false);
    this.metadataChange$.next();
  }

  onDatePickerChange(date: Date | null): void {
    if (!date) return;
    const newDate = new Date(date);
    const currentDate = this.meetingDate();
    if (currentDate) {
      newDate.setHours(currentDate.getHours(), currentDate.getMinutes(), currentDate.getSeconds(), currentDate.getMilliseconds());
    }
    this.meetingDate.set(newDate);
    this.selectedDateChip.set('custom');
    this.customDateLabel.set(this.formatDateLabel(date));
    this.showDatePicker.set(false);
    this.lastSaved.set(false);
    this.metadataChange$.next();
  }

  private formatDateLabel(date: Date): string {
    return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
  }

  private extractTimeFromDate(date: Date): void {
    let hours = date.getHours();
    const period: 'AM' | 'PM' = hours >= 12 ? 'PM' : 'AM';
    if (hours === 0) hours = 12;
    else if (hours > 12) hours = hours - 12;
    this.selectedHour.set(hours);

    const minutes = date.getMinutes();
    if (minutes % 5 !== 0) {
      const pad = minutes < 10 ? '0' : '';
      const customOption: TimeOption = { label: `${pad}${minutes}`, value: minutes };
      const opts = [...this.defaultMinuteOptions, customOption].sort((a, b) => a.value - b.value);
      this.minuteOptions.set(opts);
    } else {
      this.minuteOptions.set(this.defaultMinuteOptions);
    }
    this.selectedMinute.set(minutes);
    this.selectedPeriod.set(period);
  }

  private determineInitialDateChip(date: Date): void {
    const today = new Date();
    today.setHours(0, 0, 0, 0);
    const tomorrow = this.addDays(today, 1);
    const nextWeek = this.addDays(today, 7);
    const dateOnly = new Date(date);
    dateOnly.setHours(0, 0, 0, 0);

    if (dateOnly.getTime() === today.getTime()) {
      this.selectedDateChip.set('Today');
      this.customDateLabel.set(null);
    } else if (dateOnly.getTime() === tomorrow.getTime()) {
      this.selectedDateChip.set('Tomorrow');
      this.customDateLabel.set(null);
    } else if (dateOnly.getTime() === nextWeek.getTime()) {
      this.selectedDateChip.set('Next Week');
      this.customDateLabel.set(null);
    } else {
      this.selectedDateChip.set('custom');
      this.customDateLabel.set(this.formatDateLabel(date));
    }
  }

  // --- Audio recording ---

  onAudioFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (!file) return;

    const id = this.meetingId();
    if (!id) return;

    this.audioFileName.set(file.name);
    this.isTranscribing.set(true);
    this.meetingService.transcribeAudio(id, file);
    input.value = '';
  }

  async startRecording(): Promise<void> {
    await this.recorder.start();
    if (this.recorder.isActive()) {
      this.showTabWarning.set(true);
    }
  }

  async stopRecording(): Promise<void> {
    try {
      const file = await this.recorder.stop();
      this.showTabWarning.set(false);

      if (!file) return;

      const id = this.meetingId();
      if (!id) return;

      this.audioFileName.set(file.name);
      this.isTranscribing.set(true);
      this.meetingService.transcribeAudio(id, file);
    } catch (error) {
      this.showTabWarning.set(false);
      console.error('Failed to stop audio recording:', error);
      this.toast.error('Failed to stop recording. Please try again.');
    }
  }

  // --- AI Analysis ---

  analyze(): void {
    const id = this.meetingId();
    if (id) {
      // Save transcript first before analyzing
      this.saveTranscript();
      this.meetingService.analyzeMeeting(id);
    }
  }

  toggleActionItem(actionItemId: string): void {
    const id = this.meetingId();
    if (id) {
      this.meetingService.toggleActionItem(id, actionItemId);
    }
  }

  promoteActionItem(actionItemId: string): void {
    const id = this.meetingId();
    if (!id) return;

    this.promotingIds.update(ids => new Set([...ids, actionItemId]));

    this.meetingService.promoteActionItem(id, actionItemId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: result => {
          if (this.meetingId() !== id) return;
          this.toast.success({ summary: 'Task created', detail: result.title });
          this.loadActionItemStatuses();
          this.promotingIds.update(ids => {
            const newSet = new Set(ids);
            newSet.delete(actionItemId);
            return newSet;
          });
        },
        error: () => {
          if (this.isDestroyed) return;
          this.toast.error('Failed to promote action item');
          this.promotingIds.update(ids => {
            const newSet = new Set(ids);
            newSet.delete(actionItemId);
            return newSet;
          });
        },
      });
  }

  navigateToTask(taskId: string): void {
    this.router.navigate(['/tasks'], { queryParams: { highlight: taskId } });
  }

  private loadActionItemStatuses(): void {
    const id = this.meetingId();
    if (!id) return;

    this.meetingService.getActionItemStatus(id).subscribe({
      next: statuses => {
        if (this.meetingId() !== id) return;
        this.actionItemStatuses.set(statuses);
      },
      error: () => {
        if (this.meetingId() !== id) return;
        this.actionItemStatuses.set([]);
      },
    });
  }

  // --- Navigation ---

  navigateBack(): void {
    // Save before leaving
    if (!this.lastSaved() && !this.isNewMeeting()) {
      this.saveMetadata();
      this.saveTranscript();
    }
    this.router.navigate(['/meetings']);
  }

  deleteMeeting(): void {
    const id = this.meetingId();
    if (!id) return;

    const deleted = this.meetingService.deleteMeetingWithUndo(id);
    if (deleted) {
      this.toast.success({
        summary: 'Meeting deleted',
        action: {
          label: 'Undo',
          callback: () => this.meetingService.undoDelete(id),
        },
      });
      this.router.navigate(['/meetings']);
    }
  }

  // --- Tags ---

  openTagInput(): void {
    this.showTagPicker.set(true);
    this.tagSearch.set('');
    afterNextRender(() => {
      this.tagInput()?.nativeElement.focus();
    }, { injector: this.injector });
  }

  onTagEnter(): void {
    const query = this.tagSearch().trim();
    const suggestions = this.tagSuggestions();

    const exactMatch = suggestions.find(t => t.name.toLowerCase() === query.toLowerCase());
    if (exactMatch) {
      this.addTag({ id: exactMatch.id, name: exactMatch.name });
      return;
    }
    if (this.canCreateTag()) {
      this.createAndAddTag(query);
      return;
    }
    if (suggestions.length === 1) {
      this.addTag({ id: suggestions[0].id, name: suggestions[0].name });
    }
  }

  addTag(tag: MeetingTag): void {
    const id = this.meetingId();
    if (!id) return;
    if (this.meetingTags().some(t => t.id === tag.id)) {
      this.showTagPicker.set(false);
      this.tagSearch.set('');
      return;
    }
    this.meetingService.addTag(id, tag.id, tag.name);
    this.tagService.incrementUsageCount(tag.id);
    this.showTagPicker.set(false);
    this.tagSearch.set('');
  }

  removeTag(tagId: string): void {
    const id = this.meetingId();
    if (!id) return;
    this.meetingService.removeTag(id, tagId);
    this.tagService.decrementUsageCount(tagId);
  }

  createAndAddTag(name: string): void {
    this.tagService.createTag(name, (createdTag: Tag) => {
      this.addTag({ id: createdTag.id, name: createdTag.name });
    });
    this.showTagPicker.set(false);
    this.tagSearch.set('');
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

  // --- Formatting helpers ---

  formatDate(dateString: string): string {
    const date = new Date(dateString);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffMins = Math.floor(diffMs / 60000);
    const diffHours = Math.floor(diffMs / 3600000);
    const diffDays = Math.floor(diffMs / 86400000);

    if (diffMins < 1) return 'just now';
    if (diffMins < 60) return `${diffMins} min ago`;
    if (diffHours < 24) return `${diffHours} hour${diffHours > 1 ? 's' : ''} ago`;
    if (diffDays < 7) return `${diffDays} day${diffDays > 1 ? 's' : ''} ago`;
    return date.toLocaleDateString();
  }

  /** Type-safe helper for input events */
  asInput(event: Event): HTMLInputElement {
    return event.target as HTMLInputElement;
  }

  /** Type-safe helper for textarea events */
  asTextarea(event: Event): HTMLTextAreaElement {
    return event.target as HTMLTextAreaElement;
  }
}
