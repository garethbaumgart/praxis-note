import { Component, signal, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-root',
  imports: [],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App {
  private readonly http = inject(HttpClient);

  protected readonly title = signal('PraxisNote');
  protected readonly apiStatus = signal('checking...');

  constructor() {
    this.http.get<{ status: string }>('/api/health')
      .subscribe({
        next: (res) => this.apiStatus.set(res.status),
        error: () => this.apiStatus.set('error')
      });
  }
}
