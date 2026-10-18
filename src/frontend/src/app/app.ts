import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { AuthService } from './core/auth.service';

@Component({
  selector: 'app-root',
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  template: `
    <div class="site-frame">
      <header class="shell-header">
        <div class="header-inner">
          <a routerLink="/" class="brand" aria-label="CropCompass home">
            <span class="brand-mark" aria-hidden="true">
              <img src="/cropcompass-mark.svg" alt="" width="32" height="32" />
            </span>
            <span class="brand-copy"><strong>CropCompass</strong><small>Weather insight for better farming in Anuradhapura</small></span>
          </a>
          <nav class="top-nav" aria-label="Primary navigation">
            <a routerLink="/" routerLinkActive="active" [routerLinkActiveOptions]="{ exact: true }">Dashboard</a>
            <a routerLink="/recommendations" routerLinkActive="active">Recommendations</a>
            <a routerLink="/seasonal-planning" routerLinkActive="active">Seasonal planning</a>
            @if (auth.hasRole('Agricultural Officer')) { <a routerLink="/officer" routerLinkActive="active">Officer</a> }
            @if (auth.hasRole('Administrator')) { <a routerLink="/admin" routerLinkActive="active">Admin</a> }
            @if (auth.hasRole('Registered User')) { <a routerLink="/history" routerLinkActive="active">History</a> }
          </nav>
          <div class="session-panel">
            @if (auth.currentUser(); as user) {
              <span class="session-user" [title]="user.email">{{ user.email }}</span>
              <button type="button" class="link-button" (click)="auth.logout()">Logout</button>
            } @else {
              <a routerLink="/login" class="login-link">Login</a>
              <a routerLink="/register" class="register-link">Register</a>
            }
          </div>
        </div>
      </header>
      <main class="app-shell"><router-outlet /></main>
      <footer class="shell-footer"><span>CROPCOMPASS</span><p>Weather insight for better farming in Anuradhapura.</p><span>Research prototype · Sri Lanka</span></footer>
    </div>
  `,
})
export class App {
  constructor(public readonly auth: AuthService) {}
}
