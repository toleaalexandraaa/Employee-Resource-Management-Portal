import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, tap } from 'rxjs';
import { LoginRequest, LoginResponse, RefreshTokenRequest } from '../models/auth.models';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly TOKEN_KEY = 'crm_token';
  private readonly REFRESH_KEY = 'crm_refresh';
  private readonly USER_KEY = 'crm_user';

  private currentUserSubject = new BehaviorSubject<LoginResponse | null>(this.loadUser());
  currentUser$ = this.currentUserSubject.asObservable();

  constructor(private http: HttpClient) {}

  login(request: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>('/api/auth/login', request).pipe(
      tap((response) => {
        localStorage.setItem(this.TOKEN_KEY, response.accessToken);
        localStorage.setItem(this.REFRESH_KEY, response.refreshToken);
        localStorage.setItem(this.USER_KEY, JSON.stringify(response));
        this.currentUserSubject.next(response);
      })
    );
  }

  logout(): void {
    localStorage.removeItem(this.TOKEN_KEY);
    localStorage.removeItem(this.REFRESH_KEY);
    localStorage.removeItem(this.USER_KEY);
    this.currentUserSubject.next(null);
  }

  getToken(): string | null {
    return localStorage.getItem(this.TOKEN_KEY);
  }

  getRefreshToken(): string | null {
    return localStorage.getItem(this.REFRESH_KEY);
  }

  refreshToken(refreshToken: string): Observable<LoginResponse> {
    const body: RefreshTokenRequest = { refreshToken };
    return this.http.post<LoginResponse>('/api/auth/refresh', body).pipe(
      tap((response) => {
        localStorage.setItem(this.TOKEN_KEY, response.accessToken);
        localStorage.setItem(this.REFRESH_KEY, response.refreshToken);
        localStorage.setItem(this.USER_KEY, JSON.stringify(response));
        this.currentUserSubject.next(response);
      })
    );
  }

  get currentUser(): LoginResponse | null {
    return this.currentUserSubject.value;
  }

  changePassword(currentPassword: string, newPassword: string): Observable<void> {
    return this.http.post<void>('/api/auth/change-password', { currentPassword, newPassword });
  }

  isLoggedIn(): boolean {
    return !!this.getToken();
  }

  hasRole(role: string): boolean {
    return this.currentUser?.role === role;
  }

  hasAnyRole(roles: string[]): boolean {
    return roles.includes(this.currentUser?.role ?? '');
  }

  private loadUser(): LoginResponse | null {
    const raw = localStorage.getItem(this.USER_KEY);
    return raw ? (JSON.parse(raw) as LoginResponse) : null;
  }
}
