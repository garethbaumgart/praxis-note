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
        <button class="group p-4 bg-surface border border-border rounded-xl hover:border-accent-foreground hover:shadow-md transition-all duration-200 text-left" aria-label="Create new note">
          <div class="w-10 h-10 rounded-lg bg-accent flex items-center justify-center mb-3 group-hover:bg-accent-hover transition-colors">
            <i class="pi pi-plus text-accent-foreground" aria-hidden="true"></i>
          </div>
          <p class="font-medium text-foreground mb-1">New Note</p>
          <p class="text-sm text-foreground-secondary">Start writing</p>
        </button>
        <button
          class="group p-4 bg-surface border border-border rounded-xl hover:border-done-foreground hover:shadow-md transition-all duration-200 text-left"
          aria-label="Create new task"
          (click)="goToTasks()"
        >
          <div class="w-10 h-10 rounded-lg bg-done flex items-center justify-center mb-3 group-hover:bg-done-hover transition-colors">
            <i class="pi pi-check-square text-done-foreground" aria-hidden="true"></i>
          </div>
          <p class="font-medium text-foreground mb-1">Tasks</p>
          <p class="text-sm text-foreground-secondary">View your board</p>
        </button>
        <button class="group p-4 bg-surface border border-border rounded-xl hover:border-accent-foreground hover:shadow-md transition-all duration-200 text-left" aria-label="Search">
          <div class="w-10 h-10 rounded-lg bg-amber-100 flex items-center justify-center mb-3 group-hover:bg-amber-200 transition-colors">
            <i class="pi pi-search text-amber-600" aria-hidden="true"></i>
          </div>
          <p class="font-medium text-foreground mb-1">Search</p>
          <p class="text-sm text-foreground-secondary">Find anything</p>
        </button>
      </div>

      <!-- Empty state card -->
      <div class="bg-surface border border-border rounded-2xl p-8 lg:p-12 text-center">
        <div class="w-16 h-16 rounded-2xl bg-gradient-to-br from-violet-100 to-purple-100 flex items-center justify-center mx-auto mb-5">
          <i class="pi pi-file-edit text-3xl text-accent-foreground"></i>
        </div>
        <h2 class="text-xl font-semibold text-foreground mb-2">Your workspace is ready</h2>
        <p class="text-foreground-secondary mb-8 max-w-md mx-auto">
          Start creating notes with checkboxes that automatically become trackable tasks.
        </p>
        <p-button label="Create your first note" icon="pi pi-plus" />
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
