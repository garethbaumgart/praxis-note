import { Component, ChangeDetectionStrategy, input, output, computed } from '@angular/core';
import { ButtonModule } from 'primeng/button';
import { Meeting, parseJsonArray, parseBehavioralAnalysis } from './meeting.model';
import { MeetingBehavioralAnalysisComponent } from './meeting-behavioral-analysis.component';
import { MeetingActionItemsComponent } from './meeting-action-items.component';

@Component({
  selector: 'app-meeting-analysis',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ButtonModule, MeetingBehavioralAnalysisComponent, MeetingActionItemsComponent],
  template: `
    <div class="border border-border rounded-lg p-4 bg-surface-subtle">
      <!-- Header with Analyze button -->
      <div class="flex items-center justify-between mb-4">
        <h3 class="text-sm font-semibold text-foreground">AI Analysis</h3>
        @if (canAnalyze()) {
          <p-button
            [label]="hasAnalysis() ? 'Re-analyze' : 'Analyze'"
            icon="pi pi-sparkles"
            size="small"
            [loading]="isProcessing()"
            [disabled]="!hasTranscript()"
            (onClick)="onAnalyze.emit()"
          />
        }
      </div>

      <!-- Processing state -->
      @if (isProcessing()) {
        <div class="flex items-center gap-3 text-foreground-muted">
          <i class="pi pi-spin pi-spinner"></i>
          <span class="text-sm">Analyzing transcript...</span>
        </div>
      }

      <!-- Failed state -->
      @else if (isFailed()) {
        <div class="flex items-center gap-3 text-danger">
          <i class="pi pi-exclamation-circle"></i>
          <span class="text-sm">Analysis failed. Please try again.</span>
        </div>
      }

      <!-- Analysis results (show even if transcript was cleared) -->
      @else if (hasAnalysis()) {
        <div class="space-y-4">
          <!-- Summary -->
          @if (meeting().summary) {
            <div>
              <h4 class="text-xs font-semibold text-foreground-muted uppercase tracking-wide mb-2">Summary</h4>
              <p class="text-sm text-foreground">{{ meeting().summary }}</p>
            </div>
          }

          <!-- Key Points -->
          @if (keyPoints().length > 0) {
            <div>
              <h4 class="text-xs font-semibold text-foreground-muted uppercase tracking-wide mb-2">Key Points</h4>
              <ul class="space-y-1.5">
                @for (point of keyPoints(); track $index) {
                  <li class="flex gap-2 text-sm text-foreground">
                    <i class="pi pi-check-circle text-accent-solid mt-0.5 flex-shrink-0"></i>
                    <span>{{ point }}</span>
                  </li>
                }
              </ul>
            </div>
          }

          <!-- Decisions -->
          @if (decisions().length > 0) {
            <div>
              <h4 class="text-xs font-semibold text-foreground-muted uppercase tracking-wide mb-2">Decisions</h4>
              <ul class="space-y-1.5">
                @for (decision of decisions(); track $index) {
                  <li class="flex gap-2 text-sm text-foreground">
                    <i class="pi pi-bolt text-warning-foreground mt-0.5 flex-shrink-0"></i>
                    <span>{{ decision }}</span>
                  </li>
                }
              </ul>
            </div>
          } @else if (hasAnalysis()) {
            <div>
              <h4 class="text-xs font-semibold text-foreground-muted uppercase tracking-wide mb-2">Decisions</h4>
              <p class="text-sm text-foreground-muted italic">No decisions were recorded in this meeting.</p>
            </div>
          }

          <!-- Action Items -->
          <app-meeting-action-items
            [actionItems]="meeting().actionItems"
            (onToggle)="onToggleActionItem.emit($event)"
          />
        </div>

        <!-- Behavioral Analysis -->
        @if (hasBehavioralAnalysis()) {
          <app-meeting-behavioral-analysis [meeting]="meeting()" />
        }
      }

      <!-- No transcript -->
      @else if (!hasTranscript()) {
        <p class="text-sm text-foreground-muted">Add a transcript to enable AI analysis.</p>
      }

      <!-- No analysis yet (has transcript but no analysis) -->
      @else {
        <p class="text-sm text-foreground-muted">Click "Analyze" to generate a summary, key points, and decisions.</p>
      }
    </div>
  `,
})
export class MeetingAnalysisComponent {
  readonly meeting = input.required<Meeting>();
  readonly onAnalyze = output<void>();
  readonly onToggleActionItem = output<string>();

  readonly hasTranscript = computed(() => !!this.meeting().transcriptContent);
  readonly isProcessing = computed(() => this.meeting().status === 'Processing');
  readonly isFailed = computed(() => this.meeting().status === 'Failed');
  readonly hasAnalysis = computed(() =>
    !!this.meeting().summary?.trim() ||
    this.keyPoints().length > 0 ||
    this.decisions().length > 0 ||
    this.meeting().status === 'Ready'
  );
  readonly canAnalyze = computed(() => !this.isProcessing());

  readonly keyPoints = computed(() => parseJsonArray(this.meeting().keyPoints));
  readonly decisions = computed(() => parseJsonArray(this.meeting().decisions));
  readonly hasBehavioralAnalysis = computed(() => !!parseBehavioralAnalysis(this.meeting().behavioralAnalysis));
}
