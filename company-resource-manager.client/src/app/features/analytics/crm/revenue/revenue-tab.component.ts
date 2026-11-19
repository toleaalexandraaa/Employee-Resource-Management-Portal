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
import { CrmRevenueData } from '../../../../core/models/analytics.models';

const INDUSTRY_COLORS = [
  '#3f51b5', '#7986cb', '#42a5f5', '#26c6da', '#43a047',
  '#66bb6a', '#ffa726', '#ef5350', '#ab47bc', '#78909c',
];

@Component({
  selector: 'app-revenue-tab',
  standalone: false,
  templateUrl: './revenue-tab.component.html',
  styleUrls: ['./revenue-tab.component.css'],
})
export class RevenueTabComponent implements OnInit, AfterViewInit, OnDestroy {
  @ViewChild('trendCanvas') trendCanvas!: ElementRef<HTMLCanvasElement>;
  @ViewChild('clientCanvas') clientCanvas!: ElementRef<HTMLCanvasElement>;
  @ViewChild('industryCanvas') industryCanvas!: ElementRef<HTMLCanvasElement>;

  data: CrmRevenueData | null = null;
  loading = true;
  error = false;

  private charts: Chart[] = [];

  constructor(
    private analytics: AnalyticsService,
    private cdr: ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    this.analytics.getCrmRevenue().subscribe({
      next: (d) => {
        this.data = d;
        this.loading = false;
        this.cdr.detectChanges();
        if (this.trendCanvas) this.buildCharts(d);
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

  agingBarColor(bucket: string): string {
    if (bucket === 'Not Yet Due') return '#43a047';
    if (bucket === '0–30 Days') return '#ffa726';
    if (bucket === '31–60 Days') return '#ef5350';
    return '#b71c1c';
  }

  private buildCharts(d: CrmRevenueData): void {
    this.charts.forEach((c) => c.destroy());
    this.charts = [];

    // Monthly revenue trend — line
    this.charts.push(
      new Chart(this.trendCanvas.nativeElement, {
        type: 'line',
        data: {
          labels: d.revenueLast12Months.map((x) => x.label),
          datasets: [
            {
              label: 'Revenue (RON)',
              data: d.revenueLast12Months.map((x) => x.value),
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
              ticks: { callback: (v) => `${(Number(v) / 1000).toFixed(0)}K` },
            },
          },
        },
      }),
    );

    // Revenue by client — horizontal bar
    if (d.revenueByClient.length > 0) {
      this.charts.push(
        new Chart(this.clientCanvas.nativeElement, {
          type: 'bar',
          data: {
            labels: d.revenueByClient.map((x) => x.label),
            datasets: [
              {
                label: 'Revenue (RON)',
                data: d.revenueByClient.map((x) => x.value),
                backgroundColor: '#7986cb',
                borderRadius: 4,
              },
            ],
          },
          options: {
            indexAxis: 'y',
            responsive: true,
            maintainAspectRatio: false,
            plugins: { legend: { display: false } },
            scales: { x: { beginAtZero: true } },
          },
        }),
      );
    }

    // Revenue by industry — doughnut
    if (d.revenueByIndustry.length > 0) {
      this.charts.push(
        new Chart(this.industryCanvas.nativeElement, {
          type: 'doughnut',
          data: {
            labels: d.revenueByIndustry.map((x) => x.label),
            datasets: [
              {
                data: d.revenueByIndustry.map((x) => x.value),
                backgroundColor: INDUSTRY_COLORS,
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
    }
  }
}
