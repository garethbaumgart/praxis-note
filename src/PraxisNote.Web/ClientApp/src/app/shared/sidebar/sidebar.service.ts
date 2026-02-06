import { Injectable, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class SidebarService {
  private readonly STORAGE_KEY = 'praxisnote.sidebar.collapsed';

  readonly collapsed = signal(this.loadState());

  toggle(): void {
    this.collapsed.update(v => !v);
    try {
      localStorage.setItem(this.STORAGE_KEY, String(this.collapsed()));
    } catch {
      // localStorage may be unavailable in privacy-restricted environments
    }
  }

  private loadState(): boolean {
    try {
      return localStorage.getItem(this.STORAGE_KEY) === 'true';
    } catch {
      return false;
    }
  }
}
