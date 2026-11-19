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
import { HrPayrollData } from '../../../../core/models/analytics.models';

@Component({
  selector: 'app-payroll-tab',
  standalone: false,
  templateUrl: './payroll-tab.component.html',
  styleUrls: ['./payroll-tab.component.css'],
})
export class PayrollTabComponent implements OnInit, AfterViewInit, OnDestroy {
  @ViewChild('costCanvas') costCanvas!: ElementRef<HTMLCanvasElement>;
  @ViewChild('salaryCanvas') salaryCanvas!: ElementRef<HTMLCanvasElement>;
  @ViewChild('compositionCanvas') compositionCanvas!: ElementRef<HTMLCanvasElement>;

  data: HrPayrollData | null = null;
  loading = true;
  error = false;

  private charts: Chart[] = [];

  constructor(
    private analytics: AnalyticsService,
    private cdr: ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    this.analytics.getHrPayroll().subscribe({
      next: (d) => {
        this.data = d;
        this.loading = false;
        this.cdr.detectChanges();
        if (this.costCanvas) this.buildCharts(d);
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

  get totalThisMonth(): number {
    if (!this.data) return 0;
    return this.data.currentMonthDraft + this.data.currentMonthApproved + this.data.currentMonthPaid;
  }

  private buildCharts(d: HrPayrollData): void {
    this.charts.forEach((c) => c.destroy());
    this.charts = [];

    // Payroll cost trend — line
    this.charts.push(
      new Chart(this.costCanvas.nativeElement, {
        type: 'line',
        data: {
          labels: d.costLast12Months.map((m) => m.label),
          datasets: [
            {
              label: 'Net Payroll (RON)',
              data: d.costLast12Months.map((m) => m.value),
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

    // Avg salary by department — horizontal bar
    this.charts.push(
      new Chart(this.salaryCanvas.nativeElement, {
        type: 'bar',
        data: {
          labels: d.avgSalaryByDepartment.map((x) => x.label),
          datasets: [
            {
              label: 'Avg Gross Salary (RON)',
              data: d.avgSalaryByDepartment.map((x) => x.value),
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

    // Composition — doughnut (Net, Tax, Social)
    const c = d.composition;
    if (!this.compositionCanvas) return;
    this.charts.push(
      new Chart(this.compositionCanvas.nativeElement, {
        type: 'doughnut',
        data: {
          labels: ['Take-Home (Net)', 'Tax (10%)', 'Social (35%)'],
          datasets: [
            {
              data: [c.avgNet, c.avgTax, c.avgSocial],
              backgroundColor: ['#43a047', '#ef5350', '#fb8c00'],
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
