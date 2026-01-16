import { Component, inject, computed } from '@angular/core';
import { Router } from '@angular/router';
import { Button } from 'primeng/button';
import { AuthService } from '../auth';

@Component({
  selector: 'app-home-page',
  standalone: true,
  imports: [Button],
  template: `
    <div class="max-w-4xl mx-auto px-6 py-10 lg:py-16">
      <!-- Welcome section -->
      <div class="mb-10">
        <h1 class="text-3xl lg:text-4xl font-bold text-foreground mb-3">
          Welcome back, {{ firstName() }}
        </h1>
        <p class="text-foreground-secondary text-lg">Ready to capture your thoughts and turn them into action.</p>
      </div>

      <!-- Quick actions -->
      <div class="grid grid-cols-1 sm:grid-cols-3 gap-4 mb-10">
        <!-- New Note - Coming Soon -->
        <button
          type="button"
          class="group relative p-4 bg-surface border border-border rounded-xl transition-all duration-200 text-left opacity-70 cursor-not-allowed"
          aria-label="Create new note - Coming soon"
          disabled
          aria-disabled="true"
        >
          <span class="absolute top-2 right-2 inline-flex items-center gap-1 px-2 py-0.5 text-xs font-medium rounded-full bg-amber-100 text-amber-700 dark:bg-amber-900 dark:text-amber-300">
            <i class="pi pi-clock text-[10px]" aria-hidden="true"></i>
            Soon
          </span>
          <div class="w-10 h-10 rounded-lg bg-accent flex items-center justify-center mb-3">
            <i class="pi pi-plus text-accent-foreground" aria-hidden="true"></i>
          </div>
          <p class="font-medium text-foreground mb-1">New Note</p>
          <p class="text-sm text-foreground-secondary">Start writing</p>
        </button>

        <!-- Tasks - Live -->
        <button
          class="group relative p-4 bg-surface border border-border rounded-xl hover:border-done-foreground hover:shadow-md transition-all duration-200 text-left"
          aria-label="View tasks"
          (click)="goToTasks()"
        >
          <span class="absolute top-2 right-2 inline-flex items-center gap-1 px-2 py-0.5 text-xs font-medium rounded-full bg-green-100 text-green-700 dark:bg-green-900 dark:text-green-300">
            <i class="pi pi-check-circle text-[10px]" aria-hidden="true"></i>
            Live
          </span>
          <div class="w-10 h-10 rounded-lg bg-done flex items-center justify-center mb-3 group-hover:bg-done-hover transition-colors">
            <i class="pi pi-check-square text-done-foreground" aria-hidden="true"></i>
          </div>
          <p class="font-medium text-foreground mb-1">Tasks</p>
          <p class="text-sm text-foreground-secondary">View your board</p>
        </button>

        <!-- Search - Coming Soon -->
        <button
          type="button"
          class="group relative p-4 bg-surface border border-border rounded-xl transition-all duration-200 text-left opacity-70 cursor-not-allowed"
          aria-label="Search - Coming soon"
          disabled
          aria-disabled="true"
        >
          <span class="absolute top-2 right-2 inline-flex items-center gap-1 px-2 py-0.5 text-xs font-medium rounded-full bg-amber-100 text-amber-700 dark:bg-amber-900 dark:text-amber-300">
            <i class="pi pi-clock text-[10px]" aria-hidden="true"></i>
            Soon
          </span>
          <div class="w-10 h-10 rounded-lg bg-amber-100 flex items-center justify-center mb-3">
            <i class="pi pi-search text-amber-600" aria-hidden="true"></i>
          </div>
          <p class="font-medium text-foreground mb-1">Search</p>
          <p class="text-sm text-foreground-secondary">Find anything</p>
        </button>
      </div>

      <!-- Empty state card -->
      <div class="bg-surface border border-border rounded-2xl p-8 lg:p-12 text-center">
        <div class="w-16 h-16 rounded-2xl bg-gradient-to-br from-green-100 to-emerald-100 flex items-center justify-center mx-auto mb-5">
          <i class="pi pi-check-square text-3xl text-done-foreground" aria-hidden="true"></i>
        </div>
        <h2 class="text-xl font-semibold text-foreground mb-2">Your workspace is ready</h2>
        <p class="text-foreground-secondary mb-8 max-w-md mx-auto">
          Start organizing your work with tasks on a kanban board.
        </p>
        <p-button label="Create your first task" icon="pi pi-plus" (onClick)="goToTasks()" />
      </div>
    </div>
  `,
})
export class HomePage {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);

  readonly firstName = computed(() => {
    const name = this.auth.user()?.name;
    return name?.split(' ')[0] ?? '';
  });

  goToTasks(): void {
    this.router.navigate(['/tasks']);
  }
}
