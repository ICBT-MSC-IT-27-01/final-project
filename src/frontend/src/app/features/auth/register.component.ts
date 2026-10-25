import { Component } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth.service';

@Component({
  selector: 'app-register',
  imports: [FormsModule, RouterLink],
  template: `
    <section class="auth-page auth-register-page">
      <aside class="auth-story" aria-label="About CropCompass">
        <div class="auth-story-content">
          <span class="auth-story-kicker">ANURADHAPURA DISTRICT</span>
          <h2>Make room for better insight.</h2>
          <p>Create an account to keep your crop recommendations close and return to them when you need them.</p>
          <div class="auth-feature-list">
            <div><span aria-hidden="true">01</span><strong>Explore</strong><small>See the current seven-day weather outlook.</small></div>
            <div><span aria-hidden="true">02</span><strong>Compare</strong><small>Review six approved crops with clear evidence.</small></div>
            <div><span aria-hidden="true">03</span><strong>Save</strong><small>Keep recommendation results in your history.</small></div>
          </div>
        </div>
        <p class="auth-story-footer">CropCompass · Research prototype for Anuradhapura, Sri Lanka</p>
      </aside>

      <div class="auth-form-side">
        <div class="auth-card">
          <p class="auth-kicker">CREATE YOUR ACCOUNT</p>
          <h1>Get started with CropCompass</h1>
          <p class="auth-intro">Set up a Registered User account to save and revisit your recommendations.</p>
          <form class="auth-form" (ngSubmit)="auth.register({ name, email, password })" [attr.aria-busy]="auth.isLoading()">
            <div class="auth-field">
              <label for="register-name">Full name</label>
              <input id="register-name" name="name" [(ngModel)]="name" required maxlength="100" autocomplete="name" placeholder="Your full name" />
            </div>
            <div class="auth-field">
              <label for="register-email">Email address</label>
              <input id="register-email" name="email" type="email" [(ngModel)]="email" required autocomplete="email" inputmode="email" spellcheck="false" placeholder="you@example.com" />
            </div>
            <div class="auth-field">
              <label for="register-password">Password</label>
              <div class="auth-password-field">
                <input id="register-password" name="password" [type]="showPassword ? 'text' : 'password'" [(ngModel)]="password" required minlength="8" autocomplete="new-password" placeholder="Create a password" />
                <button type="button" class="auth-password-toggle" [attr.aria-label]="showPassword ? 'Hide password' : 'Show password'" [attr.aria-pressed]="showPassword" (click)="showPassword = !showPassword">{{ showPassword ? 'Hide' : 'Show' }}</button>
              </div>
              <small class="auth-field-hint">Use at least 8 characters.</small>
            </div>
            @if (auth.authError(); as message) {
              <div class="notice error auth-error" role="alert">{{ message }}</div>
            }
            <button type="submit" class="button primary auth-submit" [disabled]="auth.isLoading()">{{ auth.isLoading() ? 'Creating account…' : 'Create account' }}</button>
          </form>
          <p class="auth-switch">Already have an account? <a routerLink="/login">Sign in</a></p>
          <p class="auth-form-note">Registration creates a Registered User account. Officer and Administrator access is assigned separately.</p>
        </div>
      </div>
    </section>
  `,
})
export class RegisterComponent {
  name = '';
  email = '';
  password = '';
  showPassword = false;

  constructor(public readonly auth: AuthService) {}
}
