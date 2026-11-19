import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { ShellComponent } from './layout/shell/shell.component';
import { AuthGuard } from './core/guards/auth.guard';

const routes: Routes = [
  {
    path: 'auth',
    loadChildren: () => import('./features/auth/auth.module').then((m) => m.AuthModule),
  },
  {
    path: '',
    component: ShellComponent,
    canActivate: [AuthGuard],
    children: [
      {
        path: '',
        loadChildren: () =>
          import('./features/dashboard/dashboard.module').then((m) => m.DashboardModule),
      },
      {
        path: 'hr',
        loadChildren: () => import('./features/hr/hr.module').then((m) => m.HrModule),
      },
      {
        path: 'projects',
        loadChildren: () =>
          import('./features/projects/projects.module').then((m) => m.ProjectsModule),
      },
      {
        path: 'crm',
        canActivate: [AuthGuard],
        data: { roles: ['Admin', 'AccountManager'] },
        loadChildren: () => import('./features/crm/crm.module').then((m) => m.CrmModule),
      },
      {
        path: 'analytics',
        canActivate: [AuthGuard],
        data: { roles: ['Admin', 'HrManager', 'ProjectManager', 'AccountManager'] },
        loadChildren: () =>
          import('./features/analytics/analytics.module').then((m) => m.AnalyticsModule),
      },
    ],
  },
  { path: '**', redirectTo: '' },
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule],
})
export class AppRoutingModule {}
