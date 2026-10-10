import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from './core/auth.service';

@Component({
  selector: 'app-root',
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  template: `
    <header class="shell-header">
      <a routerLink="/" class="brand" aria-label="Anuradhapura AI home">
        <span class="brand-mark" aria-hidden="true">A</span>
        <span>
          <strong>Anuradhapura AI</strong>
          <small>Forecasting and crop recommendation</small>
        </span>
      </a>

      <nav class="top-nav" aria-label="Primary navigation">
        <a routerLink="/" routerLinkActive="active" [routerLinkActiveOptions]="{ exact: true }">Dashboard</a>
        <a routerLink="/recommendations" routerLinkActive="active">Recommendations</a>
        @if (auth.hasRole('Agricultural Officer')) {
          <a routerLink="/officer" routerLinkActive="active">Officer</a>
        }
        @if (auth.hasRole('Administrator')) {
          <a routerLink="/admin" routerLinkActive="active">Admin</a>
        }
        @if (auth.hasRole('Registered User')) {
          <a routerLink="/history" routerLinkActive="active">History</a>
        }
      </nav>

      <div class="session-panel">
        @if (auth.currentUser(); as user) {
          <span>{{ user.email }}</span>
          <button type="button" class="link-button" (click)="auth.logout()">Logout</button>
        } @else {
          <a routerLink="/login">Login</a>
          <a routerLink="/register" class="primary-link">Register</a>
        }
      </div>
    </header>

    <main class="app-shell">
      <router-outlet />
    </main>
  `,
  styles: [],
})
export class App {
  constructor(public readonly auth: AuthService) {}
}
