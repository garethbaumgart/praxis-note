import { Component, ChangeDetectionStrategy, input, output } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CheckboxModule } from 'primeng/checkbox';
import { ActionItem } from './meeting.model';

@Component({
  selector: 'app-meeting-action-items',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, CheckboxModule],
  template: `
    <div>
      <h4 class="text-xs font-semibold text-foreground-muted uppercase tracking-wide mb-2">Action Items</h4>
      @if (actionItems().length === 0) {
        <p class="text-sm text-foreground-muted italic">No action items were identified in this meeting.</p>
      } @else {
        <ul class="space-y-2">
          @for (item of actionItems(); track item.id) {
            <li class="flex items-start gap-3">
              <p-checkbox
                [binary]="true"
                [ngModel]="item.isCompleted"
                (onChange)="onToggle.emit(item.id)"
                [inputId]="'action-' + item.id"
              />
              <div class="flex-1 min-w-0">
                <label
                  [for]="'action-' + item.id"
                  class="text-sm cursor-pointer"
                  [class.line-through]="item.isCompleted"
                  [class.text-foreground-muted]="item.isCompleted"
                  [class.text-foreground]="!item.isCompleted"
                >
                  {{ item.description }}
                </label>
                @if (item.assignee) {
                  <span class="ml-2 inline-flex items-center px-2 py-0.5 rounded-full text-xs bg-accent text-accent-foreground">
                    {{ item.assignee }}
                  </span>
                }
              </div>
            </li>
          }
        </ul>
      }
    </div>
  `,
})
export class MeetingActionItemsComponent {
  readonly actionItems = input.required<ActionItem[]>();
  readonly onToggle = output<string>();
}
