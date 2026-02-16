import { Component, output, inject, signal, ChangeDetectionStrategy, OnInit, OnDestroy } from '@angular/core';
import { AuthService } from '../../auth/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './login.component.html',
  host: { class: 'contents' },
})
export class LoginComponent implements OnInit, OnDestroy {
  protected readonly auth = inject(AuthService);
  readonly onLogin = output<void>();
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly cooldownSeconds = signal(0);
  private cooldownInterval: ReturnType<typeof setInterval> | null = null;

  ngOnInit(): void {
    const params = new URLSearchParams(window.location.search);
    const error = params.get('error');
    if (error === 'auth_failed') {
      this.errorMessage.set('Sign-in failed. Please try again.');
    } else if (error === 'missing_claims') {
      this.errorMessage.set('Could not retrieve your account details. Please try again.');
    } else if (error === 'rate_limited') {
      this.startCooldown();
    }
    // Clean the URL to remove the error query param.
    // Use a delay to ensure Angular router has finished its initial navigation,
    // otherwise the router may re-apply the query string after replaceState.
    if (error) {
      setTimeout(() => window.history.replaceState({}, '', window.location.pathname), 100);
    }
  }

  ngOnDestroy(): void {
    if (this.cooldownInterval) {
      clearInterval(this.cooldownInterval);
      this.cooldownInterval = null;
    }
  }

  protected login(): void {
    this.errorMessage.set(null);
    this.onLogin.emit();
  }

  private startCooldown(): void {
    this.errorMessage.set('Too many sign-in attempts. Please wait before trying again.');
    this.cooldownSeconds.set(30);
    this.cooldownInterval = setInterval(() => {
      this.cooldownSeconds.update(s => s - 1);
      if (this.cooldownSeconds() <= 0) {
        if (this.cooldownInterval) {
          clearInterval(this.cooldownInterval);
          this.cooldownInterval = null;
        }
        this.errorMessage.set(null);
      }
    }, 1000);
  }
}
