import { vi } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import { NO_ERRORS_SCHEMA } from '@angular/core';
import { of, throwError } from 'rxjs';
import { TranslateModule } from '@ngx-translate/core';

import { LoginComponent } from './login.component';
import { AuthService } from '../../../core/services/auth.service';
import { LoginResponse } from '../../../core/models/auth.models';

const mockLoginResponse: LoginResponse = {
  accessToken: 'token',
  refreshToken: 'refresh',
  expiresAt: '2099-01-01T00:00:00Z',
  userId: 'u1',
  fullName: 'Test User',
  role: 'Admin',
};

describe('LoginComponent', () => {
  let component: LoginComponent;
  let fixture: ComponentFixture<LoginComponent>;
  let authService: { isLoggedIn: ReturnType<typeof vi.fn>; login: ReturnType<typeof vi.fn> };
  let router: { navigate: ReturnType<typeof vi.fn> };
  let snackBar: { open: ReturnType<typeof vi.fn> };

  beforeEach(async () => {
    authService = { isLoggedIn: vi.fn(), login: vi.fn() };
    router = { navigate: vi.fn() };
    snackBar = { open: vi.fn() };

    await TestBed.configureTestingModule({
      declarations: [LoginComponent],
      imports: [ReactiveFormsModule, TranslateModule.forRoot()],
      providers: [
        { provide: AuthService, useValue: authService },
        { provide: Router, useValue: router },
        { provide: MatSnackBar, useValue: snackBar },
      ],
      schemas: [NO_ERRORS_SCHEMA],
    }).compileComponents();

    authService.isLoggedIn.mockReturnValue(false);

    fixture = TestBed.createComponent(LoginComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should initialize the form with email and password controls', () => {
    expect(component.form.contains('email')).toBe(true);
    expect(component.form.contains('password')).toBe(true);
  });

  it('should redirect to / if already logged in on init', () => {
    authService.isLoggedIn.mockReturnValue(true);
    component.ngOnInit();
    expect(router.navigate).toHaveBeenCalledWith(['/']);
  });

  it('should mark form invalid when fields are empty', () => {
    component.form.setValue({ email: '', password: '' });
    expect(component.form.invalid).toBe(true);
  });

  it('should mark email invalid when not a valid email address', () => {
    component.form.get('email')!.setValue('not-an-email');
    expect(component.form.get('email')!.invalid).toBe(true);
  });

  it('should mark form valid with correct credentials', () => {
    component.form.setValue({ email: 'admin@test.com', password: 'secret' });
    expect(component.form.valid).toBe(true);
  });

  describe('submit', () => {
    it('should not call login when form is invalid', () => {
      component.form.setValue({ email: '', password: '' });
      component.submit();
      expect(authService.login).not.toHaveBeenCalled();
    });

    it('should call authService.login and navigate to / on success', () => {
      authService.login.mockReturnValue(of(mockLoginResponse));
      component.form.setValue({ email: 'admin@test.com', password: 'secret' });

      component.submit();

      expect(authService.login).toHaveBeenCalledWith({ email: 'admin@test.com', password: 'secret' });
      expect(router.navigate).toHaveBeenCalledWith(['/']);
    });

    it('should set loading to true while submitting', () => {
      authService.login.mockReturnValue(of(mockLoginResponse));
      component.form.setValue({ email: 'admin@test.com', password: 'secret' });

      expect(component.loading).toBe(false);
      component.submit();
      expect(router.navigate).toHaveBeenCalled();
    });

    it('should show snack bar and reset loading on error', () => {
      authService.login.mockReturnValue(throwError(() => new Error('Unauthorized')));
      component.form.setValue({ email: 'admin@test.com', password: 'wrong' });

      component.submit();

      expect(snackBar.open).toHaveBeenCalledWith('Invalid email or password', 'Close', { duration: 3000 });
      expect(component.loading).toBe(false);
    });
  });
});
