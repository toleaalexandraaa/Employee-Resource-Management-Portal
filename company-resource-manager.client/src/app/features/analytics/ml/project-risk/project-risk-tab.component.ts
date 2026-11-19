import { ChangeDetectorRef, Component, OnInit } from '@angular/core';
import { AnalyticsService } from '../../../../core/services/analytics.service';
import { ProjectRisksData, ProjectRiskScore } from '../../../../core/models/analytics.models';

@Component({
  selector: 'app-project-risk-tab',
  standalone: false,
  templateUrl: './project-risk-tab.component.html',
})
export class ProjectRiskTabComponent implements OnInit {
  data: ProjectRisksData | null = null;
  loading = true;
  error = false;

  displayedColumns = ['projectName', 'riskScore', 'riskLevel', 'budgetBurnRate', 'timeBurnRate', 'overdueTaskCount'];

  constructor(
    private analytics: AnalyticsService,
    private cdr: ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    this.analytics.getProjectRisks().subscribe({
      next: (d) => {
        this.data = d;
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.loading = false;
        this.error = true;
        this.cdr.detectChanges();
      },
    });
  }

  riskColor(level: string): string {
    const map: Record<string, string> = { OnTrack: 'primary', Watch: 'accent', AtRisk: 'warn' };
    return map[level] ?? 'accent';
  }

  riskLabel(level: string): string {
    const map: Record<string, string> = { OnTrack: 'On Track', Watch: 'Watch', AtRisk: 'At Risk' };
    return map[level] ?? level;
  }

  sortedProjects(projects: ProjectRiskScore[]): ProjectRiskScore[] {
    return [...projects].sort((a, b) => b.riskScore - a.riskScore);
  }
}
