import { Injectable, NgZone, OnDestroy } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject } from 'rxjs';
import { Notification } from '../models/notification.models';
import { AuthService } from './auth.service';

@Injectable({ providedIn: 'root' })
export class NotificationHubService implements OnDestroy {
  private hubConnection: signalR.HubConnection | null = null;
  private notificationsSubject = new BehaviorSubject<Notification[]>([]);
  notifications$ = this.notificationsSubject.asObservable();

  constructor(private authService: AuthService, private http: HttpClient, private ngZone: NgZone) {}

  startConnection(): void {
    const token = this.authService.getToken();
    if (!token) return;

    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/notifications', { accessTokenFactory: () => token })
      .withAutomaticReconnect()
      .build();

    this.hubConnection.on('ReceiveNotification', (notification: Notification) => {
      this.ngZone.run(() => {
        const current = this.notificationsSubject.value;
        this.notificationsSubject.next([notification, ...current]);
      });
    });

    this.hubConnection
      .start()
      .then(() => this.loadUnread())
      .catch((err) => console.error('SignalR connection error:', err));
  }

  private loadUnread(): void {
    this.http.get<Notification[]>('/api/notifications').subscribe({
      next: (notifications) => this.notificationsSubject.next(notifications),
      error: (err) => console.error('Failed to load notifications:', err),
    });
  }

  markRead(id: string): void {
    this.http.post(`/api/notifications/${id}/read`, {}).subscribe(() => {
      const updated = this.notificationsSubject.value.map((n) =>
        n.id === id ? { ...n, isRead: true } : n
      );
      this.notificationsSubject.next(updated);
    });
  }

  markAllRead(): void {
    this.http.post('/api/notifications/read-all', {}).subscribe(() => {
      const updated = this.notificationsSubject.value.map((n) => ({ ...n, isRead: true }));
      this.notificationsSubject.next(updated);
    });
  }

  stopConnection(): void {
    this.hubConnection?.stop();
  }

  ngOnDestroy(): void {
    this.stopConnection();
  }
}
