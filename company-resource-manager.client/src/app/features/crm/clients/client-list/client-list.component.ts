import { Component, OnInit, ViewChild, ChangeDetectorRef } from '@angular/core';
import { MatTableDataSource } from '@angular/material/table';
import { MatPaginator } from '@angular/material/paginator';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ClientService } from '../../../../core/services/client.service';
import { AnalyticsService } from '../../../../core/services/analytics.service';
import { ClientListItem } from '../../../../core/models/crm.models';
import { ClientSegmentPoint } from '../../../../core/models/analytics.models';
import {
  ConfirmDialogComponent,
  ConfirmDialogData,
} from '../../../../shared/components/confirm-dialog/confirm-dialog.component';

@Component({
  selector: 'app-client-list',
  standalone: false,
  templateUrl: './client-list.component.html',
})
export class ClientListComponent implements OnInit {
  @ViewChild(MatPaginator) paginator!: MatPaginator;
  displayedColumns = ['companyName', 'industry', 'status', 'segment', 'accountManagerName', 'actions'];
  dataSource = new MatTableDataSource<ClientListItem>();
  totalCount = 0;
  pageSize = 10;
  loading = false;

  segmentMap: Record<string, ClientSegmentPoint> = {};
  statusColors: Record<string, string> = { Active: 'primary', Inactive: 'warn', Prospect: 'accent' };
  segmentColors: Record<string, string> = { 'Key Account': 'primary', 'Growth Client': 'accent', 'At Risk': 'warn' };

  constructor(
    private clientService: ClientService,
    private analytics: AnalyticsService,
    private dialog: MatDialog,
    private snackBar: MatSnackBar,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.load(1);
    this.analytics.getClientSegments().subscribe({
      next: (r) => {
        if (r.hasSufficientData) r.clients.forEach((c) => (this.segmentMap[c.clientId] = c));
        this.cdr.detectChanges();
      },
    });
  }

  load(page: number): void {
    this.loading = true;
    this.clientService.getAll(page, this.pageSize).subscribe({
      next: (r) => {
        this.dataSource.data = r.items;
        this.totalCount = r.totalCount;
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.loading = false;
        this.cdr.detectChanges();
      },
    });
  }

  onPage(event: any): void {
    this.pageSize = event.pageSize;
    this.load(event.pageIndex + 1);
  }

  delete(client: ClientListItem): void {
    const data: ConfirmDialogData = {
      title: 'Delete Client',
      message: `Delete "${client.companyName}"?`,
    };
    this.dialog
      .open(ConfirmDialogComponent, { data })
      .afterClosed()
      .subscribe((confirmed) => {
        if (!confirmed) return;
        this.clientService.delete(client.id).subscribe({
          next: () => {
            this.snackBar.open('Client deleted', 'Close', { duration: 3000 });
            this.load(1);
          },
          error: () => this.snackBar.open('Failed to delete', 'Close', { duration: 3000 }),
        });
      });
  }
}
