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
import { CrmLeadsData } from '../../../../core/models/analytics.models';

const SOURCE_COLORS = ['#3f51b5', '#7986cb', '#9fa8da', '#c5cae9', '#43a047', '#66bb6a'];

@Component({
  selector: 'app-leads-tab',
  standalone: false,
  templateUrl: './leads-tab.component.html',
  styleUrls: ['./leads-tab.component.css'],
})
export class LeadsTabComponent implements OnInit, AfterViewInit, OnDestroy {
  @ViewChild('funnelCanvas') funnelCanvas!: ElementRef<HTMLCanvasElement>;
  @ViewChild('sourceCanvas') sourceCanvas!: ElementRef<HTMLCanvasElement>;
  @ViewChild('dealCanvas') dealCanvas!: ElementRef<HTMLCanvasElement>;
  @ViewChild('conversionCanvas') conversionCanvas!: ElementRef<HTMLCanvasElement>;

  data: CrmLeadsData | null = null;
  loading = true;
  error = false;

  private charts: Chart[] = [];

  constructor(
    private analytics: AnalyticsService,
    private cdr: ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    this.analytics.getCrmLeads().subscribe({
      next: (d) => {
        this.data = d;
        this.loading = false;
        this.cdr.detectChanges();
        if (this.funnelCanvas) this.buildCharts(d);
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

  get winRate(): number {
    if (!this.data) return 0;
    const total = this.data.wonCount + this.data.lostCount;
    if (total === 0) return 0;
    return Math.round((this.data.wonCount / total) * 100);
  }

  get openPipelineValue(): number {
    if (!this.data) return 0;
    return this.data.funnel
      .filter((s) => s.stage !== 'Won')
      .reduce((sum, s) => sum + s.totalValue, 0);
  }

  private buildCharts(d: CrmLeadsData): void {
    this.charts.forEach((c) => c.destroy());
    this.charts = [];

    // Pipeline funnel — horizontal bar (stage count)
    const funnelColors = ['#9e9e9e', '#7986cb', '#42a5f5', '#fb8c00', '#43a047'];
    this.charts.push(
      new Chart(this.funnelCanvas.nativeElement, {
        type: 'bar',
        data: {
          labels: d.funnel.map((s) => s.stage),
          datasets: [
            {
              label: 'Leads',
              data: d.funnel.map((s) => s.count),
              backgroundColor: funnelColors,
              borderRadius: 4,
            },
          ],
        },
        options: {
          indexAxis: 'y',
          responsive: true,
          maintainAspectRatio: false,
          plugins: { legend: { display: false } },
          scales: { x: { beginAtZero: true, ticks: { stepSize: 1 } } },
        },
      }),
    );

    // Leads by source — doughnut
    this.charts.push(
      new Chart(this.sourceCanvas.nativeElement, {
        type: 'doughnut',
        data: {
          labels: d.leadsBySource.map((x) => x.label),
          datasets: [
            {
              data: d.leadsBySource.map((x) => x.value),
              backgroundColor: SOURCE_COLORS,
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

    // Avg deal size by source — bar
    if (d.avgDealSizeBySource.length > 0) {
      this.charts.push(
        new Chart(this.dealCanvas.nativeElement, {
          type: 'bar',
          data: {
            labels: d.avgDealSizeBySource.map((x) => x.label),
            datasets: [
              {
                label: 'Avg Deal Size (RON)',
                data: d.avgDealSizeBySource.map((x) => x.value),
                backgroundColor: '#7986cb',
                borderRadius: 4,
              },
            ],
          },
          options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: { legend: { display: false } },
            scales: { y: { beginAtZero: true } },
          },
        }),
      );
    }

    // Avg conversion days by source — bar
    if (d.avgConversionDaysBySource.length > 0) {
      this.charts.push(
        new Chart(this.conversionCanvas.nativeElement, {
          type: 'bar',
          data: {
            labels: d.avgConversionDaysBySource.map((x) => x.label),
            datasets: [
              {
                label: 'Avg Days to Win',
                data: d.avgConversionDaysBySource.map((x) => x.value),
                backgroundColor: '#fb8c00',
                borderRadius: 4,
              },
            ],
          },
          options: {
            responsive: true,
            maintainAspectRatio: false,
            plugins: { legend: { display: false } },
            scales: { y: { beginAtZero: true } },
          },
        }),
      );
    }
  }
}
