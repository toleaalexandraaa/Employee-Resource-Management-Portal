import { Component, OnInit, ViewChild, ChangeDetectorRef } from '@angular/core';
import { MatTableDataSource } from '@angular/material/table';
import { MatPaginator } from '@angular/material/paginator';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { InvoiceService } from '../../../../core/services/invoice.service';
import { InvoiceListItem } from '../../../../core/models/crm.models';
import {
  ConfirmDialogComponent,
  ConfirmDialogData,
} from '../../../../shared/components/confirm-dialog/confirm-dialog.component';

@Component({
  selector: 'app-invoice-list',
  standalone: false,
  templateUrl: './invoice-list.component.html',
})
export class InvoiceListComponent implements OnInit {
  @ViewChild(MatPaginator) paginator!: MatPaginator;
  displayedColumns = [
    'invoiceNumber',
    'clientName',
    'issueDate',
    'dueDate',
    'totalAmount',
    'paidAmount',
    'status',
    'actions',
  ];
  dataSource = new MatTableDataSource<InvoiceListItem>();
  totalCount = 0;
  pageSize = 10;
  loading = false;

  statusColors: Record<string, string> = {
    Draft: '',
    Sent: 'accent',
    PartiallyPaid: 'accent',
    Paid: 'primary',
    Overdue: 'warn',
    Cancelled: '',
  };

  constructor(
    private invoiceService: InvoiceService,
    private dialog: MatDialog,
    private snackBar: MatSnackBar,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.load(1);
  }

  load(page: number): void {
    this.loading = true;
    this.invoiceService.getAll(page, this.pageSize).subscribe({
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

  send(invoice: InvoiceListItem): void {
    this.invoiceService.send(invoice.id).subscribe({
      next: () => {
        this.snackBar.open('Invoice sent', 'Close', { duration: 3000 });
        this.load(1);
      },
    });
  }

  delete(invoice: InvoiceListItem): void {
    const data: ConfirmDialogData = {
      title: 'Delete Invoice',
      message: `Delete invoice "${invoice.invoiceNumber}"?`,
    };
    this.dialog
      .open(ConfirmDialogComponent, { data })
      .afterClosed()
      .subscribe((confirmed) => {
        if (!confirmed) return;
        this.invoiceService.delete(invoice.id).subscribe({
          next: () => {
            this.snackBar.open('Invoice deleted', 'Close', { duration: 3000 });
            this.load(1);
          },
        });
      });
  }
}
