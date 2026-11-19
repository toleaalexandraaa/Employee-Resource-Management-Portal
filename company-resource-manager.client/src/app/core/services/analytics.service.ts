import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  ClientSegmentsData,
  CrmClientsData,
  CrmLeadsData,
  CrmRevenueData,
  EmployeeClustersData,
  HrLeaveData,
  HrPayrollData,
  OverviewKpi,
  ProjectHealthData,
  ProjectRiskScore,
  ProjectRisksData,
  ProjectTasksData,
  ProjectTimeData,
  WorkforceData,
} from '../models/analytics.models';

@Injectable({ providedIn: 'root' })
export class AnalyticsService {
  private readonly base = '/api/analytics';

  constructor(private http: HttpClient) {}

  getOverview(): Observable<OverviewKpi> {
    return this.http.get<OverviewKpi>(`${this.base}/overview`);
  }

  getHrWorkforce(): Observable<WorkforceData> {
    return this.http.get<WorkforceData>(`${this.base}/hr/workforce`);
  }

  getHrLeave(): Observable<HrLeaveData> {
    return this.http.get<HrLeaveData>(`${this.base}/hr/leave`);
  }

  getHrPayroll(): Observable<HrPayrollData> {
    return this.http.get<HrPayrollData>(`${this.base}/hr/payroll`);
  }

  getProjectHealth(): Observable<ProjectHealthData> {
    return this.http.get<ProjectHealthData>(`${this.base}/projects/health`);
  }

  getProjectTime(): Observable<ProjectTimeData> {
    return this.http.get<ProjectTimeData>(`${this.base}/projects/time`);
  }

  getProjectTasks(): Observable<ProjectTasksData> {
    return this.http.get<ProjectTasksData>(`${this.base}/projects/tasks`);
  }

  getCrmLeads(): Observable<CrmLeadsData> {
    return this.http.get<CrmLeadsData>(`${this.base}/crm/leads`);
  }

  getCrmRevenue(): Observable<CrmRevenueData> {
    return this.http.get<CrmRevenueData>(`${this.base}/crm/revenue`);
  }

  getCrmClients(): Observable<CrmClientsData> {
    return this.http.get<CrmClientsData>(`${this.base}/crm/clients`);
  }

  // ── ML Clustering ──────────────────────────────────────────────────────────

  getEmployeeClusters(): Observable<EmployeeClustersData> {
    return this.http.get<EmployeeClustersData>(`${this.base}/ml/employee-clusters`);
  }

  getClientSegments(): Observable<ClientSegmentsData> {
    return this.http.get<ClientSegmentsData>(`${this.base}/ml/client-segments`);
  }

  getProjectRisks(): Observable<ProjectRisksData> {
    return this.http.get<ProjectRisksData>(`${this.base}/ml/project-risks`);
  }

  getProjectRisk(projectId: string): Observable<ProjectRiskScore> {
    return this.http.get<ProjectRiskScore>(`${this.base}/projects/${projectId}/risk`);
  }
}
