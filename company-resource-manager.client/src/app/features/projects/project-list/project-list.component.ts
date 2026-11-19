import { Component, OnInit, ViewChild, ChangeDetectorRef } from '@angular/core';
import { MatTableDataSource } from '@angular/material/table';
import { MatPaginator } from '@angular/material/paginator';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ProjectService } from '../../../core/services/project.service';
import { AnalyticsService } from '../../../core/services/analytics.service';
import { AuthService } from '../../../core/services/auth.service';
import { ProjectListItem } from '../../../core/models/project.models';
import { ProjectRiskScore } from '../../../core/models/analytics.models';
import {
  ConfirmDialogComponent,
  ConfirmDialogData,
} from '../../../shared/components/confirm-dialog/confirm-dialog.component';

@Component({
  selector: 'app-project-list',
  standalone: false,
  templateUrl: './project-list.component.html',
})
export class ProjectListComponent implements OnInit {
  @ViewChild(MatPaginator) paginator!: MatPaginator;
  displayedColumns: string[] = [];
  dataSource = new MatTableDataSource<ProjectListItem>();
  totalCount = 0;
  pageSize = 10;
  loading = false;

  riskMap: Record<string, ProjectRiskScore> = {};

  statusColors: Record<string, string> = {
    Planning: 'accent',
    Active: 'primary',
    OnHold: 'warn',
    Completed: 'primary',
    Cancelled: '',
  };

  riskColors: Record<string, string> = { OnTrack: 'primary', Watch: 'accent', AtRisk: 'warn' };
  riskLabels: Record<string, string> = { OnTrack: 'On Track', Watch: 'Watch', AtRisk: 'At Risk' };

  canManage = false;

  constructor(
    private projectService: ProjectService,
    private analytics: AnalyticsService,
    private auth: AuthService,
    private dialog: MatDialog,
    private snackBar: MatSnackBar,
    private cdr: ChangeDetectorRef
  ) {}


  ngOnInit(): void {
    this.canManage = this.auth.hasAnyRole(['Admin', 'ProjectManager']);
    this.displayedColumns = ['name', 'clientName', 'status', 'riskLevel', 'startDate', 'endDate', 'projectManagerName',
      ...(this.canManage ? ['actions'] : [])];
    this.load(1);
    this.analytics.getProjectRisks().subscribe({
      next: (r) => {
        if (r.hasSufficientData) r.projects.forEach((p) => (this.riskMap[p.projectId] = p));
        this.cdr.detectChanges();
      },
      error: () => {}, // 403 for non-Admin/ProjectManager roles — ignore silently
    });
  }

  load(page: number): void {
    this.loading = true;
    this.projectService.getAll(page, this.pageSize).subscribe({
      next: (result) => {
        this.dataSource.data = result.items;
        this.totalCount = result.totalCount;
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.loading = false;
        this.cdr.detectChanges();
      },
    });
  }

  onPage(event: any): void {
    this.pageSize = event.pageSize;
    this.load(event.pageIndex + 1);
  }

  delete(project: ProjectListItem): void {
    const data: ConfirmDialogData = { title: 'Delete Project', message: `Delete "${project.name}"?` };
    this.dialog
      .open(ConfirmDialogComponent, { data })
      .afterClosed()
      .subscribe((confirmed) => {
        if (!confirmed) return;
        this.projectService.delete(project.id).subscribe({
          next: () => {
            this.snackBar.open('Project deleted', 'Close', { duration: 3000 });
            this.load(1);
          },
          error: () => this.snackBar.open('Failed to delete', 'Close', { duration: 3000 }),
        });
      });
  }
}
