import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { ApiService, FrontendApiError } from './api.service';
import { AuthService } from './auth.service';

describe('AuthService', () => {
  const router = { navigateByUrl: vi.fn() };
  const api = {
    login: vi.fn(),
    register: vi.fn(),
  };

  beforeEach(() => {
    localStorage.clear();
    vi.clearAllMocks();
    TestBed.configureTestingModule({
      providers: [
        AuthService,
        { provide: ApiService, useValue: api },
        { provide: Router, useValue: router },
      ],
    });
  });

  it('stores backend token and registered-user session on login success', () => {
    api.login.mockReturnValue(of({
      accessToken: 'test-token',
      expiresAt: '2026-10-10T01:00:00Z',
      userId: 12,
      name: 'User',
      email: 'user@example.test',
      role: 'Registered User',
    }));
    const service = TestBed.inject(AuthService);

    service.login({ email: 'user@example.test', password: 'StrongPassword1!' });

    expect(service.isAuthenticated()).toBe(true);
    expect(service.hasRole('Registered User')).toBe(true);
    expect(localStorage.getItem('anuradhapura-ai-access-token')).toBe('test-token');
    expect(router.navigateByUrl).toHaveBeenCalledWith('/recommendations');
  });

  it('reports login failure without storing a token', () => {
    api.login.mockReturnValue(throwError(() => new FrontendApiError('Invalid credentials.', 401)));
    const service = TestBed.inject(AuthService);

    service.login({ email: 'user@example.test', password: 'wrong' });

    expect(service.isAuthenticated()).toBe(false);
    expect(service.authError()).toBe('Invalid credentials.');
    expect(localStorage.getItem('anuradhapura-ai-access-token')).toBeNull();
  });

  it('clears session values on logout', () => {
    localStorage.setItem('anuradhapura-ai-access-token', 'test-token');
    localStorage.setItem('anuradhapura-ai-current-user', JSON.stringify({ id: 2, email: 'officer@example.test', role: 'Agricultural Officer' }));
    const service = TestBed.inject(AuthService);

    service.logout();

    expect(service.currentUser()).toBeNull();
    expect(localStorage.getItem('anuradhapura-ai-access-token')).toBeNull();
    expect(router.navigateByUrl).toHaveBeenCalledWith('/');
  });
});
