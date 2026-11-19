import { Component } from '@angular/core';
import { AuthService } from '../../core/services/auth.service';

interface NavItem {
  label: string;
  icon: string;
  route: string;
  roles?: string[];
}

@Component({
  selector: 'app-sidenav',
  standalone: false,
  templateUrl: './sidenav.component.html',
  styleUrls: ['./sidenav.component.css'],
})
export class SidenavComponent {
  navItems: NavItem[] = [
    { label: 'NAV.DASHBOARD', icon: 'dashboard', route: '/' },
    {
      label: 'NAV.ANALYTICS',
      icon: 'bar_chart',
      route: '/analytics',
      roles: ['Admin', 'HrManager', 'ProjectManager', 'AccountManager'],
    },
    {
      label: 'NAV.HR_ANALYTICS',
      icon: 'people_outline',
      route: '/analytics/hr',
      roles: ['Admin', 'HrManager'],
    },
    {
      label: 'NAV.PROJECT_ANALYTICS',
      icon: 'insert_chart_outlined',
      route: '/analytics/projects',
      roles: ['Admin', 'ProjectManager'],
    },
    {
      label: 'NAV.CRM_ANALYTICS',
      icon: 'leaderboard',
      route: '/analytics/crm',
      roles: ['Admin', 'AccountManager'],
    },
    {
      label: 'NAV.ML_INTELLIGENCE',
      icon: 'model_training',
      route: '/analytics/ml',
      roles: ['Admin', 'HrManager', 'ProjectManager', 'AccountManager'],
    },
    {
      label: 'NAV.EMPLOYEES',
      icon: 'people',
      route: '/hr/employees',
      roles: ['Admin', 'HrManager', 'ProjectManager'],
    },
    {
      label: 'NAV.LEAVE_REQUESTS',
      icon: 'event_busy',
      route: '/hr/leave-requests',
      roles: ['Admin', 'HrManager', 'Employee'],
    },
    { label: 'NAV.PAYROLL', icon: 'payments', route: '/hr/payroll', roles: ['Admin', 'HrManager'] },
    { label: 'NAV.PROJECTS', icon: 'work', route: '/projects' },
    {
      label: 'NAV.CLIENTS',
      icon: 'business',
      route: '/crm/clients',
      roles: ['Admin', 'AccountManager', 'ProjectManager'],
    },
    {
      label: 'NAV.CONTRACTS',
      icon: 'description',
      route: '/crm/contracts',
      roles: ['Admin', 'AccountManager'],
    },
    {
      label: 'NAV.INVOICES',
      icon: 'receipt',
      route: '/crm/invoices',
      roles: ['Admin', 'AccountManager'],
    },
    {
      label: 'NAV.LEADS',
      icon: 'trending_up',
      route: '/crm/leads',
      roles: ['Admin', 'AccountManager'],
    },
  ];

  constructor(public authService: AuthService) {}

  isVisible(item: NavItem): boolean {
    if (!item.roles) return true;
    return this.authService.hasAnyRole(item.roles);
  }
}
