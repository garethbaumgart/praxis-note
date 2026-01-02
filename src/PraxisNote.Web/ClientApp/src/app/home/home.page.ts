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
        <h1 class="text-3xl lg:text-4xl font-bold text-gray-900 mb-3">
          Welcome back, {{ firstName() }}
        </h1>
        <p class="text-gray-500 text-lg">Ready to capture your thoughts and turn them into action.</p>
      </div>

      <!-- Quick actions -->
      <div class="grid grid-cols-1 sm:grid-cols-3 gap-4 mb-10">
        <button class="group p-4 bg-white border border-gray-200 rounded-xl hover:border-violet-300 hover:shadow-md transition-all duration-200 text-left" aria-label="Create new note">
          <div class="w-10 h-10 rounded-lg bg-violet-100 flex items-center justify-center mb-3 group-hover:bg-violet-200 transition-colors">
            <i class="pi pi-plus text-violet-600" aria-hidden="true"></i>
          </div>
          <p class="font-medium text-gray-900 mb-1">New Note</p>
          <p class="text-sm text-gray-500">Start writing</p>
        </button>
        <button
          class="group p-4 bg-white border border-gray-200 rounded-xl hover:border-violet-300 hover:shadow-md transition-all duration-200 text-left"
          aria-label="Create new task"
          (click)="goToTasks()"
        >
          <div class="w-10 h-10 rounded-lg bg-emerald-100 flex items-center justify-center mb-3 group-hover:bg-emerald-200 transition-colors">
            <i class="pi pi-check-square text-emerald-600" aria-hidden="true"></i>
          </div>
          <p class="font-medium text-gray-900 mb-1">Tasks</p>
          <p class="text-sm text-gray-500">View your board</p>
        </button>
        <button class="group p-4 bg-white border border-gray-200 rounded-xl hover:border-violet-300 hover:shadow-md transition-all duration-200 text-left" aria-label="Search">
          <div class="w-10 h-10 rounded-lg bg-amber-100 flex items-center justify-center mb-3 group-hover:bg-amber-200 transition-colors">
            <i class="pi pi-search text-amber-600" aria-hidden="true"></i>
          </div>
          <p class="font-medium text-gray-900 mb-1">Search</p>
          <p class="text-sm text-gray-500">Find anything</p>
        </button>
      </div>

      <!-- Empty state card -->
      <div class="bg-white border border-gray-200 rounded-2xl p-8 lg:p-12 text-center">
        <div class="w-16 h-16 rounded-2xl bg-gradient-to-br from-violet-100 to-purple-100 flex items-center justify-center mx-auto mb-5">
          <i class="pi pi-file-edit text-3xl text-violet-600"></i>
        </div>
        <h2 class="text-xl font-semibold text-gray-900 mb-2">Your workspace is ready</h2>
        <p class="text-gray-500 mb-8 max-w-md mx-auto">
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
