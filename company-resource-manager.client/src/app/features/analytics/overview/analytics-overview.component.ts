import {
  AfterViewInit,
  ChangeDetectorRef,
  Component,
  ElementRef,
  OnDestroy,
  OnInit,
  ViewChild,
} from '@angular/core';
import { Chart, registerables } from 'chart.js';
import { AnalyticsService } from '../../../core/services/analytics.service';
import { OverviewKpi } from '../../../core/models/analytics.models';

Chart.register(...registerables);

@Component({
  selector: 'app-analytics-overview',
  standalone: false,
  templateUrl: './analytics-overview.component.html',
  styleUrls: ['./analytics-overview.component.css'],
})
export class AnalyticsOverviewComponent implements OnInit, AfterViewInit, OnDestroy {
  @ViewChild('revenueCanvas') revenueCanvas!: ElementRef<HTMLCanvasElement>;
  @ViewChild('hiresCanvas') hiresCanvas!: ElementRef<HTMLCanvasElement>;

  kpi: OverviewKpi | null = null;
  loading = true;
  error = false;

  private revenueChart?: Chart;
  private hiresChart?: Chart;

  constructor(
    private analytics: AnalyticsService,
    private cdr: ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    this.analytics.getOverview().subscribe({
      next: (data) => {
        this.kpi = data;
        this.loading = false;
        this.cdr.detectChanges();
        // Canvas elements are now in the DOM (loading is false); build charts.
        if (this.revenueCanvas) {
          this.buildCharts(data);
        }
      },
      error: () => {
        this.loading = false;
        this.error = true;
        this.cdr.detectChanges();
      },
    });
  }

  ngAfterViewInit(): void {
    if (this.kpi) {
      this.buildCharts(this.kpi);
    }
  }

  ngOnDestroy(): void {
    this.revenueChart?.destroy();
    this.hiresChart?.destroy();
  }

  get budgetVarianceLabel(): string {
    if (!this.kpi) return '—';
    const v = this.kpi.budgetVariancePct;
    return v === 0 ? '0%' : `${v > 0 ? '+' : ''}${v.toFixed(1)}%`;
  }

  get budgetVarianceClass(): string {
    if (!this.kpi || this.kpi.budgetVariancePct === 0) return '';
    return this.kpi.budgetVariancePct > 0 ? 'warn' : 'positive';
  }

  private buildCharts(data: OverviewKpi): void {
    this.buildRevenueChart(data);
    this.buildHiresChart(data);
  }

  private buildRevenueChart(data: OverviewKpi): void {
    this.revenueChart?.destroy();
    this.revenueChart = new Chart(this.revenueCanvas.nativeElement, {
      type: 'line',
      data: {
        labels: data.revenueLast12Months.map((m) => m.label),
        datasets: [
          {
            label: 'Revenue (paid)',
            data: data.revenueLast12Months.map((m) => m.value),
            borderColor: '#3f51b5',
            backgroundColor: 'rgba(63,81,181,0.08)',
            fill: true,
            tension: 0.3,
            pointRadius: 3,
          },
        ],
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: { legend: { display: false } },
        scales: {
          y: {
            beginAtZero: true,
            ticks: { callback: (v) => formatCurrency(Number(v)) },
          },
        },
      },
    });
  }

  private buildHiresChart(data: OverviewKpi): void {
    this.hiresChart?.destroy();
    this.hiresChart = new Chart(this.hiresCanvas.nativeElement, {
      type: 'bar',
      data: {
        labels: data.hiresLast6Months.map((m) => m.label),
        datasets: [
          {
            label: 'New Hires',
            data: data.hiresLast6Months.map((m) => m.value),
            backgroundColor: '#7986cb',
            borderRadius: 4,
          },
        ],
      },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: { legend: { display: false } },
        scales: {
          y: { beginAtZero: true, ticks: { stepSize: 1 } },
        },
      },
    });
  }
}

function formatCurrency(value: number): string {
  if (value >= 1_000_000) return `${(value / 1_000_000).toFixed(1)}M`;
  if (value >= 1_000) return `${(value / 1_000).toFixed(0)}K`;
  return String(value);
}
