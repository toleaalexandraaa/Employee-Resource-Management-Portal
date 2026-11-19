import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { AuthService } from './auth.service';
import { LoginResponse } from '../models/auth.models';

const mockUser: LoginResponse = {
  accessToken: 'test-access-token',
  refreshToken: 'test-refresh-token',
  expiresAt: '2099-01-01T00:00:00Z',
  userId: 'user-1',
  fullName: 'Test User',
  role: 'Admin',
};

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [AuthService, provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    localStorage.clear();
  });

  describe('login', () => {
    it('should POST to /api/auth/login and store tokens', () => {
      service.login({ email: 'test@test.com', password: 'pass' }).subscribe((res) => {
        expect(res).toEqual(mockUser);
      });

      const req = httpMock.expectOne('/api/auth/login');
      expect(req.request.method).toBe('POST');
      req.flush(mockUser);

      expect(localStorage.getItem('crm_token')).toBe('test-access-token');
      expect(localStorage.getItem('crm_refresh')).toBe('test-refresh-token');
      expect(JSON.parse(localStorage.getItem('crm_user')!)).toEqual(mockUser);
    });

    it('should update currentUser$ after login', () => {
      return new Promise<void>((resolve) => {
        service.login({ email: 'test@test.com', password: 'pass' }).subscribe(() => {
          service.currentUser$.subscribe((user) => {
            expect(user).toEqual(mockUser);
            resolve();
          });
        });

        httpMock.expectOne('/api/auth/login').flush(mockUser);
      });
    });
  });

  describe('logout', () => {
    it('should clear localStorage and emit null to currentUser$', () => {
      localStorage.setItem('crm_token', 'tok');
      localStorage.setItem('crm_refresh', 'ref');
      localStorage.setItem('crm_user', JSON.stringify(mockUser));

      service.logout();

      expect(localStorage.getItem('crm_token')).toBeNull();
      expect(localStorage.getItem('crm_refresh')).toBeNull();
      expect(localStorage.getItem('crm_user')).toBeNull();
      expect(service.currentUser).toBeNull();
    });
  });

  describe('isLoggedIn', () => {
    it('should return true when token exists', () => {
      localStorage.setItem('crm_token', 'some-token');
      expect(service.isLoggedIn()).toBe(true);
    });

    it('should return false when no token', () => {
      expect(service.isLoggedIn()).toBe(false);
    });
  });

  describe('getToken / getRefreshToken', () => {
    it('should return the stored access token', () => {
      localStorage.setItem('crm_token', 'my-token');
      expect(service.getToken()).toBe('my-token');
    });

    it('should return null when no token stored', () => {
      expect(service.getToken()).toBeNull();
    });

    it('should return the stored refresh token', () => {
      localStorage.setItem('crm_refresh', 'my-refresh');
      expect(service.getRefreshToken()).toBe('my-refresh');
    });
  });

  describe('hasRole', () => {
    it('should return true when current user has the given role', () => {
      localStorage.setItem('crm_user', JSON.stringify(mockUser));
      TestBed.resetTestingModule();
      TestBed.configureTestingModule({ providers: [AuthService, provideHttpClient(), provideHttpClientTesting()] });
      service = TestBed.inject(AuthService);
      httpMock = TestBed.inject(HttpTestingController);

      expect(service.hasRole('Admin')).toBe(true);
    });

    it('should return false for a different role', () => {
      localStorage.setItem('crm_user', JSON.stringify(mockUser));
      TestBed.resetTestingModule();
      TestBed.configureTestingModule({ providers: [AuthService, provideHttpClient(), provideHttpClientTesting()] });
      service = TestBed.inject(AuthService);
      httpMock = TestBed.inject(HttpTestingController);

      expect(service.hasRole('Employee')).toBe(false);
    });

    it('should return false when no user is logged in', () => {
      expect(service.hasRole('Admin')).toBe(false);
    });
  });

  describe('hasAnyRole', () => {
    beforeEach(() => {
      localStorage.setItem('crm_user', JSON.stringify(mockUser));
      TestBed.resetTestingModule();
      TestBed.configureTestingModule({ providers: [AuthService, provideHttpClient(), provideHttpClientTesting()] });
      service = TestBed.inject(AuthService);
      httpMock = TestBed.inject(HttpTestingController);
    });

    it('should return true when user role is in the provided list', () => {
      expect(service.hasAnyRole(['Employee', 'Admin'])).toBe(true);
    });

    it('should return false when user role is not in the provided list', () => {
      expect(service.hasAnyRole(['Employee', 'HrManager'])).toBe(false);
    });
  });

  describe('refreshToken', () => {
    it('should POST to /api/auth/refresh and update stored tokens', () => {
      const refreshed: LoginResponse = { ...mockUser, accessToken: 'new-access', refreshToken: 'new-refresh' };

      service.refreshToken('old-refresh').subscribe((res) => {
        expect(res.accessToken).toBe('new-access');
      });

      const req = httpMock.expectOne('/api/auth/refresh');
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({ refreshToken: 'old-refresh' });
      req.flush(refreshed);

      expect(localStorage.getItem('crm_token')).toBe('new-access');
      expect(localStorage.getItem('crm_refresh')).toBe('new-refresh');
    });
  });

  describe('changePassword', () => {
    it('should POST to /api/auth/change-password', () => {
      service.changePassword('old', 'new').subscribe();

      const req = httpMock.expectOne('/api/auth/change-password');
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({ currentPassword: 'old', newPassword: 'new' });
      req.flush(null);
    });
  });
});
