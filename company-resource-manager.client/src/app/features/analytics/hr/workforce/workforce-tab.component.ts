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
import { WorkforceData } from '../../../../core/models/analytics.models';

const CHART_BLUE = '#3f51b5';
const CONTRACT_COLORS = ['#3f51b5', '#7986cb', '#9fa8da', '#c5cae9'];

@Component({
  selector: 'app-workforce-tab',
  standalone: false,
  templateUrl: './workforce-tab.component.html',
  styleUrls: ['./workforce-tab.component.css'],
})
export class WorkforceTabComponent implements OnInit, AfterViewInit, OnDestroy {
  @ViewChild('deptCanvas') deptCanvas!: ElementRef<HTMLCanvasElement>;
  @ViewChild('contractCanvas') contractCanvas!: ElementRef<HTMLCanvasElement>;
  @ViewChild('seniorityCanvas') seniorityCanvas!: ElementRef<HTMLCanvasElement>;
  @ViewChild('hiresCanvas') hiresCanvas!: ElementRef<HTMLCanvasElement>;

  data: WorkforceData | null = null;
  loading = true;
  error = false;

  private charts: Chart[] = [];

  constructor(
    private analytics: AnalyticsService,
    private cdr: ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    this.analytics.getHrWorkforce().subscribe({
      next: (d) => {
        this.data = d;
        this.loading = false;
        this.cdr.detectChanges();
        if (this.deptCanvas) this.buildCharts(d);
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

  private buildCharts(d: WorkforceData): void {
    this.charts.forEach((c) => c.destroy());
    this.charts = [];

    // Employees by department — horizontal bar
    this.charts.push(
      new Chart(this.deptCanvas.nativeElement, {
        type: 'bar',
        data: {
          labels: d.employeesByDepartment.map((x) => x.label),
          datasets: [
            {
              label: 'Employees',
              data: d.employeesByDepartment.map((x) => x.value),
              backgroundColor: CHART_BLUE,
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

    // Contract type — doughnut
    this.charts.push(
      new Chart(this.contractCanvas.nativeElement, {
        type: 'doughnut',
        data: {
          labels: d.employeesByContractType.map((x) => x.label),
          datasets: [
            {
              data: d.employeesByContractType.map((x) => x.value),
              backgroundColor: CONTRACT_COLORS,
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

    // Seniority distribution — bar
    this.charts.push(
      new Chart(this.seniorityCanvas.nativeElement, {
        type: 'bar',
        data: {
          labels: d.seniorityDistribution.map((x) => x.label),
          datasets: [
            {
              label: 'Employees',
              data: d.seniorityDistribution.map((x) => x.value),
              backgroundColor: '#7986cb',
              borderRadius: 4,
            },
          ],
        },
        options: {
          responsive: true,
          maintainAspectRatio: false,
          plugins: { legend: { display: false } },
          scales: { y: { beginAtZero: true, ticks: { stepSize: 1 } } },
        },
      }),
    );

    // Hiring trend — line
    this.charts.push(
      new Chart(this.hiresCanvas.nativeElement, {
        type: 'line',
        data: {
          labels: d.hiresLast24Months.map((m) => m.label),
          datasets: [
            {
              label: 'New Hires',
              data: d.hiresLast24Months.map((m) => m.value),
              borderColor: CHART_BLUE,
              backgroundColor: 'rgba(63,81,181,0.08)',
              fill: true,
              tension: 0.3,
              pointRadius: 2,
            },
          ],
        },
        options: {
          responsive: true,
          maintainAspectRatio: false,
          plugins: { legend: { display: false } },
          scales: { y: { beginAtZero: true, ticks: { stepSize: 1 } } },
        },
      }),
    );
  }
}
