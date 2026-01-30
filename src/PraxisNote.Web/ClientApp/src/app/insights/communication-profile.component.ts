import { Component, ChangeDetectionStrategy, inject, computed, effect } from '@angular/core';
import { Skeleton } from 'primeng/skeleton';
import { CommunicationProfileService } from './communication-profile.service';
import { InsightsService } from './insights.service';
import { ArchetypeScore } from './insights.model';

@Component({
  selector: 'app-communication-profile',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Skeleton],
  template: `
    <section class="mb-6">
      <h2 class="text-lg font-semibold text-foreground mb-3">Communication Style</h2>

      @if (profileService.loading()) {
        <!-- Skeleton loading -->
        <div class="bg-surface-subtle border border-border rounded-xl p-6">
          <div class="flex flex-col lg:flex-row gap-6">
            <div class="flex-1">
              <p-skeleton width="40%" height="12px" styleClass="mb-3" />
              <p-skeleton width="60%" height="28px" styleClass="mb-2" />
              <p-skeleton width="50%" height="14px" styleClass="mb-4" />
              <p-skeleton width="100%" height="48px" />
            </div>
            <p-skeleton width="280px" height="240px" />
          </div>
        </div>
      } @else if (profileService.error()) {
        <div class="bg-surface-subtle border border-border rounded-xl p-6 text-center">
          <i class="pi pi-exclamation-triangle text-3xl text-danger mb-2"></i>
          <p class="text-sm text-danger">{{ profileService.error() }}</p>
        </div>
      } @else if (!profileService.profile()?.hasEnoughData) {
        <!-- Empty state: not enough meetings -->
        <div class="bg-surface-subtle border border-border rounded-xl p-8 text-center">
          <div class="w-14 h-14 rounded-2xl bg-surface-muted flex items-center justify-center mx-auto mb-4">
            <i class="pi pi-user text-2xl text-foreground-muted"></i>
          </div>
          <h3 class="text-base font-semibold text-foreground mb-2">Communication profile building...</h3>
          <p class="text-sm text-foreground-muted max-w-md mx-auto mb-4">
            Analyze
            <strong class="text-foreground">{{ meetingsRemaining() }} more meeting{{ meetingsRemaining() === 1 ? '' : 's' }}</strong>
            to unlock your communication style profile.
            We need at least {{ profileService.profile()?.minimumMeetings ?? 5 }} analyzed meetings.
          </p>
          <div class="flex items-center justify-center gap-1.5" role="img" [attr.aria-label]="progressAriaLabel()">
            @for (dot of progressDots(); track $index) {
              <span class="w-3 h-3 rounded-full" [class.bg-accent-solid]="dot" [class.bg-border]="!dot"></span>
            }
            <span class="text-xs text-foreground-muted ml-2">
              {{ profileService.profile()?.meetingCount ?? 0 }} of {{ profileService.profile()?.minimumMeetings ?? 5 }} meetings
            </span>
          </div>
        </div>
      } @else {
        <!-- Full profile: Hero Card with Radar -->
        <div class="bg-surface-subtle border border-border rounded-xl overflow-hidden">
          <!-- Hero section -->
          <div class="p-6 flex flex-col lg:flex-row gap-6">
            <!-- Left: Archetype info -->
            <div class="flex-1">
              <p class="text-xs font-medium text-foreground-muted uppercase tracking-wider mb-1">Your Communication Style</p>
              <h3 class="text-2xl font-bold text-foreground mb-1">{{ profileService.profile()!.primaryArchetype }}</h3>
              @if (profileService.profile()!.secondaryArchetype) {
                <p class="text-sm text-accent-foreground mb-3">
                  with tendencies toward <strong>{{ profileService.profile()!.secondaryArchetype }}</strong>
                </p>
              }
              <p class="text-sm text-foreground-secondary leading-relaxed">
                {{ profileService.profile()!.primaryDescription }}
              </p>
              <div class="mt-4 flex items-center gap-2 text-xs text-foreground-muted">
                <i class="pi pi-chart-bar text-accent-foreground"></i>
                <span>Based on <strong class="text-foreground">{{ profileService.profile()!.meetingCount }} meetings</strong> over {{ insightsService.dateRange() === 'all' ? 'all time' : insightsService.dateRange() }}</span>
              </div>
              <div class="mt-2 flex items-center gap-2 text-xs text-foreground-muted">
                <i class="pi pi-sync" [class.text-done-foreground]="profileService.profile()!.styleConsistency >= 60"
                                       [class.text-inprogress-foreground]="profileService.profile()!.styleConsistency < 60"></i>
                <span>Style consistency:
                  <strong [class.text-done-foreground]="profileService.profile()!.styleConsistency >= 60"
                          [class.text-inprogress-foreground]="profileService.profile()!.styleConsistency < 60">
                    {{ profileService.profile()!.styleConsistency }}%
                  </strong>
                  ({{ profileService.profile()!.styleConsistency >= 75 ? 'stable' : profileService.profile()!.styleConsistency >= 50 ? 'moderate' : 'variable' }})
                </span>
              </div>
            </div>

            <!-- Right: Radar chart -->
            <div class="flex-shrink-0 flex items-center justify-center">
              <svg viewBox="0 0 300 260" width="280" height="240" role="img" [attr.aria-label]="radarAriaLabel()">
                <!-- Radar grid (3 levels) -->
                <g transform="translate(150, 130)">
                  <!-- Outer hexagon -->
                  <polygon [attr.points]="hexagonPoints(100)"
                           fill="none" stroke="var(--color-border-default)" stroke-width="1" opacity="0.5"/>
                  <!-- Mid hexagon -->
                  <polygon [attr.points]="hexagonPoints(66)"
                           fill="none" stroke="var(--color-border-default)" stroke-width="1" opacity="0.3"/>
                  <!-- Inner hexagon -->
                  <polygon [attr.points]="hexagonPoints(33)"
                           fill="none" stroke="var(--color-border-default)" stroke-width="1" opacity="0.2"/>
                  <!-- Axes -->
                  @for (axis of axisEndpoints; track $index) {
                    <line x1="0" y1="0" [attr.x2]="axis.x" [attr.y2]="axis.y"
                          stroke="var(--color-border-default)" stroke-width="0.5" opacity="0.3"/>
                  }
                  <!-- Data polygon -->
                  <polygon [attr.points]="dataPolygonPoints()"
                           fill="var(--color-primary-bg)" stroke="var(--color-primary-solid)" stroke-width="2" opacity="0.8"/>
                  <!-- Data dots -->
                  @for (point of dataPoints(); track $index) {
                    <circle [attr.cx]="point.x" [attr.cy]="point.y"
                            [attr.r]="$index === 0 ? 4 : 3"
                            fill="var(--color-primary-solid)"
                            [attr.opacity]="$index === 0 ? 1 : 0.7"/>
                  }
                </g>
                <!-- Labels -->
                @for (label of radarLabels(); track $index) {
                  <text [attr.x]="label.x" [attr.y]="label.y"
                        [attr.text-anchor]="label.anchor"
                        [attr.font-size]="label.bold ? 11 : 11"
                        [attr.font-weight]="label.bold ? 600 : 400"
                        [attr.fill]="label.bold ? 'var(--color-primary-text)' : 'var(--color-text-muted)'">
                    {{ label.text }}
                  </text>
                }
              </svg>
            </div>
          </div>

          <!-- Context shifts + Strengths/Growth -->
          @if (profileService.profile()!.contextShifts.length > 0 || profileService.profile()!.strengths.length > 0) {
            <div class="border-t border-border grid grid-cols-1 md:grid-cols-2 divide-y md:divide-y-0 md:divide-x divide-border">
              <!-- Context shifts -->
              <div class="p-5">
                <h4 class="text-xs font-semibold text-foreground-muted uppercase tracking-wider mb-3">Context Shifts</h4>
                @if (profileService.profile()!.contextShifts.length > 0) {
                  <div class="space-y-2.5">
                    @for (shift of profileService.profile()!.contextShifts; track shift.context) {
                      <div class="flex items-start gap-2">
                        <span class="w-5 h-5 rounded-full flex items-center justify-center shrink-0 mt-0.5"
                              [class.bg-accent]="shift.icon === 'pi-user'"
                              [class.bg-inprogress]="shift.icon === 'pi-users'"
                              [class.bg-done]="shift.icon === 'pi-sitemap'">
                          <i class="pi text-[10px]"
                             [class.pi-user]="shift.icon === 'pi-user'"
                             [class.pi-users]="shift.icon === 'pi-users'"
                             [class.pi-sitemap]="shift.icon === 'pi-sitemap'"
                             [class.text-accent-foreground]="shift.icon === 'pi-user'"
                             [class.text-inprogress-foreground]="shift.icon === 'pi-users'"
                             [class.text-done-foreground]="shift.icon === 'pi-sitemap'"></i>
                        </span>
                        <div>
                          <p class="text-sm text-foreground">{{ shift.context }} &rarr; <strong>{{ shift.archetype }}</strong></p>
                          <p class="text-xs text-foreground-muted">{{ shift.description }}</p>
                        </div>
                      </div>
                    }
                  </div>
                } @else {
                  <p class="text-sm text-foreground-muted">Not enough varied meeting types to detect shifts yet.</p>
                }
              </div>

              <!-- Strengths & Growth -->
              <div class="p-5">
                <div class="grid grid-cols-2 gap-4">
                  <div>
                    <h4 class="text-xs font-semibold text-done-foreground uppercase tracking-wider mb-2">Strengths</h4>
                    <ul class="space-y-1.5 text-sm text-foreground-secondary">
                      @for (strength of profileService.profile()!.strengths; track strength) {
                        <li class="flex items-start gap-1.5">
                          <i class="pi pi-check-circle text-done-foreground text-xs mt-0.5"></i> {{ strength }}
                        </li>
                      }
                    </ul>
                  </div>
                  <div>
                    <h4 class="text-xs font-semibold text-inprogress-foreground uppercase tracking-wider mb-2">Growth Areas</h4>
                    <ul class="space-y-1.5 text-sm text-foreground-secondary">
                      @for (area of profileService.profile()!.growthAreas; track area) {
                        <li class="flex items-start gap-1.5">
                          <i class="pi pi-arrow-up-right text-inprogress-foreground text-xs mt-0.5"></i> {{ area }}
                        </li>
                      }
                    </ul>
                  </div>
                </div>
              </div>
            </div>
          }
        </div>
      }
    </section>
  `,
})
export class CommunicationProfileComponent {
  protected readonly profileService = inject(CommunicationProfileService);
  protected readonly insightsService = inject(InsightsService);

  // Hexagon axis endpoints (6 vertices at 60-degree intervals starting from top)
  protected readonly axisEndpoints = [
    { x: 0, y: -100 },      // Top (0°)
    { x: 86.6, y: -50 },    // Top-right (60°)
    { x: 86.6, y: 50 },     // Bottom-right (120°)
    { x: 0, y: 100 },       // Bottom (180°)
    { x: -86.6, y: 50 },    // Bottom-left (240°)
    { x: -86.6, y: -50 },   // Top-left (300°)
  ];

  // Archetype order for radar chart (matches axis positions)
  private readonly archetypeOrder = ['Facilitator', 'Challenger', 'Driver', 'Supporter', 'Mediator', 'Observer'];

  protected readonly meetingsRemaining = computed(() => {
    const profile = this.profileService.profile();
    if (!profile) return 5;
    return Math.max(0, profile.minimumMeetings - profile.meetingCount);
  });

  protected readonly progressDots = computed(() => {
    const profile = this.profileService.profile();
    if (!profile) return [false, false, false, false, false];
    return Array.from({ length: profile.minimumMeetings }, (_, i) => i < profile.meetingCount);
  });

  protected readonly progressAriaLabel = computed(() => {
    const profile = this.profileService.profile();
    if (!profile) return 'Progress: 0 of 5 meetings analyzed';
    return `Progress: ${profile.meetingCount} of ${profile.minimumMeetings} meetings analyzed`;
  });

  protected readonly dataPoints = computed(() => {
    const profile = this.profileService.profile();
    if (!profile?.hasEnoughData) return [];
    return this.getDataCoordinates(profile.scores);
  });

  protected readonly radarAriaLabel = computed(() => {
    const profile = this.profileService.profile();
    if (!profile?.hasEnoughData) return 'Communication style radar chart';
    const scoreText = profile.scores
      .map(s => `${s.name}: ${Math.round(s.score)}%`)
      .join(', ');
    return `Communication style radar chart. ${scoreText}`;
  });

  protected readonly radarLabels = computed(() => {
    const profile = this.profileService.profile();
    if (!profile?.hasEnoughData) return [];

    const primary = profile.primaryArchetype;
    // Label positions (outside the hexagon)
    return [
      { x: 150, y: 18, anchor: 'middle', text: this.archetypeOrder[0], bold: this.archetypeOrder[0] === primary },
      { x: 260, y: 72, anchor: 'start', text: this.archetypeOrder[1], bold: this.archetypeOrder[1] === primary },
      { x: 260, y: 192, anchor: 'start', text: this.archetypeOrder[2], bold: this.archetypeOrder[2] === primary },
      { x: 150, y: 252, anchor: 'middle', text: this.archetypeOrder[3], bold: this.archetypeOrder[3] === primary },
      { x: 40, y: 192, anchor: 'end', text: this.archetypeOrder[4], bold: this.archetypeOrder[4] === primary },
      { x: 40, y: 72, anchor: 'end', text: this.archetypeOrder[5], bold: this.archetypeOrder[5] === primary },
    ];
  });

  constructor() {
    // Reload profile when date range changes
    effect(() => {
      const range = this.insightsService.dateRange();
      this.profileService.loadProfile(range);
    });
  }

  protected hexagonPoints(radius: number): string {
    return this.archetypeOrder.map((_, i) => {
      const angle = (Math.PI / 2) + (i * 2 * Math.PI / 6);
      const x = Math.round(radius * Math.cos(angle) * 10) / 10;
      const y = Math.round(-radius * Math.sin(angle) * 10) / 10;
      return `${x},${y}`;
    }).join(' ');
  }

  protected dataPolygonPoints(): string {
    const profile = this.profileService.profile();
    if (!profile?.hasEnoughData) return '';
    return this.getDataCoordinates(profile.scores)
      .map(p => `${p.x},${p.y}`)
      .join(' ');
  }

  private getDataCoordinates(scores: ArchetypeScore[]): { x: number; y: number }[] {
    const scoreMap = new Map(scores.map(s => [s.name, s.score]));
    return this.archetypeOrder.map((name, i) => {
      const score = scoreMap.get(name) ?? 0;
      const radius = score; // Score is 0-100, matching our 100-unit radius
      const angle = (Math.PI / 2) + (i * 2 * Math.PI / 6);
      return {
        x: Math.round(radius * Math.cos(angle) * 10) / 10,
        y: Math.round(-radius * Math.sin(angle) * 10) / 10,
      };
    });
  }
}
