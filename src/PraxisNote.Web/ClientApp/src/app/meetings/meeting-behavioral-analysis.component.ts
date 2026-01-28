import { Component, ChangeDetectionStrategy, input, computed } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { Accordion, AccordionPanel, AccordionHeader, AccordionContent } from 'primeng/accordion';
import { ProgressBar } from 'primeng/progressbar';
import { Meeting, parseBehavioralAnalysis, BehavioralAnalysis, RedFlag } from './meeting.model';

@Component({
  selector: 'app-meeting-behavioral-analysis',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DecimalPipe, Accordion, AccordionPanel, AccordionHeader, AccordionContent, ProgressBar],
  template: `
    @if (behavioralData()) {
      <div class="border border-border rounded-lg bg-surface-subtle mt-4">
        <div class="px-4 py-3 border-b border-border">
          <h3 class="text-sm font-semibold text-foreground flex items-center gap-2">
            <i class="pi pi-users"></i>
            Behavioral Analysis
          </h3>
        </div>

        <p-accordion [multiple]="true" class="behavioral-accordion">
          <!-- Speaking Dynamics -->
          <p-accordion-panel value="speaking">
            <p-accordion-header>
              <div class="flex items-center gap-2">
                <i class="pi pi-chart-bar text-accent-foreground"></i>
                <span class="font-medium">Speaking Dynamics</span>
              </div>
            </p-accordion-header>
            <p-accordion-content>
              <div class="space-y-4 p-2">
                <!-- Talk Time -->
                @if (talkTimes().length > 0) {
                  <div>
                    <h5 class="text-xs font-semibold text-foreground-muted uppercase tracking-wide mb-2">Talk Time</h5>
                    <div class="space-y-2">
                      @for (participant of talkTimes(); track participant.participant) {
                        <div class="flex items-center gap-3">
                          <span class="text-sm text-foreground min-w-24">{{ participant.participant }}</span>
                          <div class="flex-1">
                            <p-progressBar
                              [value]="participant.percentage"
                              [showValue]="false"
                              styleClass="h-2"
                            />
                          </div>
                          <span class="text-xs text-foreground-muted min-w-16 text-right">
                            {{ participant.percentage | number:'1.0-1' }}% ({{ participant.duration }})
                          </span>
                        </div>
                      }
                    </div>
                  </div>
                }

                <!-- Interruptions -->
                @if (interruptions().length > 0) {
                  <div>
                    <h5 class="text-xs font-semibold text-foreground-muted uppercase tracking-wide mb-2">Interruption Patterns</h5>
                    <ul class="space-y-1">
                      @for (pattern of interruptions(); track $index) {
                        <li class="text-sm text-foreground flex items-center gap-2">
                          <i class="pi pi-arrow-right-arrow-left text-inprogress-foreground text-xs"></i>
                          <span>{{ pattern.interrupter }} interrupted {{ pattern.interrupted }} ({{ pattern.count }}x)</span>
                        </li>
                      }
                    </ul>
                  </div>
                }

                <!-- Question Ratios -->
                @if (questionRatioEntries().length > 0) {
                  <div>
                    <h5 class="text-xs font-semibold text-foreground-muted uppercase tracking-wide mb-2">Question vs Statement Ratio</h5>
                    <div class="flex flex-wrap gap-2">
                      @for (entry of questionRatioEntries(); track entry.name) {
                        <span class="px-2 py-1 bg-surface-muted rounded text-xs text-foreground">
                          {{ entry.name }}: {{ entry.ratio | number:'1.0-0' }}% questions
                        </span>
                      }
                    </div>
                  </div>
                }
              </div>
            </p-accordion-content>
          </p-accordion-panel>

          <!-- Sentiment & Tone -->
          <p-accordion-panel value="sentiment">
            <p-accordion-header>
              <div class="flex items-center gap-2">
                <i class="pi pi-heart text-accent-foreground"></i>
                <span class="font-medium">Sentiment & Tone</span>
              </div>
            </p-accordion-header>
            <p-accordion-content>
              <div class="space-y-4 p-2">
                <!-- Participant Sentiments -->
                @if (sentiments().length > 0) {
                  <div>
                    <h5 class="text-xs font-semibold text-foreground-muted uppercase tracking-wide mb-2">Participant Sentiments</h5>
                    <div class="flex flex-wrap gap-2">
                      @for (s of sentiments(); track s.participant) {
                        <span
                          class="px-2 py-1 rounded text-xs inline-flex items-center gap-1"
                          [class.bg-done]="s.sentiment === 'positive'"
                          [class.text-done-foreground]="s.sentiment === 'positive'"
                          [class.bg-danger-bg]="s.sentiment === 'negative'"
                          [class.text-danger]="s.sentiment === 'negative'"
                          [class.bg-surface-muted]="s.sentiment === 'neutral'"
                          [class.text-foreground-secondary]="s.sentiment === 'neutral'"
                        >
                          <i
                            class="pi text-xs"
                            [class.pi-thumbs-up]="s.sentiment === 'positive'"
                            [class.pi-thumbs-down]="s.sentiment === 'negative'"
                            [class.pi-minus]="s.sentiment === 'neutral'"
                          ></i>
                          {{ s.participant }}
                        </span>
                      }
                    </div>
                  </div>
                }

                <!-- Tone Shifts -->
                @if (toneShifts().length > 0) {
                  <div>
                    <h5 class="text-xs font-semibold text-foreground-muted uppercase tracking-wide mb-2">Tone Shifts</h5>
                    <ul class="space-y-2">
                      @for (shift of toneShifts(); track $index) {
                        <li class="text-sm text-foreground">
                          <span class="text-foreground-muted">{{ shift.timestamp }}:</span>
                          {{ shift.description }}
                          <span class="text-xs text-foreground-muted">({{ shift.from }} → {{ shift.to }})</span>
                        </li>
                      }
                    </ul>
                  </div>
                }

                <!-- Emotional Indicators -->
                @if (emotionalIndicators().length > 0) {
                  <div>
                    <h5 class="text-xs font-semibold text-foreground-muted uppercase tracking-wide mb-2">Emotional Indicators</h5>
                    <ul class="space-y-1">
                      @for (indicator of emotionalIndicators(); track $index) {
                        <li class="text-sm text-foreground flex items-center gap-2">
                          <i class="pi pi-info-circle text-accent-foreground text-xs"></i>
                          {{ indicator }}
                        </li>
                      }
                    </ul>
                  </div>
                }
              </div>
            </p-accordion-content>
          </p-accordion-panel>

          <!-- Communication Patterns -->
          <p-accordion-panel value="communication">
            <p-accordion-header>
              <div class="flex items-center gap-2">
                <i class="pi pi-comments text-accent-foreground"></i>
                <span class="font-medium">Communication Patterns</span>
              </div>
            </p-accordion-header>
            <p-accordion-content>
              <div class="space-y-4 p-2">
                <!-- Overall Clarity -->
                <div>
                  <h5 class="text-xs font-semibold text-foreground-muted uppercase tracking-wide mb-2">Overall Clarity</h5>
                  <div class="flex items-center gap-3">
                    <div class="flex-1">
                      <p-progressBar
                        [value]="clarityPercent()"
                        [showValue]="false"
                        styleClass="h-2"
                      />
                    </div>
                    <span class="text-sm text-foreground min-w-12 text-right">{{ clarityPercent() | number:'1.0-0' }}%</span>
                  </div>
                </div>

                <!-- Follow-up Patterns -->
                @if (followUpPatterns().length > 0) {
                  <div>
                    <h5 class="text-xs font-semibold text-foreground-muted uppercase tracking-wide mb-2">Follow-up Items</h5>
                    <ul class="space-y-1">
                      @for (pattern of followUpPatterns(); track pattern.topic) {
                        <li class="text-sm text-foreground flex items-center gap-2">
                          <i
                            class="pi text-xs"
                            [class.pi-check-circle]="pattern.wasFollowedUp"
                            [class.text-done-foreground]="pattern.wasFollowedUp"
                            [class.pi-circle]="!pattern.wasFollowedUp"
                            [class.text-foreground-muted]="!pattern.wasFollowedUp"
                          ></i>
                          <span>{{ pattern.topic }}</span>
                          @if (pattern.assignedTo) {
                            <span class="text-xs text-foreground-muted">({{ pattern.assignedTo }})</span>
                          }
                        </li>
                      }
                    </ul>
                  </div>
                }

                <!-- Engagement Levels -->
                @if (engagementLevels().length > 0) {
                  <div>
                    <h5 class="text-xs font-semibold text-foreground-muted uppercase tracking-wide mb-2">Engagement Levels</h5>
                    <div class="flex flex-wrap gap-2">
                      @for (e of engagementLevels(); track e.participant) {
                        <span
                          class="px-2 py-1 rounded text-xs"
                          [class.bg-done]="e.level === 'high'"
                          [class.text-done-foreground]="e.level === 'high'"
                          [class.bg-inprogress]="e.level === 'medium'"
                          [class.text-inprogress-foreground]="e.level === 'medium'"
                          [class.bg-surface-muted]="e.level === 'low'"
                          [class.text-foreground-muted]="e.level === 'low'"
                          [title]="e.indicators.join(', ')"
                        >
                          {{ e.participant }}: {{ e.level }}
                        </span>
                      }
                    </div>
                  </div>
                }
              </div>
            </p-accordion-content>
          </p-accordion-panel>

          <!-- Red Flags -->
          @if (redFlags().length > 0) {
            <p-accordion-panel value="redflags">
              <p-accordion-header>
                <div class="flex items-center gap-2">
                  <i class="pi pi-exclamation-triangle text-danger"></i>
                  <span class="font-medium">Red Flags</span>
                  <span class="ml-2 px-1.5 py-0.5 bg-danger-bg text-danger text-xs rounded">{{ redFlags().length }}</span>
                </div>
              </p-accordion-header>
              <p-accordion-content>
                <div class="space-y-3 p-2">
                  @for (flag of redFlags(); track $index) {
                    <div
                      class="p-3 rounded-lg border"
                      [class.border-danger]="flag.severity === 'high'"
                      [class.bg-danger-bg]="flag.severity === 'high'"
                      [class.border-inprogress-border]="flag.severity === 'medium'"
                      [class.bg-inprogress]="flag.severity === 'medium'"
                      [class.border-border]="flag.severity === 'low'"
                      [class.bg-surface-muted]="flag.severity === 'low'"
                    >
                      <div class="flex items-start justify-between gap-2 mb-1">
                        <span class="text-sm font-medium text-foreground">{{ flag.participant }}</span>
                        <span
                          class="text-xs px-1.5 py-0.5 rounded capitalize"
                          [class.bg-danger]="flag.severity === 'high'"
                          [class.text-white]="flag.severity === 'high'"
                          [class.bg-inprogress-solid]="flag.severity === 'medium'"
                          [class.text-surface]="flag.severity === 'medium'"
                          [class.bg-surface-muted]="flag.severity === 'low'"
                          [class.text-foreground-muted]="flag.severity === 'low'"
                        >{{ flag.severity }}</span>
                      </div>
                      <p class="text-sm text-foreground">{{ flag.description }}</p>
                      @if (flag.context) {
                        <p class="text-xs text-foreground-muted mt-1 italic">"{{ flag.context }}"</p>
                      }
                      <span class="inline-block mt-2 text-xs px-2 py-0.5 bg-surface-muted text-foreground-muted rounded capitalize">
                        {{ flag.type }}
                      </span>
                    </div>
                  }
                </div>
              </p-accordion-content>
            </p-accordion-panel>
          }
        </p-accordion>
      </div>
    }
  `,
  styles: [`
    :host ::ng-deep .behavioral-accordion {
      p-accordion-panel {
        border-bottom: 1px solid var(--color-border);

        &:last-child {
          border-bottom: none;
        }
      }

      p-accordion-header {
        display: block;
        background: transparent;
        padding: 0.75rem 1rem;
        cursor: pointer;

        &:hover {
          background: var(--color-surface-subtle);
        }
      }

      p-accordion-content {
        display: block;
        padding: 0.5rem 1rem 1rem;
        background: transparent;
      }
    }

    :host ::ng-deep .p-progressbar {
      background: var(--color-surface-muted);
      border-radius: 0.25rem;

      .p-progressbar-value {
        background: var(--color-accent-solid);
      }
    }
  `]
})
export class MeetingBehavioralAnalysisComponent {
  readonly meeting = input.required<Meeting>();

  readonly behavioralData = computed(() => parseBehavioralAnalysis(this.meeting().behavioralAnalysis));

  // Speaking Dynamics
  readonly talkTimes = computed(() => this.behavioralData()?.speakingDynamics?.talkTimeByParticipant ?? []);
  readonly interruptions = computed(() => this.behavioralData()?.speakingDynamics?.interruptionPatterns ?? []);
  readonly questionRatioEntries = computed(() => {
    const ratios = this.behavioralData()?.speakingDynamics?.questionVsStatementRatio ?? {};
    return Object.entries(ratios).map(([name, ratio]) => ({ name, ratio: ratio * 100 }));
  });

  // Sentiment & Tone
  readonly sentiments = computed(() => this.behavioralData()?.sentimentTone?.participantSentiments ?? []);
  readonly toneShifts = computed(() => this.behavioralData()?.sentimentTone?.toneShifts ?? []);
  readonly emotionalIndicators = computed(() => this.behavioralData()?.sentimentTone?.emotionalIndicators ?? []);

  // Communication Patterns
  readonly clarityPercent = computed(() => (this.behavioralData()?.communicationPatterns?.overallClarity ?? 0) * 100);
  readonly followUpPatterns = computed(() => this.behavioralData()?.communicationPatterns?.followUpPatterns ?? []);
  readonly engagementLevels = computed(() => this.behavioralData()?.communicationPatterns?.engagementLevels ?? []);

  // Red Flags
  readonly redFlags = computed<RedFlag[]>(() => this.behavioralData()?.redFlags ?? []);
}
