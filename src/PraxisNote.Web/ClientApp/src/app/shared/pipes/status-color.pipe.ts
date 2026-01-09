import { Pipe, PipeTransform } from '@angular/core';

type TaskStatus = 'Todo' | 'InProgress' | 'Done';
type ColorVariant = 'bg' | 'text' | 'border' | 'text-muted' | 'bg-hover';

const STATUS_MAP: Record<TaskStatus, string> = {
  'Todo': 'todo',
  'InProgress': 'inprogress',
  'Done': 'done',
};

const VARIANT_MAP: Record<ColorVariant, { prefix: string; suffix: string }> = {
  'bg': { prefix: 'bg-', suffix: '' },
  'text': { prefix: 'text-', suffix: '-foreground' },
  'border': { prefix: 'border-', suffix: '-border' },
  'text-muted': { prefix: 'text-', suffix: '-foreground-muted' },
  'bg-hover': { prefix: 'bg-', suffix: '-hover' },
};

@Pipe({
  name: 'statusColor',
  standalone: true,
  pure: true,
})
export class StatusColorPipe implements PipeTransform {
  transform(status: TaskStatus, variant: ColorVariant): string {
    const statusKey = STATUS_MAP[status];
    const { prefix, suffix } = VARIANT_MAP[variant];
    return `${prefix}${statusKey}${suffix}`;
  }
}
