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
import { MemberUtilization, ProjectTimeData } from '../../../../core/models/analytics.models';

@Component({
  selector: 'app-time-tab',
  standalone: false,
  templateUrl: './time-tab.component.html',
  styleUrls: ['./time-tab.component.css'],
})
export class TimeTabComponent implements OnInit, AfterViewInit, OnDestroy {
  @ViewChild('hoursCanvas') hoursCanvas!: ElementRef<HTMLCanvasElement>;
  @ViewChild('dayCanvas') dayCanvas!: ElementRef<HTMLCanvasElement>;
  @ViewChild('weeklyCanvas') weeklyCanvas!: ElementRef<HTMLCanvasElement>;

  data: ProjectTimeData | null = null;
  loading = true;
  error = false;

  private charts: Chart[] = [];

  constructor(
    private analytics: AnalyticsService,
    private cdr: ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    this.analytics.getProjectTime().subscribe({
      next: (d) => {
        this.data = d;
        this.loading = false;
        this.cdr.detectChanges();
        if (this.hoursCanvas) this.buildCharts(d);
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

  utilizationPct(m: MemberUtilization): number {
    if (m.allocatedHoursPerWeek <= 0) return 0;
    return Math.round((m.loggedHoursThisWeek / m.allocatedHoursPerWeek) * 100);
  }

  utilizationColor(m: MemberUtilization): string {
    const pct = this.utilizationPct(m);
    if (pct > 100) return '#ef5350';
    if (pct >= 70) return '#43a047';
    return '#fb8c00';
  }

  private buildCharts(d: ProjectTimeData): void {
    this.charts.forEach((c) => c.destroy());
    this.charts = [];

    // Hours logged vs estimated by project — grouped bar
    this.charts.push(
      new Chart(this.hoursCanvas.nativeElement, {
        type: 'bar',
        data: {
          labels: d.hoursByProject.map((x) => x.projectName),
          datasets: [
            {
              label: 'Estimated Hours',
              data: d.hoursByProject.map((x) => x.estimatedHours),
              backgroundColor: 'rgba(63,81,181,0.6)',
              borderRadius: 4,
            },
            {
              label: 'Logged Hours',
              data: d.hoursByProject.map((x) => x.loggedHours),
              backgroundColor: 'rgba(67,160,71,0.7)',
              borderRadius: 4,
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

    // Hours by day of week — bar
    this.charts.push(
      new Chart(this.dayCanvas.nativeElement, {
        type: 'bar',
        data: {
          labels: d.hoursByDayOfWeek.map((x) => x.label),
          datasets: [
            {
              label: 'Hours',
              data: d.hoursByDayOfWeek.map((x) => x.value),
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

    // Weekly hours trend — line
    this.charts.push(
      new Chart(this.weeklyCanvas.nativeElement, {
        type: 'line',
        data: {
          labels: d.weeklyHours.map((x) => x.label),
          datasets: [
            {
              label: 'Hours Logged',
              data: d.weeklyHours.map((x) => x.value),
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
          scales: { y: { beginAtZero: true } },
        },
      }),
    );
  }
}
