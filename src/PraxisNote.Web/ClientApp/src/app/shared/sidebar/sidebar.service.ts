import { Injectable, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class SidebarService {
  private readonly STORAGE_KEY = 'sidebar-collapsed';

  readonly collapsed = signal(this.loadState());

  toggle(): void {
    this.collapsed.update(v => !v);
    localStorage.setItem(this.STORAGE_KEY, String(this.collapsed()));
  }

  private loadState(): boolean {
    return localStorage.getItem(this.STORAGE_KEY) === 'true';
  }
}
