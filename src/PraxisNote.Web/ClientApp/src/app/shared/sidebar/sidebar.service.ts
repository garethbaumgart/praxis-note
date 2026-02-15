import { Injectable, signal, WritableSignal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class SidebarService {
  private readonly STORAGE_KEY = 'praxisnote.sidebar.collapsed';
  private readonly SECTION_KEYS = {
    inProgress: 'praxisnote.sidebar.inprogress.collapsed',
    upNext: 'praxisnote.sidebar.upnext.collapsed',
    context: 'praxisnote.sidebar.context.collapsed',
  } as const;

  readonly collapsed = signal(this.loadKey(this.STORAGE_KEY));

  readonly inProgressCollapsed = signal(this.loadKey(this.SECTION_KEYS.inProgress));
  readonly upNextCollapsed = signal(this.loadKey(this.SECTION_KEYS.upNext));
  readonly contextCollapsed = signal(this.loadKey(this.SECTION_KEYS.context));

  toggle(): void {
    this.collapsed.update(v => !v);
    this.saveKey(this.STORAGE_KEY, this.collapsed());
  }

  toggleSection(section: WritableSignal<boolean>, storageKey: string): void {
    section.update(v => !v);
    this.saveKey(storageKey, section());
  }

  readonly sectionKeys = this.SECTION_KEYS;

  private loadKey(key: string): boolean {
    try {
      return localStorage.getItem(key) === 'true';
    } catch {
      return false;
    }
  }

  private saveKey(key: string, value: boolean): void {
    try {
      localStorage.setItem(key, String(value));
    } catch { }
  }
}
