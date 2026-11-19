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
import { ProjectTasksData } from '../../../../core/models/analytics.models';

const STATUS_COLORS: Record<string, string> = {
  Backlog: '#9e9e9e',
  InProgress: '#3f51b5',
  InReview: '#fb8c00',
  Done: '#43a047',
};

const PRIORITY_COLORS = ['#66bb6a', '#42a5f5', '#ffa726', '#ef5350'];

@Component({
  selector: 'app-tasks-tab',
  standalone: false,
  templateUrl: './tasks-tab.component.html',
  styleUrls: ['./tasks-tab.component.css'],
})
export class TasksTabComponent implements OnInit, AfterViewInit, OnDestroy {
  @ViewChild('statusCanvas') statusCanvas!: ElementRef<HTMLCanvasElement>;
  @ViewChild('priorityCanvas') priorityCanvas!: ElementRef<HTMLCanvasElement>;
  @ViewChild('cycleCanvas') cycleCanvas!: ElementRef<HTMLCanvasElement>;
  @ViewChild('velocityCanvas') velocityCanvas!: ElementRef<HTMLCanvasElement>;

  data: ProjectTasksData | null = null;
  loading = true;
  error = false;

  private charts: Chart[] = [];

  constructor(
    private analytics: AnalyticsService,
    private cdr: ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    this.analytics.getProjectTasks().subscribe({
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

  private buildCharts(d: ProjectTasksData): void {
    this.charts.forEach((c) => c.destroy());
    this.charts = [];

    // Task status distribution — bar
    this.charts.push(
      new Chart(this.statusCanvas.nativeElement, {
        type: 'bar',
        data: {
          labels: d.tasksByStatus.map((x) => x.label),
          datasets: [
            {
              label: 'Tasks',
              data: d.tasksByStatus.map((x) => x.value),
              backgroundColor: d.tasksByStatus.map((x) => STATUS_COLORS[x.label] ?? '#9e9e9e'),
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

    // Tasks by priority — doughnut
    this.charts.push(
      new Chart(this.priorityCanvas.nativeElement, {
        type: 'doughnut',
        data: {
          labels: d.tasksByPriority.map((x) => x.label),
          datasets: [
            {
              data: d.tasksByPriority.map((x) => x.value),
              backgroundColor: PRIORITY_COLORS,
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

    // Avg cycle time by priority — bar
    if (d.avgCycleTimeByPriority.length > 0) {
      this.charts.push(
        new Chart(this.cycleCanvas.nativeElement, {
          type: 'bar',
          data: {
            labels: d.avgCycleTimeByPriority.map((x) => x.label),
            datasets: [
              {
                label: 'Avg Days',
                data: d.avgCycleTimeByPriority.map((x) => x.value),
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

    // Task completion velocity — line
    this.charts.push(
      new Chart(this.velocityCanvas.nativeElement, {
        type: 'line',
        data: {
          labels: d.completionVelocity.map((x) => x.label),
          datasets: [
            {
              label: 'Tasks Completed',
              data: d.completionVelocity.map((x) => x.value),
              borderColor: '#43a047',
              backgroundColor: 'rgba(67,160,71,0.08)',
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
          scales: { y: { beginAtZero: true, ticks: { stepSize: 1 } } },
        },
      }),
    );
  }
}
