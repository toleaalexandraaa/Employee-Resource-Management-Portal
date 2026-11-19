import {
  AfterViewInit,
  ChangeDetectorRef,
  Component,
  ElementRef,
  OnDestroy,
  OnInit,
  ViewChild,
} from '@angular/core';
import { Chart } from 'chart.js';
import { AnalyticsService } from '../../../../core/services/analytics.service';
import { EmployeeClustersData, EmployeeClusterPoint } from '../../../../core/models/analytics.models';

const CLUSTER_COLORS: Record<string, string> = {
  'High Performer': 'rgba(67,160,71,0.8)',
  'Standard': 'rgba(63,81,181,0.8)',
  'Low Utilisation': 'rgba(239,83,80,0.8)',
};

const CLUSTER_BORDER: Record<string, string> = {
  'High Performer': '#2e7d32',
  'Standard': '#283593',
  'Low Utilisation': '#c62828',
};

@Component({
  selector: 'app-employee-clusters-tab',
  standalone: false,
  templateUrl: './employee-clusters-tab.component.html',
})
export class EmployeeClustersTabComponent implements OnInit, AfterViewInit, OnDestroy {
  @ViewChild('scatterCanvas') scatterCanvas!: ElementRef<HTMLCanvasElement>;

  data: EmployeeClustersData | null = null;
  loading = true;
  error = false;

  displayedColumns = ['employeeName', 'clusterLabel', 'hoursLogged', 'tasksCompleted', 'projectsActive', 'seniorityYears'];

  private chart: Chart | null = null;

  constructor(
    private analytics: AnalyticsService,
    private cdr: ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    this.analytics.getEmployeeClusters().subscribe({
      next: (d) => {
        this.data = d;
        this.loading = false;
        this.cdr.detectChanges();
        if (this.scatterCanvas && d.hasSufficientData) this.buildChart(d.employees);
      },
      error: () => {
        this.loading = false;
        this.error = true;
        this.cdr.detectChanges();
      },
    });
  }

  ngAfterViewInit(): void {
    if (this.data?.hasSufficientData) this.buildChart(this.data.employees);
  }

  ngOnDestroy(): void {
    this.chart?.destroy();
  }

  clusterColor(label: string): string {
    const map: Record<string, string> = {
      'High Performer': 'primary',
      'Standard': 'accent',
      'Low Utilisation': 'warn',
    };
    return map[label] ?? 'accent';
  }

  private buildChart(employees: EmployeeClusterPoint[]): void {
    this.chart?.destroy();

    const labels = [...new Set(employees.map((e) => e.clusterLabel))];
    const datasets = labels.map((label) => {
      const pts = employees.filter((e) => e.clusterLabel === label);
      return {
        label,
        data: pts.map((e) => ({ x: e.hoursLogged, y: e.tasksCompleted, name: e.employeeName })),
        backgroundColor: CLUSTER_COLORS[label] ?? 'rgba(128,128,128,0.7)',
        borderColor: CLUSTER_BORDER[label] ?? '#555',
        pointRadius: 8,
        pointHoverRadius: 10,
      };
    });

    this.chart = new Chart(this.scatterCanvas.nativeElement, {
      type: 'scatter',
      data: { datasets },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: { position: 'top' },
          tooltip: {
            callbacks: {
              label: (ctx) => {
                const pt = ctx.raw as { x: number; y: number; name: string };
                return `${pt.name}: ${pt.x.toFixed(0)}h logged, ${pt.y.toFixed(0)} tasks`;
              },
            },
          },
        },
        scales: {
          x: { title: { display: true, text: 'Hours Logged (90 days)' }, beginAtZero: true },
          y: { title: { display: true, text: 'Tasks Completed (90 days)' }, beginAtZero: true },
        },
      },
    });
  }
}
