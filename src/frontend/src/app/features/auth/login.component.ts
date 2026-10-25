import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { AuthService, LoginArea } from '../../core/auth.service';

@Component({
  selector: 'app-login',
  imports: [FormsModule, RouterLink],
  template: `
    <section class="auth-page auth-login-page">
      <aside class="auth-story" aria-label="About CropCompass">
        <div class="auth-story-content">
          <span class="auth-story-kicker">ANURADHAPURA DISTRICT</span>
          <h2>{{ isStaff ? 'Your staff workspace starts here.' : 'Clarity for every farming decision.' }}</h2>
          <p>{{ isStaff ? 'Sign in to review recommendations or manage CropCompass according to your assigned role.' : 'Bring the seven-day weather outlook, crop suitability evidence, and your account workspace together in one place.' }}</p>
          <div class="auth-feature-list">
            <div><span aria-hidden="true">01</span><strong>Weather outlook</strong><small>Review the latest seven-day AI forecast.</small></div>
            <div><span aria-hidden="true">02</span><strong>Crop context</strong><small>Explore explainable suitability results.</small></div>
            <div><span aria-hidden="true">03</span><strong>Your workspace</strong><small>Return to the features available to your role.</small></div>
          </div>
        </div>
        <p class="auth-story-footer">CropCompass · Research prototype for Anuradhapura, Sri Lanka</p>
      </aside>

      <div class="auth-form-side">
        <div class="auth-card">
          <p class="auth-kicker">{{ isStaff ? 'STAFF ACCESS' : 'REGISTERED USER ACCESS' }}</p>
          <h1>{{ isStaff ? 'Staff sign in' : 'User sign in' }}</h1>
          <p class="auth-intro">{{ isStaff ? 'For Agricultural Officers and Administrators. Use your assigned account details.' : 'Sign in to save and revisit your crop recommendations.' }}</p>
          <form class="auth-form" (ngSubmit)="auth.login({ email, password }, loginArea)" [attr.aria-busy]="auth.isLoading()">
            <div class="auth-field">
              <label for="login-email">Email address</label>
              <input id="login-email" name="email" type="email" [(ngModel)]="email" required autocomplete="email" inputmode="email" spellcheck="false" placeholder="you@example.com" />
            </div>
            <div class="auth-field">
              <label for="login-password">Password</label>
              <div class="auth-password-field">
                <input id="login-password" name="password" [type]="showPassword ? 'text' : 'password'" [(ngModel)]="password" required autocomplete="current-password" placeholder="Enter your password" />
                <button type="button" class="auth-password-toggle" [attr.aria-label]="showPassword ? 'Hide password' : 'Show password'" [attr.aria-pressed]="showPassword" (click)="showPassword = !showPassword">{{ showPassword ? 'Hide' : 'Show' }}</button>
              </div>
            </div>
            @if (auth.authError(); as message) {
              <div class="notice error auth-error" role="alert">{{ message }}</div>
            }
            <button type="submit" class="button primary auth-submit" [disabled]="auth.isLoading()">{{ auth.isLoading() ? 'Signing in…' : 'Sign in' }}</button>
          </form>
          @if (isStaff) {
            <p class="auth-switch">Have a Registered User account? <a routerLink="/login">User sign in</a></p>
            <p class="auth-form-note">Staff access is assigned by an Administrator. Registration does not create a staff account.</p>
          } @else {
            <p class="auth-switch">New to CropCompass? <a routerLink="/register">Create an account</a></p>
            <p class="auth-form-note">The public forecast and crop recommendations are available without signing in. <a routerLink="/staff/login">Staff sign in</a></p>
          }
        </div>
      </div>
    </section>
  `,
})
export class LoginComponent {
  email = '';
  password = '';
  showPassword = false;
  readonly loginArea: LoginArea;
  readonly isStaff: boolean;

  constructor(public readonly auth: AuthService, route: ActivatedRoute) {
    this.loginArea = route.snapshot.data['loginArea'] === 'staff' ? 'staff' : 'user';
    this.isStaff = this.loginArea === 'staff';
  }
}
