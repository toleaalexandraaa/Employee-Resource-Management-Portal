import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { SharedModule } from '../../shared/shared.module';
import { AnalyticsOverviewComponent } from './overview/analytics-overview.component';
import { HrAnalyticsComponent } from './hr/hr-analytics.component';
import { WorkforceTabComponent } from './hr/workforce/workforce-tab.component';
import { LeaveTabComponent } from './hr/leave/leave-tab.component';
import { PayrollTabComponent } from './hr/payroll/payroll-tab.component';
import { ProjectsAnalyticsComponent } from './projects/projects-analytics.component';
import { HealthTabComponent } from './projects/health/health-tab.component';
import { TimeTabComponent } from './projects/time/time-tab.component';
import { TasksTabComponent } from './projects/tasks/tasks-tab.component';
import { CrmAnalyticsComponent } from './crm/crm-analytics.component';
import { LeadsTabComponent } from './crm/leads/leads-tab.component';
import { RevenueTabComponent } from './crm/revenue/revenue-tab.component';
import { ClientsTabComponent } from './crm/clients/clients-tab.component';
import { MlAnalyticsComponent } from './ml/ml-analytics.component';
import { EmployeeClustersTabComponent } from './ml/employee-clusters/employee-clusters-tab.component';
import { ClientSegmentsTabComponent } from './ml/client-segments/client-segments-tab.component';
import { ProjectRiskTabComponent } from './ml/project-risk/project-risk-tab.component';

const routes: Routes = [
  { path: '', component: AnalyticsOverviewComponent },
  { path: 'hr', component: HrAnalyticsComponent },
  { path: 'projects', component: ProjectsAnalyticsComponent },
  { path: 'crm', component: CrmAnalyticsComponent },
  { path: 'ml', component: MlAnalyticsComponent },
];

@NgModule({
  declarations: [
    AnalyticsOverviewComponent,
    HrAnalyticsComponent,
    WorkforceTabComponent,
    LeaveTabComponent,
    PayrollTabComponent,
    ProjectsAnalyticsComponent,
    HealthTabComponent,
    TimeTabComponent,
    TasksTabComponent,
    CrmAnalyticsComponent,
    LeadsTabComponent,
    RevenueTabComponent,
    ClientsTabComponent,
    MlAnalyticsComponent,
    EmployeeClustersTabComponent,
    ClientSegmentsTabComponent,
    ProjectRiskTabComponent,
  ],
  imports: [SharedModule, RouterModule.forChild(routes)],
})
export class AnalyticsModule {}
