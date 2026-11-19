import { vi } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { Router, ActivatedRouteSnapshot } from '@angular/router';
import { AuthGuard } from './auth.guard';
import { AuthService } from '../services/auth.service';

describe('AuthGuard', () => {
  let guard: AuthGuard;
  let authService: { isLoggedIn: ReturnType<typeof vi.fn>; hasAnyRole: ReturnType<typeof vi.fn> };
  let router: { navigate: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    authService = { isLoggedIn: vi.fn(), hasAnyRole: vi.fn() };
    router = { navigate: vi.fn() };

    TestBed.configureTestingModule({
      providers: [
        AuthGuard,
        { provide: AuthService, useValue: authService },
        { provide: Router, useValue: router },
      ],
    });

    guard = TestBed.inject(AuthGuard);
  });

  function makeRoute(roles?: string[]): ActivatedRouteSnapshot {
    const route = new ActivatedRouteSnapshot();
    (route as any).data = roles ? { roles } : {};
    return route;
  }

  describe('when not logged in', () => {
    beforeEach(() => authService.isLoggedIn.mockReturnValue(false));

    it('should return false and navigate to /auth/login', () => {
      const result = guard.canActivate(makeRoute());
      expect(result).toBe(false);
      expect(router.navigate).toHaveBeenCalledWith(['/auth/login']);
    });
  });

  describe('when logged in', () => {
    beforeEach(() => authService.isLoggedIn.mockReturnValue(true));

    it('should return true when no roles are required', () => {
      const result = guard.canActivate(makeRoute());
      expect(result).toBe(true);
      expect(router.navigate).not.toHaveBeenCalled();
    });

    it('should return true when user has a required role', () => {
      authService.hasAnyRole.mockReturnValue(true);
      const result = guard.canActivate(makeRoute(['Admin', 'HrManager']));
      expect(result).toBe(true);
      expect(authService.hasAnyRole).toHaveBeenCalledWith(['Admin', 'HrManager']);
    });

    it('should return false and navigate to / when user lacks required role', () => {
      authService.hasAnyRole.mockReturnValue(false);
      const result = guard.canActivate(makeRoute(['Admin']));
      expect(result).toBe(false);
      expect(router.navigate).toHaveBeenCalledWith(['/']);
    });
  });
});
