import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { beforeEach, describe, expect, it } from 'vitest';
import { AuthService } from './auth.service';
import { roleGuard } from './role.guard';

describe('roleGuard', () => {
  const router = {
    createUrlTree: (commands: string[]) => ({ redirectTo: commands.join('/') }),
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [{ provide: Router, useValue: router }],
    });
  });

  it('rejects stored users when the access token is missing', () => {
    const auth = {
      currentUser: () => ({ id: 7, email: 'officer@example.test', role: 'Agricultural Officer' }),
      isAuthenticated: () => false,
    };
    TestBed.overrideProvider(AuthService, { useValue: auth });

    const result = TestBed.runInInjectionContext(() => roleGuard(['Agricultural Officer'])({} as never, {} as never));

    expect(result).toEqual({ redirectTo: '/login' });
  });

  it('allows an authenticated officer to access officer routes', () => {
    const auth = {
      currentUser: () => ({ id: 7, email: 'officer@example.test', role: 'Agricultural Officer' }),
      isAuthenticated: () => true,
    };
    TestBed.overrideProvider(AuthService, { useValue: auth });

    const result = TestBed.runInInjectionContext(() => roleGuard(['Agricultural Officer'])({} as never, {} as never));

    expect(result).toBe(true);
  });

  it('rejects registered users from officer routes', () => {
    const auth = {
      currentUser: () => ({ id: 9, email: 'user@example.test', role: 'Registered User' }),
      isAuthenticated: () => true,
    };
    TestBed.overrideProvider(AuthService, { useValue: auth });

    const result = TestBed.runInInjectionContext(() => roleGuard(['Agricultural Officer'])({} as never, {} as never));

    expect(result).toEqual({ redirectTo: '/' });
  });
});
