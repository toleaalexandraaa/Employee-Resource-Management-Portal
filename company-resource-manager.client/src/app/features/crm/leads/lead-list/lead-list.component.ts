import { Component, OnInit, ViewChild, ChangeDetectorRef } from '@angular/core';
import { MatTableDataSource } from '@angular/material/table';
import { MatPaginator } from '@angular/material/paginator';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { LeadService } from '../../../../core/services/lead.service';
import { Lead } from '../../../../core/models/crm.models';
import {
  ConfirmDialogComponent,
  ConfirmDialogData,
} from '../../../../shared/components/confirm-dialog/confirm-dialog.component';
import { ConvertLeadDialogComponent } from '../convert-lead-dialog/convert-lead-dialog.component';

@Component({
  selector: 'app-lead-list',
  standalone: false,
  templateUrl: './lead-list.component.html',
})
export class LeadListComponent implements OnInit {
  @ViewChild(MatPaginator) paginator!: MatPaginator;
  displayedColumns = [
    'companyName',
    'contactName',
    'stage',
    'estimatedValue',
    'assignedToEmployeeName',
    'actions',
  ];
  dataSource = new MatTableDataSource<Lead>();
  totalCount = 0;
  pageSize = 10;
  loading = false;

  stageColors: Record<string, string> = {
    New: '',
    Contacted: 'accent',
    Qualified: 'accent',
    ProposalSent: 'primary',
    Won: 'primary',
    Lost: 'warn',
  };

  constructor(
    private leadService: LeadService,
    private dialog: MatDialog,
    private snackBar: MatSnackBar,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.load(1);
  }

  load(page: number): void {
    this.loading = true;
    this.leadService.getAll(page, this.pageSize).subscribe({
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

  advanceStage(lead: Lead): void {
    this.leadService.advanceStage(lead.id).subscribe({
      next: () => {
        this.snackBar.open('Stage advanced', 'Close', { duration: 3000 });
        this.load(1);
      },
      error: () => this.snackBar.open('Failed to advance stage', 'Close', { duration: 3000 }),
    });
  }

  convert(lead: Lead): void {
    this.dialog
      .open(ConvertLeadDialogComponent, { width: '400px' })
      .afterClosed()
      .subscribe((accountManagerId: string | null) => {
        if (!accountManagerId) return;
        this.leadService.convertToClient(lead.id, { accountManagerId }).subscribe({
          next: () => {
            this.snackBar.open('Lead converted to client', 'Close', { duration: 3000 });
            this.load(1);
          },
          error: () => this.snackBar.open('Failed to convert lead', 'Close', { duration: 3000 }),
        });
      });
  }

  delete(lead: Lead): void {
    const data: ConfirmDialogData = {
      title: 'Delete Lead',
      message: `Delete lead "${lead.companyName}"?`,
    };
    this.dialog
      .open(ConfirmDialogComponent, { data })
      .afterClosed()
      .subscribe((confirmed) => {
        if (!confirmed) return;
        this.leadService.delete(lead.id).subscribe({
          next: () => {
            this.snackBar.open('Lead deleted', 'Close', { duration: 3000 });
            this.load(1);
          },
          error: () => this.snackBar.open('Failed to delete', 'Close', { duration: 3000 }),
        });
      });
  }
}
