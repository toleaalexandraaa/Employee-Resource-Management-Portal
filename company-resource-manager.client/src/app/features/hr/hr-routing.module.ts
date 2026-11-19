import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { EmployeeListComponent } from './employees/employee-list/employee-list.component';
import { EmployeeFormComponent } from './employees/employee-form/employee-form.component';
import { EmployeeDetailComponent } from './employees/employee-detail/employee-detail.component';
import { LeaveListComponent } from './leave-requests/leave-list/leave-list.component';
import { LeaveFormComponent } from './leave-requests/leave-form/leave-form.component';
import { PayrollListComponent } from './payroll/payroll-list/payroll-list.component';
import { AuthGuard } from '../../core/guards/auth.guard';

const ADMIN_HR = ['Admin', 'HrManager'];

const routes: Routes = [
  {
    path: 'employees',
    component: EmployeeListComponent,
    canActivate: [AuthGuard],
    data: { roles: ADMIN_HR },
  },
  {
    path: 'employees/new',
    component: EmployeeFormComponent,
    canActivate: [AuthGuard],
    data: { roles: ADMIN_HR },
  },
  {
    path: 'employees/:id',
    component: EmployeeDetailComponent,
    canActivate: [AuthGuard],
    data: { roles: ADMIN_HR },
  },
  {
    path: 'employees/:id/edit',
    component: EmployeeFormComponent,
    canActivate: [AuthGuard],
    data: { roles: ADMIN_HR },
  },
  {
    path: 'leave-requests',
    component: LeaveListComponent,
    canActivate: [AuthGuard],
    data: { roles: [...ADMIN_HR, 'Employee'] },
  },
  {
    path: 'leave-requests/new',
    component: LeaveFormComponent,
    canActivate: [AuthGuard],
    data: { roles: [...ADMIN_HR, 'Employee'] },
  },
  {
    path: 'payroll',
    component: PayrollListComponent,
    canActivate: [AuthGuard],
    data: { roles: ADMIN_HR },
  },
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule],
})
export class HrRoutingModule {}
