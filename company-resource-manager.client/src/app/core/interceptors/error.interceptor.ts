import { Injectable } from '@angular/core';
import {
  HttpRequest,
  HttpHandler,
  HttpEvent,
  HttpInterceptor,
  HttpErrorResponse,
} from '@angular/common/http';
import {
  Observable,
  throwError,
  BehaviorSubject,
  filter,
  take,
  switchMap,
  catchError,
} from 'rxjs';
import { Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

@Injectable()
export class ErrorInterceptor implements HttpInterceptor {
  private isRefreshing = false;
  private refreshDone$ = new BehaviorSubject<boolean>(false);

  constructor(
    private router: Router,
    private authService: AuthService
  ) {}

  intercept(request: HttpRequest<unknown>, next: HttpHandler): Observable<HttpEvent<unknown>> {
    return next.handle(request).pipe(
      catchError((error: HttpErrorResponse) => {
        if (error.status === 401 && !request.url.includes('/api/auth/')) {
          return this.handle401(request, next);
        }
        return throwError(() => error);
      })
    );
  }

  private handle401(
    request: HttpRequest<unknown>,
    next: HttpHandler
  ): Observable<HttpEvent<unknown>> {
    const refreshToken = this.authService.getRefreshToken();
    if (!refreshToken) {
      this.doLogout();
      return throwError(() => new Error('No refresh token'));
    }

    if (this.isRefreshing) {
      return this.refreshDone$.pipe(
        filter((done) => done),
        take(1),
        switchMap(() => next.handle(this.cloneWithToken(request)))
      );
    }

    this.isRefreshing = true;
    this.refreshDone$.next(false);

    return this.authService.refreshToken(refreshToken).pipe(
      switchMap(() => {
        this.isRefreshing = false;
        this.refreshDone$.next(true);
        return next.handle(this.cloneWithToken(request));
      }),
      catchError((err) => {
        this.isRefreshing = false;
        this.doLogout();
        return throwError(() => err);
      })
    );
  }

  private cloneWithToken(request: HttpRequest<unknown>): HttpRequest<unknown> {
    const token = this.authService.getToken();
    return token
      ? request.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
      : request;
  }

  private doLogout(): void {
    this.authService.logout();
    this.router.navigate(['/auth/login']);
  }
}
