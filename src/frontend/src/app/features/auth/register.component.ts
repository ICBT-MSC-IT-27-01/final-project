import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth.service';

@Component({
  selector: 'app-register',
  imports: [FormsModule, RouterLink],
  template: `
    <section class="auth-layout">
      <form class="panel form-panel" (ngSubmit)="auth.register({ name, email, password })">
        <p class="eyebrow">Registered User only</p>
        <h1>Register</h1>
        <label>Name <input name="name" [(ngModel)]="name" required maxlength="100" autocomplete="name" /></label>
        <label>Email <input name="email" type="email" [(ngModel)]="email" required autocomplete="email" /></label>
        <label>Password <input name="password" type="password" [(ngModel)]="password" required minlength="8" autocomplete="new-password" /></label>
        <button type="submit" class="button primary" [disabled]="auth.isLoading()">Register</button>
        @if (auth.authError(); as message) {
          <div class="notice error" role="alert">{{ message }}</div>
        }
        <a routerLink="/login">Use an existing account</a>
      </form>
    </section>
  `,
})
export class RegisterComponent {
  name = '';
  email = '';
  password = '';

  constructor(public readonly auth: AuthService) {}
}
