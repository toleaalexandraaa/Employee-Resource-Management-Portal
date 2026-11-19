import { NgModule } from '@angular/core';
import { SharedModule } from '../../shared/shared.module';
import { HrRoutingModule } from './hr-routing.module';
import { EmployeeListComponent } from './employees/employee-list/employee-list.component';
import { EmployeeFormComponent } from './employees/employee-form/employee-form.component';
import { EmployeeDetailComponent } from './employees/employee-detail/employee-detail.component';
import { LeaveListComponent } from './leave-requests/leave-list/leave-list.component';
import { LeaveFormComponent } from './leave-requests/leave-form/leave-form.component';
import { PayrollListComponent } from './payroll/payroll-list/payroll-list.component';
import { TempPasswordDialogComponent } from './employees/temp-password-dialog/temp-password-dialog.component';

@NgModule({
  declarations: [
    EmployeeListComponent,
    EmployeeFormComponent,
    EmployeeDetailComponent,
    LeaveListComponent,
    LeaveFormComponent,
    PayrollListComponent,
    TempPasswordDialogComponent,
  ],
  imports: [SharedModule, HrRoutingModule],
})
export class HrModule {}
