import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { forkJoin, of } from 'rxjs';
import { AuthService } from '../../core/services/auth.service';
import { EmployeeService } from '../../core/services/employee.service';
import { ProjectService } from '../../core/services/project.service';
import { ClientService } from '../../core/services/client.service';
import { LeaveRequestService } from '../../core/services/leave-request.service';
import { LeadService } from '../../core/services/lead.service';
import { ProjectListItem } from '../../core/models/project.models';
import { ClientListItem, Lead } from '../../core/models/crm.models';
import { EmployeeListItem, LeaveRequest } from '../../core/models/hr.models';
import { PagedResult } from '../../core/models/shared.models';

@Component({
  selector: 'app-dashboard',
  standalone: false,
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.css'],
})
export class DashboardComponent implements OnInit {
  greetingKey = '';
  subtitleKey = '';
  loading = true;

  // Role-based visibility flags (derived from sidenav role rules)
  canSeeEmployees = false;   // Admin, HrManager, ProjectManager
  canSeePendingLeaves = false; // Admin, HrManager
  canSeeClients = false;     // Admin, AccountManager, ProjectManager
  canSeeLeads = false;       // Admin, AccountManager

  totalEmployees = 0;
  activeEmployees = 0;
  activeProjects = 0;
  totalProjects = 0;
  activeClients = 0;
  totalClients = 0;
  pendingLeaves = 0;

  recentProjects: ProjectListItem[] = [];
  recentLeads: Lead[] = [];

  constructor(
    private authService: AuthService,
    private employeeService: EmployeeService,
    private projectService: ProjectService,
    private clientService: ClientService,
    private leaveService: LeaveRequestService,
    private leadService: LeadService,
    private cdr: ChangeDetectorRef,
  ) {
    this.greetingKey = this.getGreetingKey();
    this.subtitleKey = this.getSubtitleKey();
    this.canSeeEmployees = this.authService.hasAnyRole(['Admin', 'HrManager', 'ProjectManager']);
    this.canSeePendingLeaves = this.authService.hasAnyRole(['Admin', 'HrManager']);
    this.canSeeClients = this.authService.hasAnyRole(['Admin', 'AccountManager', 'ProjectManager']);
    this.canSeeLeads = this.authService.hasAnyRole(['Admin', 'AccountManager']);
  }

  ngOnInit(): void {
    const empty = <T>(items: T[] = []): PagedResult<T> => ({ items, totalCount: 0, page: 1, pageSize: 0 });

    forkJoin({
      employees: this.canSeeEmployees
        ? this.employeeService.getAll(1, 100)
        : of(empty<EmployeeListItem>()),
      projects: this.projectService.getAll(1, 100),
      clients: this.canSeeClients
        ? this.clientService.getAll(1, 100)
        : of(empty<ClientListItem>()),
      leaves: this.canSeePendingLeaves
        ? this.leaveService.getAll(1, 100)
        : of(empty<LeaveRequest>()),
      leads: this.canSeeLeads
        ? this.leadService.getAll(1, 100)
        : of(empty<Lead>()),
    }).subscribe({
      next: ({ employees, projects, clients, leaves, leads }) => {
        this.totalEmployees = employees.totalCount;
        this.activeEmployees = employees.items.filter((e) => e.isActive).length;

        this.totalProjects = projects.totalCount;
        this.activeProjects = projects.items.filter((p) => p.status === 'Active').length;
        this.recentProjects = projects.items.slice(0, 5);

        this.totalClients = clients.totalCount;
        this.activeClients = clients.items.filter((c) => c.status === 'Active').length;

        this.pendingLeaves = leaves.items.filter((l) => l.status === 'Pending').length;

        this.recentLeads = leads.items
          .filter((l) => l.convertedToClientId === null && l.stage !== 'Lost')
          .slice(0, 5);

        this.loading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.loading = false;
        this.cdr.detectChanges();
      },
    });
  }

  private getGreetingKey(): string {
    const hour = new Date().getHours();
    if (hour >= 5 && hour < 12) return 'DASHBOARD.WELCOME_MORNING';
    if (hour >= 12 && hour < 17) return 'DASHBOARD.WELCOME_AFTERNOON';
    if (hour >= 17 && hour < 22) return 'DASHBOARD.WELCOME_EVENING';
    return 'DASHBOARD.WELCOME_NIGHT';
  }

  private getSubtitleKey(): string {
    const role = this.authService.currentUser?.role ?? '';
    const map: Record<string, string> = {
      Admin: 'DASHBOARD.SUBTITLE_ADMIN',
      HrManager: 'DASHBOARD.SUBTITLE_HR_MANAGER',
      ProjectManager: 'DASHBOARD.SUBTITLE_PROJECT_MANAGER',
      AccountManager: 'DASHBOARD.SUBTITLE_ACCOUNT_MANAGER',
      Employee: 'DASHBOARD.SUBTITLE_EMPLOYEE',
    };
    return map[role] ?? 'DASHBOARD.SUBTITLE_ADMIN';
  }

  formatCurrency(value: number): string {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD',
      maximumFractionDigits: 0,
    }).format(value);
  }
}
