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
import { ProjectHealthData } from '../../../../core/models/analytics.models';

const STATUS_COLORS: Record<string, string> = {
  Planning: '#9e9e9e',
  Active: '#43a047',
  OnHold: '#fb8c00',
  Completed: '#3f51b5',
  Cancelled: '#ef5350',
};

@Component({
  selector: 'app-health-tab',
  standalone: false,
  templateUrl: './health-tab.component.html',
  styleUrls: ['./health-tab.component.css'],
})
export class HealthTabComponent implements OnInit, AfterViewInit, OnDestroy {
  @ViewChild('statusCanvas') statusCanvas!: ElementRef<HTMLCanvasElement>;
  @ViewChild('budgetCanvas') budgetCanvas!: ElementRef<HTMLCanvasElement>;
  @ViewChild('scatterCanvas') scatterCanvas!: ElementRef<HTMLCanvasElement>;

  data: ProjectHealthData | null = null;
  loading = true;
  error = false;

  private charts: Chart[] = [];

  constructor(
    private analytics: AnalyticsService,
    private cdr: ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    this.analytics.getProjectHealth().subscribe({
      next: (d) => {
        this.data = d;
        this.loading = false;
        this.cdr.detectChanges();
        if (this.statusCanvas) this.buildCharts(d);
      },
      error: () => {
        this.loading = false;
        this.error = true;
        this.cdr.detectChanges();
      },
    });
  }

  ngAfterViewInit(): void {
    if (this.data) this.buildCharts(this.data);
  }

  ngOnDestroy(): void {
    this.charts.forEach((c) => c.destroy());
  }

  get onTimeRate(): number {
    if (!this.data || this.data.completedCount === 0) return 0;
    return Math.round((this.data.onTimeCount / this.data.completedCount) * 100);
  }

  private buildCharts(d: ProjectHealthData): void {
    this.charts.forEach((c) => c.destroy());
    this.charts = [];

    // Projects by status — doughnut
    this.charts.push(
      new Chart(this.statusCanvas.nativeElement, {
        type: 'doughnut',
        data: {
          labels: d.byStatus.map((x) => x.label),
          datasets: [
            {
              data: d.byStatus.map((x) => x.value),
              backgroundColor: d.byStatus.map((x) => STATUS_COLORS[x.label] ?? '#9e9e9e'),
            },
          ],
        },
        options: {
          responsive: true,
          maintainAspectRatio: false,
          plugins: { legend: { position: 'right' } },
        },
      }),
    );

    // Budget variance — horizontal bar (red = over, green = under)
    this.charts.push(
      new Chart(this.budgetCanvas.nativeElement, {
        type: 'bar',
        data: {
          labels: d.budgetVariances.map((x) => x.projectName),
          datasets: [
            {
              label: 'Budget Variance (RON)',
              data: d.budgetVariances.map((x) => x.variance),
              backgroundColor: d.budgetVariances.map((x) =>
                x.variance > 0 ? '#ef5350' : '#43a047',
              ),
              borderRadius: 4,
            },
          ],
        },
        options: {
          indexAxis: 'y',
          responsive: true,
          maintainAspectRatio: false,
          plugins: { legend: { display: false } },
          scales: { x: { beginAtZero: false } },
        },
      }),
    );

    // Duration scatter — estimated vs actual days
    if (d.durationScatter.length > 0) {
      const points = d.durationScatter;
      const projectNames = points.map((p) => p.projectName);
      const maxVal = Math.max(...points.map((p) => Math.max(p.estimatedDays, p.actualDays)), 1);
      this.charts.push(
        new Chart(this.scatterCanvas.nativeElement, {
          type: 'scatter',
          data: {
            datasets: [
              {
                label: 'Projects',
                data: points.map((p) => ({ x: p.estimatedDays, y: p.actualDays })),
                backgroundColor: points.map((p) =>
                  p.actualDays > p.estimatedDays
                    ? 'rgba(239,83,80,0.7)'
                    : 'rgba(67,160,71,0.7)',
                ),
                pointRadius: 6,
              },
              {
                label: 'Reference',
                data: [
                  { x: 0, y: 0 },
                  { x: maxVal, y: maxVal },
                ],
                type: 'line' as const,
                borderColor: 'rgba(0,0,0,0.2)',
                borderDash: [4, 4],
                pointRadius: 0,
                fill: false,
              },
            ],
          },
          options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: {
              legend: { display: false },
              tooltip: {
                callbacks: {
                  label: (ctx) => {
                    if (ctx.datasetIndex !== 0) return '';
                    const name = projectNames[ctx.dataIndex] ?? '';
                    return `${name}: Est ${ctx.parsed.x}d → Act ${ctx.parsed.y}d`;
                  },
                },
              },
            },
            scales: {
              x: { title: { display: true, text: 'Estimated Days' }, beginAtZero: true },
              y: { title: { display: true, text: 'Actual Days' }, beginAtZero: true },
            },
          },
        }),
      );
    }
  }
}
