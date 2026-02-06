import { Injectable, signal, TemplateRef } from '@angular/core';

export interface BreadcrumbItem {
  label: string;
  icon?: string;
  route?: string;
}

@Injectable({ providedIn: 'root' })
export class ContextualHeaderService {
  /** Breadcrumb items set by child routes (null = no contextual breadcrumb) */
  readonly breadcrumb = signal<BreadcrumbItem[] | null>(null);

  /** Template ref for page-specific action buttons (save, delete, export, etc.) */
  readonly actionsTemplate = signal<TemplateRef<unknown> | null>(null);

  /** Called by child routes in ngOnDestroy to clean up */
  clearContext(): void {
    this.breadcrumb.set(null);
    this.actionsTemplate.set(null);
  }
}
