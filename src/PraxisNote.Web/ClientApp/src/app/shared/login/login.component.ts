import { Component, output } from '@angular/core';

@Component({
  selector: 'app-login',
  standalone: true,
  templateUrl: './login.component.html',
  host: { class: 'contents' },
})
export class LoginComponent {
  readonly onLogin = output<void>();

  protected login(): void {
    this.onLogin.emit();
  }
}
