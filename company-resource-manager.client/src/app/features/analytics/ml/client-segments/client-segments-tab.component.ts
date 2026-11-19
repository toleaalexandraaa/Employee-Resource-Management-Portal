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
import { ClientSegmentsData, ClientSegmentPoint } from '../../../../core/models/analytics.models';

const SEGMENT_COLORS: Record<string, string> = {
  'Key Account': 'rgba(63,81,181,0.7)',
  'Growth Client': 'rgba(67,160,71,0.7)',
  'At Risk': 'rgba(239,83,80,0.7)',
};

@Component({
  selector: 'app-client-segments-tab',
  standalone: false,
  templateUrl: './client-segments-tab.component.html',
})
export class ClientSegmentsTabComponent implements OnInit, AfterViewInit, OnDestroy {
  @ViewChild('bubbleCanvas') bubbleCanvas!: ElementRef<HTMLCanvasElement>;

  data: ClientSegmentsData | null = null;
  loading = true;
  error = false;

  displayedColumns = ['clientName', 'segmentLabel', 'totalRevenue', 'projectCount', 'avgPaymentDelayDays', 'openInvoiceAmount'];

  private chart: Chart | null = null;

  constructor(
    private analytics: AnalyticsService,
    private cdr: ChangeDetectorRef,
  ) {}

  ngOnInit(): void {
    this.analytics.getClientSegments().subscribe({
      next: (d) => {
        this.data = d;
        this.loading = false;
        this.cdr.detectChanges();
        if (this.bubbleCanvas && d.hasSufficientData) this.buildChart(d.clients);
      },
      error: () => {
        this.loading = false;
        this.error = true;
        this.cdr.detectChanges();
      },
    });
  }

  ngAfterViewInit(): void {
    if (this.data?.hasSufficientData) this.buildChart(this.data.clients);
  }

  ngOnDestroy(): void {
    this.chart?.destroy();
  }

  segmentColor(label: string): string {
    const map: Record<string, string> = {
      'Key Account': 'primary',
      'Growth Client': 'accent',
      'At Risk': 'warn',
    };
    return map[label] ?? 'accent';
  }

  private buildChart(clients: ClientSegmentPoint[]): void {
    this.chart?.destroy();

    const maxOpen = Math.max(...clients.map((c) => c.openInvoiceAmount), 1);
    const labels = [...new Set(clients.map((c) => c.clusterLabel))];

    const datasets = labels.map((label) => {
      const pts = clients.filter((c) => c.clusterLabel === label);
      return {
        label,
        data: pts.map((c) => ({
          x: c.totalRevenue,
          y: c.avgPaymentDelayDays,
          r: Math.max(6, (c.openInvoiceAmount / maxOpen) * 30),
          name: c.clientName,
          open: c.openInvoiceAmount,
        })),
        backgroundColor: SEGMENT_COLORS[label] ?? 'rgba(128,128,128,0.6)',
      };
    });

    this.chart = new Chart(this.bubbleCanvas.nativeElement, {
      type: 'bubble',
      data: { datasets },
      options: {
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
          legend: { position: 'top' },
          tooltip: {
            callbacks: {
              label: (ctx) => {
                const pt = ctx.raw as { x: number; y: number; r: number; name: string; open: number };
                return [
                  pt.name,
                  `Revenue: ${pt.x.toLocaleString()} RON`,
                  `Avg delay: ${pt.y.toFixed(1)} days`,
                  `Open invoices: ${pt.open.toLocaleString()} RON`,
                ];
              },
            },
          },
        },
        scales: {
          x: { title: { display: true, text: 'Total Revenue (RON)' }, beginAtZero: true },
          y: { title: { display: true, text: 'Avg Payment Delay (days)' } },
        },
      },
    });
  }
}
