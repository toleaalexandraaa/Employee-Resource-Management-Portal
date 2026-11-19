import { ChangeDetectorRef, Component, EventEmitter, Output, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { MatDialog } from '@angular/material/dialog';
import { AuthService } from '../../core/services/auth.service';
import { NotificationHubService } from '../../core/services/notification-hub.service';
import { Notification } from '../../core/models/notification.models';
import { ChangePasswordDialogComponent } from './change-password-dialog.component';
import { TranslateService } from '@ngx-translate/core';

@Component({
  selector: 'app-toolbar',
  standalone: false,
  templateUrl: './toolbar.component.html',
  styleUrls: ['./toolbar.component.css'],
})
export class ToolbarComponent implements OnInit {
  @Output() menuToggle = new EventEmitter<void>();
  notifications: Notification[] = [];
  unreadCount = 0;
  userName = '';

  constructor(
    public authService: AuthService,
    private router: Router,
    private dialog: MatDialog,
    private notificationHub: NotificationHubService,
    private cdr: ChangeDetectorRef,
    public translate: TranslateService
  ) {}

  get currentLang(): string {
    return this.translate.currentLang || 'en';
  }

  toggleLanguage(): void {
    const next = this.currentLang === 'ro' ? 'en' : 'ro';
    this.translate.use(next);
    localStorage.setItem('app_lang', next);
  }

  ngOnInit(): void {
    this.userName = this.authService.currentUser?.fullName ?? '';
    this.notificationHub.notifications$.subscribe((notifs) => {
      this.notifications = notifs.slice(0, 5);
      this.unreadCount = notifs.filter((n) => !n.isRead).length;
      this.cdr.detectChanges();
    });
  }

  markRead(id: string): void {
    this.notificationHub.markRead(id);
  }

  markAllRead(): void {
    this.notificationHub.markAllRead();
  }

  openChangePassword(): void {
    this.dialog.open(ChangePasswordDialogComponent, { width: '380px', disableClose: true });
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/auth/login']);
  }
}
