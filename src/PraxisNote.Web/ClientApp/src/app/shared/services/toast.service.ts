import { Injectable, inject } from '@angular/core';
import { MessageService } from 'primeng/api';

export interface ToastAction {
  label: string;
  callback: () => void;
}

export interface SuccessToastOptions {
  summary: string;
  detail?: string;
  action?: ToastAction;
  life?: number;
}

@Injectable({ providedIn: 'root' })
export class ToastService {
  private readonly messageService = inject(MessageService);

  error(summary: string, detail?: string): void {
    this.messageService.add({
      severity: 'error',
      summary,
      detail,
      life: 5000,
    });
  }

  success(options: SuccessToastOptions): void {
    this.messageService.add({
      severity: 'success',
      summary: options.summary,
      detail: options.detail,
      life: options.life ?? 5000,
      data: options.action,
    });
  }

  /** Clear a specific toast by its key, or all toasts if no key provided */
  clear(key?: string): void {
    this.messageService.clear(key);
  }
}
