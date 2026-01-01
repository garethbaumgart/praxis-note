import { Component, signal, inject, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Card } from 'primeng/card';
import { Tag } from 'primeng/tag';

@Component({
  selector: 'app-root',
  imports: [Card, Tag],
  templateUrl: './app.html',
})
export class App {
  private readonly http = inject(HttpClient);

  protected readonly title = signal('PraxisNote');
  protected readonly apiStatus = signal<'checking...' | 'healthy' | 'error'>('checking...');
  protected readonly apiSeverity = computed(() => {
    const status = this.apiStatus();
    if (status === 'healthy') return 'success';
    if (status === 'error') return 'danger';
    return 'info';
  });

  constructor() {
    this.http.get<{ status: string }>('/api/health')
      .subscribe({
        next: (res) => this.apiStatus.set(res.status as 'healthy'),
        error: () => this.apiStatus.set('error')
      });
  }
}
