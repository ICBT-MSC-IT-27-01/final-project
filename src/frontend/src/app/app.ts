import { Component, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { filter } from 'rxjs';
import { AuthService } from './core/auth.service';

@Component({
  selector: 'app-root',
  imports: [RouterLink, RouterLinkActive, RouterOutlet],
  template: `
    <div class="site-frame">
      @if (isAuthRoute()) {
        <header class="auth-header">
          <a routerLink="/" class="brand" aria-label="CropCompass home">
            <span class="brand-mark" aria-hidden="true"><img src="/cropcompass-mark.svg" alt="" width="32" height="32" /></span>
            <span class="brand-copy"><strong>CropCompass</strong><small>Weather insight for better farming</small></span>
          </a>
          <div class="auth-header-actions">
            <button type="button" class="theme-toggle" [attr.aria-label]="theme() === 'dark' ? 'Switch to light mode' : 'Switch to dark mode'" [attr.aria-pressed]="theme() === 'dark'" (click)="toggleTheme()">
              <span aria-hidden="true">{{ theme() === 'dark' ? '☀' : '☾' }}</span>
              <span class="theme-toggle-label">{{ theme() === 'dark' ? 'Light mode' : 'Dark mode' }}</span>
            </button>
            <a routerLink="/" class="auth-back-link">← <span class="auth-back-label">Back to dashboard</span><span class="auth-back-short">Dashboard</span></a>
          </div>
        </header>
        <main class="app-shell auth-shell"><router-outlet /></main>
      } @else if (auth.isAuthenticated()) {
        <div class="workspace-layout">
          <aside class="workspace-sidebar" aria-label="Workspace navigation">
            <a routerLink="/" class="brand workspace-brand" aria-label="CropCompass home">
              <span class="brand-mark" aria-hidden="true"><img src="/cropcompass-mark.svg" alt="" width="32" height="32" /></span>
              <span class="brand-copy"><strong>CropCompass</strong><small>Anuradhapura District</small></span>
            </a>
            <details class="mobile-menu workspace-mobile-menu" #workspaceMenu><summary>Menu</summary><nav aria-label="Mobile workspace navigation"><a routerLink="/" (click)="workspaceMenu.open = false">Overview</a><a routerLink="/recommendations" (click)="workspaceMenu.open = false">Recommendations</a><a routerLink="/seasonal-planning" (click)="workspaceMenu.open = false">Seasonal planning</a>@if (auth.hasRole('Registered User')) { <a routerLink="/history" (click)="workspaceMenu.open = false">Saved history</a> }@if (auth.hasRole('Agricultural Officer')) { <a routerLink="/officer" (click)="workspaceMenu.open = false">Validation queue</a> }@if (auth.hasRole('Administrator')) { <a routerLink="/admin" (click)="workspaceMenu.open = false">Administration</a> }</nav></details>
            <div class="workspace-sidebar-label">WORKSPACE</div>
            <nav class="workspace-nav" aria-label="Primary navigation">
              <a routerLink="/" routerLinkActive="active" [routerLinkActiveOptions]="{ exact: true }"><span aria-hidden="true">◫</span> Overview</a>
              <a routerLink="/recommendations" routerLinkActive="active"><span aria-hidden="true">◇</span> Recommendations</a>
              <a routerLink="/seasonal-planning" routerLinkActive="active"><span aria-hidden="true">▦</span> Seasonal planning</a>
              @if (auth.hasRole('Registered User')) { <a routerLink="/history" routerLinkActive="active"><span aria-hidden="true">◷</span> Saved history</a> }
              @if (auth.hasRole('Agricultural Officer')) { <a routerLink="/officer" routerLinkActive="active"><span aria-hidden="true">✓</span> Validation queue</a> }
              @if (auth.hasRole('Administrator')) { <a routerLink="/admin" routerLinkActive="active"><span aria-hidden="true">▤</span> Administration</a> }
            </nav>
            <div class="workspace-sidebar-bottom">
              <span>DECISION SUPPORT</span>
              <p>Forecast and suitability evidence for Anuradhapura District.</p>
            </div>
          </aside>
          <div class="workspace-main">
            <header class="workspace-topbar">
              <div class="workspace-location"><span>CropCompass / {{ workspaceTitle() }}</span><strong>{{ workspaceTitle() }}</strong></div>
              <div class="workspace-actions">
                <span class="workspace-role">{{ auth.currentUser()?.role }}</span>
                <button type="button" class="theme-toggle" [attr.aria-label]="theme() === 'dark' ? 'Switch to light mode' : 'Switch to dark mode'" [attr.aria-pressed]="theme() === 'dark'" (click)="toggleTheme()"><span aria-hidden="true">{{ theme() === 'dark' ? '☀' : '☾' }}</span><span class="theme-toggle-label">{{ theme() === 'dark' ? 'Light mode' : 'Dark mode' }}</span></button>
                <div class="workspace-user" [title]="auth.currentUser()?.email || ''"><span class="workspace-avatar" aria-hidden="true">{{ auth.currentUser()?.email?.charAt(0)?.toUpperCase() }}</span><span>{{ auth.currentUser()?.email }}</span></div>
                <button type="button" class="workspace-logout" (click)="auth.logout()">Sign out</button>
              </div>
            </header>
            <main class="workspace-content"><router-outlet /></main>
          </div>
        </div>
      } @else {
        <header class="public-header">
          <div class="public-header-inner">
            <a routerLink="/" class="brand" aria-label="CropCompass home"><span class="brand-mark" aria-hidden="true"><img src="/cropcompass-mark.svg" alt="" width="32" height="32" /></span><span class="brand-copy"><strong>CropCompass</strong><small>Anuradhapura District</small></span></a>
            <nav class="public-nav" aria-label="Public navigation"><a routerLink="/" routerLinkActive="active" [routerLinkActiveOptions]="{ exact: true }">Home</a><a routerLink="/recommendations" routerLinkActive="active">Crop recommendations</a><a routerLink="/seasonal-planning" routerLinkActive="active">Seasonal planning</a><a routerLink="/login" class="public-nav-signin">Sign in</a><a routerLink="/staff/login" class="public-nav-staff">Staff access</a></nav>
            <div class="public-actions"><button type="button" class="theme-toggle" [attr.aria-label]="theme() === 'dark' ? 'Switch to light mode' : 'Switch to dark mode'" [attr.aria-pressed]="theme() === 'dark'" (click)="toggleTheme()"><span aria-hidden="true">{{ theme() === 'dark' ? '☀' : '☾' }}</span></button><a routerLink="/staff/login" class="public-staff-link">Staff access</a><a routerLink="/login" class="public-login">Sign in</a><a routerLink="/register" class="public-register">Create account</a></div>
            <details class="mobile-menu public-mobile-menu" #publicMenu><summary>Menu</summary><nav aria-label="Mobile public navigation"><a routerLink="/" (click)="publicMenu.open = false">Home</a><a routerLink="/recommendations" (click)="publicMenu.open = false">Crop recommendations</a><a routerLink="/seasonal-planning" (click)="publicMenu.open = false">Seasonal planning</a><a routerLink="/login" (click)="publicMenu.open = false">Sign in</a><a routerLink="/register" (click)="publicMenu.open = false">Create account</a><a routerLink="/staff/login" (click)="publicMenu.open = false">Staff access</a></nav></details>
          </div>
        </header>
        <main class="public-content"><router-outlet /></main>
        <footer class="public-footer"><div><a routerLink="/" class="brand"><span class="brand-mark" aria-hidden="true"><img src="/cropcompass-mark.svg" alt="" width="32" height="32" /></span><span class="brand-copy"><strong>CropCompass</strong><small>Weather insight for better farming</small></span></a><p>Research prototype for Anuradhapura District, Sri Lanka.</p></div><nav aria-label="Footer navigation"><a routerLink="/recommendations">Recommendations</a><a routerLink="/seasonal-planning">Seasonal planning</a><a routerLink="/staff/login">Staff access</a></nav></footer>
      }
    </div>
  `,
  styles: [],
})
export class App {
  readonly theme = signal<'light' | 'dark'>('light');
  readonly isAuthRoute = signal(false);
  readonly workspaceTitle = signal('Overview');

  constructor(public readonly auth: AuthService, private readonly router: Router) {
    const saved = localStorage.getItem('cropcompass-theme');
    const preferred = saved === 'light' || saved === 'dark'
      ? saved
      : window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
    this.setTheme(preferred);
    this.isAuthRoute.set(isAuthenticationRoute(this.router.url));
    this.workspaceTitle.set(titleForRoute(this.router.url));
    this.router.events
      .pipe(filter((event): event is NavigationEnd => event instanceof NavigationEnd), takeUntilDestroyed())
      .subscribe(event => {
        this.isAuthRoute.set(isAuthenticationRoute(event.urlAfterRedirects));
        this.workspaceTitle.set(titleForRoute(event.urlAfterRedirects));
      });
  }

  toggleTheme(): void {
    this.setTheme(this.theme() === 'dark' ? 'light' : 'dark');
    localStorage.setItem('cropcompass-theme', this.theme());
  }

  private setTheme(theme: 'light' | 'dark'): void {
    this.theme.set(theme);
    document.documentElement.dataset['theme'] = theme;
  }
}

function isAuthenticationRoute(url: string): boolean {
  const path = url.split(/[?#]/, 1)[0];
  return path === '/login' || path === '/staff/login' || path === '/register';
}

function titleForRoute(url: string): string {
  const path = url.split(/[?#]/, 1)[0];
  if (path.startsWith('/admin')) return 'Administration';
  if (path.startsWith('/officer')) return 'Validation queue';
  if (path.startsWith('/history')) return 'Saved history';
  if (path.startsWith('/seasonal-planning')) return 'Seasonal planning';
  if (path.startsWith('/recommendations')) return 'Recommendations';
  return 'Overview';
}
