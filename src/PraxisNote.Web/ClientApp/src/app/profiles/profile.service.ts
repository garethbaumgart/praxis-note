import { Injectable, inject, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Profile } from './profile.model';
import { ToastService } from '../shared/services/toast.service';
import { TaskService } from '../tasks/task.service';
import { NoteService } from '../notes/note.service';
import { MeetingService } from '../meetings/meeting.service';
import { TagService } from '../tags/tag.service';
import { SummaryService } from '../summary/summary.service';

const ACTIVE_PROFILE_KEY = 'praxis_active_profile_id';

@Injectable({ providedIn: 'root' })
export class ProfileService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);
  private readonly taskService = inject(TaskService);
  private readonly noteService = inject(NoteService);
  private readonly meetingService = inject(MeetingService);
  private readonly tagService = inject(TagService);
  private readonly summaryService = inject(SummaryService);

  private readonly _profiles = signal<Profile[]>([]);
  private readonly _activeProfileId = signal<string | null>(null);
  private readonly _loading = signal(false);
  private readonly _error = signal<string | null>(null);

  readonly profiles = this._profiles.asReadonly();
  readonly activeProfileId = this._activeProfileId.asReadonly();
  readonly loading = this._loading.asReadonly();
  readonly error = this._error.asReadonly();

  readonly activeProfile = computed(() =>
    this._profiles().find(p => p.id === this._activeProfileId()) ?? null
  );

  readonly hasMultipleProfiles = computed(() => this._profiles().length > 1);

  /**
   * Initialize profiles from the /api/auth/me response.
   * Sets the active profile ID from localStorage or falls back to the default profile.
   */
  initFromUser(profiles: Profile[]): void {
    this._profiles.set(profiles);

    const storedId = localStorage.getItem(ACTIVE_PROFILE_KEY);
    const storedProfile = storedId ? profiles.find(p => p.id === storedId) : null;
    const defaultProfile = profiles.find(p => p.isDefault) ?? profiles[0];

    const activeId = storedProfile?.id ?? defaultProfile?.id ?? null;
    this._activeProfileId.set(activeId);

    if (activeId) {
      localStorage.setItem(ACTIVE_PROFILE_KEY, activeId);
    }
  }

  /**
   * Load full profile list from the API (used by Settings page to get fresh data).
   */
  loadProfiles(): void {
    this._loading.set(true);
    this._error.set(null);
    this.http.get<Profile[]>('/api/profiles').subscribe({
      next: (profiles) => {
        this._profiles.set(profiles);
        this._loading.set(false);
      },
      error: () => {
        this._error.set('Failed to load profiles');
        this._loading.set(false);
      },
    });
  }

  /**
   * Create a new profile.
   */
  createProfile(name: string, icon: string | null, onSuccess?: () => void): void {
    this._loading.set(true);
    this.http.post<{ id: string }>('/api/profiles', { name, icon }).subscribe({
      next: () => {
        this.toast.success({ summary: 'Profile created', detail: `"${name}" has been created.` });
        this.loadProfiles();
        onSuccess?.();
      },
      error: (err) => {
        this._loading.set(false);
        const message = err.error?.error ?? 'Failed to create profile';
        this.toast.error(message);
      },
    });
  }

  /**
   * Update an existing profile.
   */
  updateProfile(id: string, name: string, icon: string | null, onSuccess?: () => void): void {
    this.http.put(`/api/profiles/${id}`, { name, icon }).subscribe({
      next: () => {
        this._profiles.update(profiles =>
          profiles.map(p => p.id === id ? { ...p, name, icon } : p)
        );
        this.toast.success({ summary: 'Profile updated' });
        onSuccess?.();
      },
      error: (err) => {
        const message = err.error?.error ?? 'Failed to update profile';
        this.toast.error(message);
      },
    });
  }

  /**
   * Delete a profile. Profile must be empty (no data) and not the default.
   */
  deleteProfile(id: string, onSuccess?: () => void): void {
    this.http.delete(`/api/profiles/${id}`).subscribe({
      next: () => {
        this._profiles.update(profiles => profiles.filter(p => p.id !== id));
        // If the deleted profile was active, switch to default
        if (this._activeProfileId() === id) {
          const defaultProfile = this._profiles().find(p => p.isDefault) ?? this._profiles()[0];
          if (defaultProfile) {
            this.switchProfile(defaultProfile.id);
          }
        }
        this.toast.success({ summary: 'Profile deleted' });
        onSuccess?.();
      },
      error: (err) => {
        const message = err.error?.error ?? 'Failed to delete profile';
        this.toast.error(message);
      },
    });
  }

  /**
   * Set a profile as the default.
   */
  setDefaultProfile(id: string): void {
    this.http.post(`/api/profiles/${id}/default`, {}).subscribe({
      next: () => {
        this._profiles.update(profiles =>
          profiles.map(p => ({ ...p, isDefault: p.id === id }))
        );
        this.toast.success({ summary: 'Default profile updated' });
      },
      error: () => {
        this.toast.error('Failed to set default profile');
      },
    });
  }

  /**
   * Switch to a different profile. Updates the active profile ID,
   * persists to localStorage, reloads all data services, and navigates to Home.
   */
  switchProfile(id: string): void {
    if (this._activeProfileId() === id) return;

    this._activeProfileId.set(id);
    localStorage.setItem(ACTIVE_PROFILE_KEY, id);

    // Reload all data services — the interceptor will now send the new X-Profile-Id
    this.reloadAllData();

    // Navigate to home to avoid stale state
    this.router.navigate(['/home']);
  }

  /**
   * Reload all data services. Called after profile switch.
   */
  private reloadAllData(): void {
    this.taskService.loadTasks();
    this.noteService.loadNotes();
    this.meetingService.loadMeetings();
    this.tagService.loadTags();
    this.summaryService.loadSummary();
  }
}
