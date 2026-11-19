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
import { CrmClientsData } from '../../../../core/models/analytics.models';

@Component({
  selector: 'app-clients-tab',
  standalone: false,
  templateUrl: './clients-tab.component.html',
  styleUrls: ['./clients-tab.component.css'],
})
export class ClientsTabComponent implements OnInit, AfterViewInit, OnDestroy {
  @ViewChild('statusCanvas') statusCanvas!: ElementRef<HTMLCanvasElement>;
  @ViewChild('industryCanvas') industryCanvas!: ElementRef<HTMLCanvasElement>;
  @ViewChild('acquisitionCanvas') acquisitionCanvas!: ElementRef<HTMLCanvasElement>;

  data: CrmClientsData | null = null;
  loading = true;
  error = false;

  readonly tableColumns = ['clientName', 'projectCount', 'totalInvoiced', 'totalPaid'];

  private charts: Chart[] = [];

  constructor(
    private analytics: AnalyticsService,
    private cdr: ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    this.analytics.getCrmClients().subscribe({
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

  private buildCharts(d: CrmClientsData): void {
    this.charts.forEach((c) => c.destroy());
    this.charts = [];

    // Clients by status — doughnut
    this.charts.push(
      new Chart(this.statusCanvas.nativeElement, {
        type: 'doughnut',
        data: {
          labels: d.clientsByStatus.map((x) => x.label),
          datasets: [
            {
              data: d.clientsByStatus.map((x) => x.value),
              backgroundColor: ['#43a047', '#9e9e9e', '#7986cb'],
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

    // Clients by industry — bar
    if (d.clientsByIndustry.length > 0) {
      this.charts.push(
        new Chart(this.industryCanvas.nativeElement, {
          type: 'bar',
          data: {
            labels: d.clientsByIndustry.map((x) => x.label),
            datasets: [
              {
                label: 'Clients',
                data: d.clientsByIndustry.map((x) => x.value),
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
    }

    // Acquisition by quarter — bar
    this.charts.push(
      new Chart(this.acquisitionCanvas.nativeElement, {
        type: 'bar',
        data: {
          labels: d.acquisitionByQuarter.map((x) => x.label),
          datasets: [
            {
              label: 'New Clients',
              data: d.acquisitionByQuarter.map((x) => x.value),
              backgroundColor: '#3f51b5',
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
  }
}
