import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth.service';

@Component({
  selector: 'app-login',
  imports: [FormsModule, RouterLink],
  template: `
    <section class="auth-layout">
      <form class="panel form-panel" (ngSubmit)="auth.login({ email, password })">
        <p class="eyebrow">Authenticated workflows</p>
        <h1>Login</h1>
        <label>Email <input name="email" type="email" [(ngModel)]="email" required autocomplete="email" /></label>
        <label>Password <input name="password" type="password" [(ngModel)]="password" required autocomplete="current-password" /></label>
        <button type="submit" class="button primary" [disabled]="auth.isLoading()">Login</button>
        @if (auth.authError(); as message) {
          <div class="notice error" role="alert">{{ message }}</div>
        }
        <a routerLink="/register">Create registered-user account</a>
      </form>
    </section>
  `,
})
export class LoginComponent {
  email = '';
  password = '';

  constructor(public readonly auth: AuthService) {}
}
