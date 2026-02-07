import { Component, ChangeDetectionStrategy, input, output, signal, computed } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Dialog } from 'primeng/dialog';
import { InputText } from 'primeng/inputtext';
import { InputNumber } from 'primeng/inputnumber';
import { Select } from 'primeng/select';
import { GOAL_PRESETS, GoalPreset, MetricType, GoalOperator } from './insights.model';

@Component({
  selector: 'app-add-goal-dialog',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, Dialog, InputText, InputNumber, Select],
  template: `
    <p-dialog
      header="Add Goal"
      [visible]="visible()"
      (visibleChange)="onDialogVisibleChange($event)"
      [modal]="true"
      [style]="{ width: '30rem' }"
      [draggable]="false"
      [resizable]="false"
      [dismissableMask]="true">

      @if (!showCustom()) {
        <!-- Preset selection -->
        <p class="text-sm text-foreground-muted mb-3">Choose a preset or create your own:</p>
        <div class="flex flex-col gap-2 mb-4">
          @for (preset of presets; track preset.title) {
            <button type="button"
                    class="text-left p-3 rounded-lg border border-border hover:bg-surface-hover transition"
                    (click)="selectPreset(preset)">
              <p class="text-sm font-medium text-foreground">{{ preset.title }}</p>
              <p class="text-xs text-foreground-muted mt-0.5">{{ preset.description }}</p>
            </button>
          }
        </div>
        <button type="button"
                class="w-full text-center text-sm text-accent-foreground hover:underline"
                (click)="showCustom.set(true)">
          Create custom goal
        </button>
      } @else {
        <!-- Custom goal form -->
        <div class="flex flex-col gap-3">
          <div>
            <label class="block text-xs font-medium text-foreground-muted mb-1">Title</label>
            <input pInputText
                   class="w-full"
                   [value]="customTitle()"
                   (input)="customTitle.set($any($event.target).value)"
                   placeholder="e.g. Keep talk time under 40%" />
          </div>
          <div>
            <label class="block text-xs font-medium text-foreground-muted mb-1">Metric</label>
            <p-select
              [options]="metricOptions"
              optionLabel="label"
              optionValue="value"
              [ngModel]="customMetric()"
              (ngModelChange)="customMetric.set($event)"
              placeholder="Select metric"
              styleClass="w-full" />
          </div>
          <div>
            <label class="block text-xs font-medium text-foreground-muted mb-1">Condition</label>
            <p-select
              [options]="operatorOptions"
              optionLabel="label"
              optionValue="value"
              [ngModel]="customOperator()"
              (ngModelChange)="customOperator.set($event)"
              placeholder="Select condition"
              styleClass="w-full" />
          </div>
          <div class="flex gap-2">
            <div class="flex-1">
              <label class="block text-xs font-medium text-foreground-muted mb-1">Target value</label>
              <p-inputNumber
                [ngModel]="customTarget()"
                (ngModelChange)="customTarget.set($event)"
                [minFractionDigits]="0"
                [maxFractionDigits]="2"
                styleClass="w-full" />
            </div>
            @if (customOperator() === 'Between') {
              <div class="flex-1">
                <label class="block text-xs font-medium text-foreground-muted mb-1">Upper bound</label>
                <p-inputNumber
                  [ngModel]="customTargetUpper()"
                  (ngModelChange)="customTargetUpper.set($event)"
                  [minFractionDigits]="0"
                  [maxFractionDigits]="2"
                  styleClass="w-full" />
              </div>
            }
          </div>
          <div class="flex justify-end gap-2 mt-2">
            <button type="button"
                    class="px-4 py-2 text-sm text-foreground-muted hover:text-foreground transition"
                    (click)="showCustom.set(false)">
              Back
            </button>
            <button type="button"
                    class="px-4 py-2 bg-accent-solid text-white rounded-lg text-sm font-medium hover:opacity-90 transition"
                    [disabled]="!isCustomValid()"
                    (click)="submitCustom()">
              Add Goal
            </button>
          </div>
        </div>
      }
    </p-dialog>
  `,
})
export class AddGoalDialogComponent {
  readonly visible = input.required<boolean>();
  readonly onClose = output<void>();
  readonly onAdd = output<{
    metricType: string;
    operator: string;
    targetValue: number;
    targetValueUpper: number | null;
    title: string;
  }>();

  protected readonly presets = GOAL_PRESETS;
  protected readonly showCustom = signal(false);
  protected readonly customTitle = signal('');
  protected readonly customMetric = signal<MetricType | null>(null);
  protected readonly customOperator = signal<GoalOperator | null>(null);
  protected readonly customTarget = signal<number | null>(null);
  protected readonly customTargetUpper = signal<number | null>(null);

  protected readonly metricOptions = [
    { label: 'Talk Time %', value: 'TalkTimePercentage' },
    { label: 'Question Ratio', value: 'QuestionRatio' },
    { label: 'Interruption Count', value: 'InterruptionCount' },
    { label: 'Sentiment Score', value: 'SentimentScore' },
    { label: 'Red Flag Count', value: 'RedFlagCount' },
  ];

  protected readonly operatorOptions = [
    { label: 'Less than (<)', value: 'LessThan' },
    { label: 'At most (≤)', value: 'LessThanOrEqual' },
    { label: 'Greater than (>)', value: 'GreaterThan' },
    { label: 'At least (≥)', value: 'GreaterThanOrEqual' },
    { label: 'Between', value: 'Between' },
  ];

  protected readonly isCustomValid = computed(() =>
    this.customTitle().trim().length > 0 &&
    this.customMetric() !== null &&
    this.customOperator() !== null &&
    this.customTarget() !== null &&
    (this.customOperator() !== 'Between' || this.customTargetUpper() !== null),
  );

  protected onDialogVisibleChange(visible: boolean): void {
    if (!visible) {
      this.resetForm();
      this.onClose.emit();
    }
  }

  protected selectPreset(preset: GoalPreset): void {
    this.onAdd.emit({
      metricType: preset.metricType,
      operator: preset.operator,
      targetValue: preset.targetValue,
      targetValueUpper: preset.targetValueUpper,
      title: preset.title,
    });
    this.resetForm();
  }

  protected submitCustom(): void {
    if (!this.isCustomValid()) return;
    this.onAdd.emit({
      metricType: this.customMetric()!,
      operator: this.customOperator()!,
      targetValue: this.customTarget()!,
      targetValueUpper: this.customOperator() === 'Between' ? this.customTargetUpper() : null,
      title: this.customTitle().trim(),
    });
    this.resetForm();
  }

  protected resetForm(): void {
    this.showCustom.set(false);
    this.customTitle.set('');
    this.customMetric.set(null);
    this.customOperator.set(null);
    this.customTarget.set(null);
    this.customTargetUpper.set(null);
  }
}
