import { Injectable, signal } from '@angular/core';
import { Router } from '@angular/router';
import { finalize } from 'rxjs/operators';
import { ApiService } from './api.service';
import { CurrentUserResponse, LoginRequest, LoginResponse, RegisterRequest, UserRole } from './api.models';

const tokenStorageKey = 'anuradhapura-ai-access-token';
const userStorageKey = 'anuradhapura-ai-current-user';

@Injectable({ providedIn: 'root' })
export class AuthService {
  readonly currentUser = signal<CurrentUserResponse | null>(readStoredUser());
  readonly isLoading = signal(false);
  readonly authError = signal<string | null>(null);

  constructor(
    private readonly api: ApiService,
    private readonly router: Router,
  ) {}

  get token(): string | null {
    return localStorage.getItem(tokenStorageKey);
  }

  login(request: LoginRequest): void {
    this.isLoading.set(true);
    this.authError.set(null);
    this.api
      .login(request)
      .pipe(finalize(() => this.isLoading.set(false)))
      .subscribe({
        next: response => this.establishSession(response),
        error: error => this.authError.set(error.message),
      });
  }

  register(request: RegisterRequest): void {
    this.isLoading.set(true);
    this.authError.set(null);
    this.api
      .register(request)
      .pipe(finalize(() => this.isLoading.set(false)))
      .subscribe({
        next: () => this.router.navigateByUrl('/login'),
        error: error => this.authError.set(error.message),
      });
  }

  logout(): void {
    localStorage.removeItem(tokenStorageKey);
    localStorage.removeItem(userStorageKey);
    this.currentUser.set(null);
    this.router.navigateByUrl('/');
  }

  hasRole(role: UserRole): boolean {
    return this.isAuthenticated() && this.currentUser()?.role === role;
  }

  isAuthenticated(): boolean {
    return this.currentUser() !== null && this.token !== null;
  }

  private establishSession(response: LoginResponse): void {
    localStorage.setItem(tokenStorageKey, response.accessToken);
    const user: CurrentUserResponse = {
      id: response.userId,
      email: response.email,
      role: response.role,
    };
    localStorage.setItem(userStorageKey, JSON.stringify(user));
    this.currentUser.set(user);
    const destination = response.role === 'Agricultural Officer'
      ? '/officer'
      : response.role === 'Administrator'
        ? '/admin'
        : '/recommendations';
    this.router.navigateByUrl(destination);
  }
}

function readStoredUser(): CurrentUserResponse | null {
  const json = localStorage.getItem(userStorageKey);
  if (!json) {
    return null;
  }

  try {
    return JSON.parse(json) as CurrentUserResponse;
  } catch {
    localStorage.removeItem(userStorageKey);
    return null;
  }
}
