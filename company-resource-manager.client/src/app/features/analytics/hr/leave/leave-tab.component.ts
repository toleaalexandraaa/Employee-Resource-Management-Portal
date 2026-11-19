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
import { HrLeaveData } from '../../../../core/models/analytics.models';

const LEAVE_COLORS: Record<string, string> = {
  Annual: '#3f51b5',
  Medical: '#e53935',
  Unpaid: '#757575',
  Parental: '#43a047',
  Other: '#fb8c00',
};

@Component({
  selector: 'app-leave-tab',
  standalone: false,
  templateUrl: './leave-tab.component.html',
  styleUrls: ['./leave-tab.component.css'],
})
export class LeaveTabComponent implements OnInit, AfterViewInit, OnDestroy {
  @ViewChild('balanceCanvas') balanceCanvas!: ElementRef<HTMLCanvasElement>;
  @ViewChild('monthCanvas') monthCanvas!: ElementRef<HTMLCanvasElement>;

  data: HrLeaveData | null = null;
  loading = true;
  error = false;

  displayedColumns = ['employeeName', 'leaveType', 'startDate', 'endDate', 'workingDays'];

  private charts: Chart[] = [];

  constructor(
    private analytics: AnalyticsService,
    private cdr: ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    this.analytics.getHrLeave().subscribe({
      next: (d) => {
        this.data = d;
        this.loading = false;
        this.cdr.detectChanges();
        if (this.balanceCanvas) this.buildCharts(d);
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

  heatmapColor(value: number, max: number): string {
    if (max === 0 || value === 0) return 'transparent';
    const opacity = Math.min(value / max, 1);
    return `rgba(63, 81, 181, ${opacity.toFixed(2)})`;
  }

  heatmapMax(data: HrLeaveData): number {
    return Math.max(...data.leaveByDepartment.flatMap((r) => r.values), 0);
  }

  private buildCharts(d: HrLeaveData): void {
    this.charts.forEach((c) => c.destroy());
    this.charts = [];

    // Balance utilization — grouped bar (Used vs Remaining)
    this.charts.push(
      new Chart(this.balanceCanvas.nativeElement, {
        type: 'bar',
        data: {
          labels: d.balanceUtilization.map((b) => b.leaveType),
          datasets: [
            {
              label: 'Used',
              data: d.balanceUtilization.map((b) => b.usedDays),
              backgroundColor: d.balanceUtilization.map((b) => LEAVE_COLORS[b.leaveType] ?? '#3f51b5'),
              borderRadius: 3,
            },
            {
              label: 'Remaining',
              data: d.balanceUtilization.map((b) => Math.max(0, b.totalDays - b.usedDays)),
              backgroundColor: 'rgba(0,0,0,0.08)',
              borderRadius: 3,
            },
          ],
        },
        options: {
          responsive: true,
          maintainAspectRatio: false,
          plugins: { legend: { position: 'top' } },
          scales: { y: { beginAtZero: true } },
        },
      }),
    );

    // Leave by month — stacked bar per type
    this.charts.push(
      new Chart(this.monthCanvas.nativeElement, {
        type: 'bar',
        data: {
          labels: d.leaveMonthLabels,
          datasets: d.leaveTypeLabels.map((type, idx) => ({
            label: type,
            data: d.leaveByMonthMatrix[idx],
            backgroundColor: LEAVE_COLORS[type] ?? '#9e9e9e',
            stack: 'leave',
            borderRadius: 2,
          })),
        },
        options: {
          responsive: true,
          maintainAspectRatio: false,
          plugins: { legend: { position: 'top' } },
          scales: {
            x: { stacked: true },
            y: { stacked: true, beginAtZero: true },
          },
        },
      }),
    );
  }
}
