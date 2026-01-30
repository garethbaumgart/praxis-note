import { Component, ChangeDetectionStrategy, input, output, signal, computed, inject, OnInit } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { Textarea } from 'primeng/textarea';
import { Button } from 'primeng/button';
import { Meeting, ReflectionPrompt, MeetingReflection, PromptResponse, parseReflection, parseBehavioralAnalysis } from './meeting.model';
import { MeetingService } from './meeting.service';

@Component({
  selector: 'app-meeting-reflection',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Textarea, Button],
  template: `
    @if (editing()) {
      <!-- Edit mode -->
      <div class="space-y-5">
        <!-- Contextual prompts -->
        @for (prompt of prompts(); track prompt.promptId) {
          <div>
            <p class="text-sm text-foreground mb-2">{{ prompt.promptText }}</p>
            @if (prompt.quickOptions.length > 0) {
              <div class="flex flex-wrap gap-2">
                @for (option of prompt.quickOptions; track option) {
                  <button
                    type="button"
                    class="px-3 py-1.5 rounded-full text-xs font-medium transition-colors cursor-pointer"
                    [class.bg-surface-muted]="getPromptResponse(prompt.promptId) !== option"
                    [class.text-foreground-secondary]="getPromptResponse(prompt.promptId) !== option"
                    [class.reflection-chip-selected]="getPromptResponse(prompt.promptId) === option"
                    (click)="setPromptResponse(prompt.promptId, prompt.promptText, option)"
                    (keydown.enter)="setPromptResponse(prompt.promptId, prompt.promptText, option)"
                  >
                    {{ option }}
                  </button>
                }
              </div>
            } @else {
              <textarea
                pTextarea
                class="w-full text-sm"
                [rows]="2"
                [value]="getPromptResponse(prompt.promptId)"
                (input)="setPromptResponse(prompt.promptId, prompt.promptText, $any($event.target).value)"
                placeholder="Your thoughts..."
              ></textarea>
            }
          </div>
        }

        <!-- Freeform reflection -->
        <div>
          <p class="text-sm text-foreground mb-2">Any additional thoughts or reflections?</p>
          <textarea
            pTextarea
            class="w-full text-sm"
            [rows]="3"
            [value]="freeformReflection()"
            (input)="freeformReflection.set($any($event.target).value)"
            placeholder="Open reflection..."
          ></textarea>
        </div>

        <div class="flex justify-end gap-2">
          @if (hasExistingReflection()) {
            <p-button
              label="Cancel"
              severity="secondary"
              [text]="true"
              size="small"
              (onClick)="cancelEdit()"
            />
          }
          <p-button
            label="Save Reflection"
            size="small"
            [loading]="saving()"
            (onClick)="save()"
            icon="pi pi-check"
          />
        </div>
      </div>
    } @else if (hasExistingReflection()) {
      <!-- View mode -->
      <div class="space-y-4">
        <!-- Prompt responses summary -->
        @if (existingPromptResponses().length > 0) {
          <div class="space-y-2">
            @for (response of existingPromptResponses(); track response.promptId) {
              <div class="bg-surface-subtle rounded-lg p-3">
                <p class="text-[10px] uppercase tracking-wide text-foreground-muted mb-1">{{ response.promptText }}</p>
                <p class="text-sm font-medium text-foreground">{{ response.response }}</p>
              </div>
            }
          </div>
        }

        <!-- Blind spot insights -->
        @if (blindSpotInsights().length > 0) {
          <div class="bg-surface-subtle border border-border rounded-lg p-3">
            <div class="flex items-center gap-2 mb-2">
              <i class="pi pi-lightbulb text-inprogress-foreground text-xs"></i>
              <span class="text-xs font-semibold text-foreground">Awareness Insights</span>
            </div>
            <ul class="space-y-1.5">
              @for (insight of blindSpotInsights(); track $index) {
                <li class="text-xs text-foreground flex items-start gap-2">
                  <i class="pi pi-arrow-right text-[10px] mt-0.5 text-foreground-muted"></i>
                  <span>{{ insight }}</span>
                </li>
              }
            </ul>
          </div>
        }

        <!-- Freeform reflection -->
        @if (existingFreeform()) {
          <div class="bg-surface-subtle rounded-lg p-3">
            <p class="text-[10px] uppercase tracking-wide text-foreground-muted mb-1">Open Reflection</p>
            <p class="text-sm text-foreground">{{ existingFreeform() }}</p>
          </div>
        }

        <div class="flex justify-end">
          <p-button
            label="Edit"
            severity="secondary"
            [text]="true"
            size="small"
            icon="pi pi-pencil"
            (onClick)="startEdit()"
          />
        </div>
      </div>
    } @else {
      <!-- Loading / empty -->
      @if (loadingPrompts()) {
        <div class="text-center py-4">
          <i class="pi pi-spin pi-spinner text-foreground-muted"></i>
          <p class="text-xs text-foreground-muted mt-2">Loading reflection prompts...</p>
        </div>
      } @else {
        <div class="text-center py-4">
          <p class="text-sm text-foreground-muted mb-3">Reflect on your meeting to build self-awareness.</p>
          <p-button
            label="Start Reflection"
            size="small"
            [outlined]="true"
            icon="pi pi-pencil"
            (onClick)="startReflection()"
          />
        </div>
      }
    }
  `,
  styles: [`
    :host {
      display: block;
    }

    .reflection-chip-selected {
      background: var(--color-meeting-reflection-border);
      color: white;
    }
  `]
})
export class MeetingReflectionComponent implements OnInit {
  private readonly meetingService = inject(MeetingService);

  readonly meeting = input.required<Meeting>();
  readonly onReflectionSaved = output<void>();

  // State
  readonly prompts = signal<ReflectionPrompt[]>([]);
  readonly loadingPrompts = signal(false);
  readonly saving = signal(false);
  readonly editing = signal(false);
  readonly freeformReflection = signal('');
  readonly promptResponses = signal<Map<string, PromptResponse>>(new Map());

  // Computed
  readonly hasExistingReflection = computed(() => !!parseReflection(this.meeting().reflectionData));

  readonly existingReflection = computed(() => parseReflection(this.meeting().reflectionData));

  readonly existingPromptResponses = computed(() => this.existingReflection()?.promptResponses ?? []);

  readonly existingFreeform = computed(() => this.existingReflection()?.freeformReflection ?? null);

  readonly blindSpotInsights = computed(() => {
    const reflection = this.existingReflection();
    const analysis = parseBehavioralAnalysis(this.meeting().behavioralAnalysis);
    if (!reflection || !analysis) return [];

    const insights: string[] = [];

    // Compare self-assessed talk time responses to actual data
    const talkTimeResponse = reflection.promptResponses.find(r => r.promptId === 'talk-time-dominant');
    const dominantSpeaker = analysis.speakingDynamics?.talkTimeByParticipant?.find(p => p.percentage > 50);
    if (talkTimeResponse && dominantSpeaker) {
      if (talkTimeResponse.response === 'About Right' && dominantSpeaker.percentage > 55) {
        insights.push(
          `You rated your talk time as "About Right" but the analysis shows ${dominantSpeaker.percentage.toFixed(0)}% — consider being more mindful of speaking balance.`
        );
      }
    }

    // Compare interruption awareness to actual data
    const interruptionResponse = reflection.promptResponses.find(r => r.promptId === 'interruptions-awareness');
    const totalInterruptions = analysis.speakingDynamics?.interruptionPatterns?.reduce((sum, p) => sum + p.count, 0) ?? 0;
    if (interruptionResponse && totalInterruptions >= 2) {
      if (interruptionResponse.response === 'No') {
        insights.push(
          `You weren't aware of ${totalInterruptions} interruption(s). Building awareness of this pattern could improve meeting dynamics.`
        );
      }
    }

    // Compare engagement self-assessment
    const engagementResponse = reflection.promptResponses.find(r => r.promptId === 'engagement-low');
    const lowEngagement = analysis.communicationPatterns?.engagementLevels?.some(e => e.level === 'low');
    if (engagementResponse && lowEngagement) {
      if (engagementResponse.response === 'Highly Engaged') {
        insights.push(
          'You felt highly engaged, but the analysis detected low engagement signals. This could indicate your engagement style differs from typical patterns.'
        );
      }
    }

    // Compare tone assessment
    const toneResponse = reflection.promptResponses.find(r => r.promptId === 'tone-negative');
    const hasNegativeSentiment = analysis.sentimentTone?.participantSentiments?.some(s => s.sentiment === 'negative');
    if (toneResponse && hasNegativeSentiment) {
      if (toneResponse.response === 'Collaborative') {
        insights.push(
          'You perceived the meeting as collaborative, but the analysis detected negative tones. This gap could indicate unconscious tension worth exploring.'
        );
      }
    }

    return insights;
  });

  ngOnInit(): void {
    // If there's already a reflection, don't auto-load prompts
  }

  startReflection(): void {
    this.loadPrompts();
  }

  startEdit(): void {
    const existing = this.existingReflection();
    if (existing) {
      this.freeformReflection.set(existing.freeformReflection ?? '');
      const map = new Map<string, PromptResponse>();
      for (const r of existing.promptResponses) {
        map.set(r.promptId, r);
      }
      this.promptResponses.set(map);
    }
    this.loadPrompts();
  }

  cancelEdit(): void {
    this.editing.set(false);
  }

  getPromptResponse(promptId: string): string {
    return this.promptResponses().get(promptId)?.response ?? '';
  }

  setPromptResponse(promptId: string, promptText: string, response: string): void {
    this.promptResponses.update(map => {
      const newMap = new Map(map);
      newMap.set(promptId, { promptId, promptText, response });
      return newMap;
    });
  }

  save(): void {
    this.saving.set(true);

    const responses: PromptResponse[] = Array.from(this.promptResponses().values())
      .filter(r => r.response.trim().length > 0);

    const reflection: MeetingReflection = {
      selfAssessedTalkTime: null,
      selfAssessedEngagement: null,
      selfAssessedTone: null,
      interruptionAwareness: null,
      freeformReflection: this.freeformReflection().trim() || null,
      promptResponses: responses,
    };

    this.meetingService.submitReflection(this.meeting().id, reflection);
    this.saving.set(false);
    this.editing.set(false);
    this.onReflectionSaved.emit();
  }

  private loadPrompts(): void {
    this.loadingPrompts.set(true);
    this.meetingService.getReflectionPrompts(this.meeting().id).subscribe({
      next: prompts => {
        this.prompts.set(prompts);
        this.loadingPrompts.set(false);
        this.editing.set(true);
      },
      error: () => {
        this.loadingPrompts.set(false);
      },
    });
  }
}
