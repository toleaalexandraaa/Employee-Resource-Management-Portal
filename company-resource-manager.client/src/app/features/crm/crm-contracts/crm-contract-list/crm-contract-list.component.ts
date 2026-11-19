import { Component, OnInit, ViewChild, ChangeDetectorRef } from '@angular/core';
import { MatTableDataSource } from '@angular/material/table';
import { MatPaginator } from '@angular/material/paginator';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { CrmContractService } from '../../../../core/services/crm-contract.service';
import { CrmContract } from '../../../../core/models/crm.models';
import {
  ConfirmDialogComponent,
  ConfirmDialogData,
} from '../../../../shared/components/confirm-dialog/confirm-dialog.component';

@Component({
  selector: 'app-crm-contract-list',
  standalone: false,
  templateUrl: './crm-contract-list.component.html',
})
export class CrmContractListComponent implements OnInit {
  @ViewChild(MatPaginator) paginator!: MatPaginator;
  displayedColumns = ['contractNumber', 'clientName', 'title', 'value', 'status', 'actions'];
  dataSource = new MatTableDataSource<CrmContract>();
  totalCount = 0;
  pageSize = 10;
  loading = false;

  statusColors: Record<string, string> = {
    Draft: 'accent',
    Signed: 'primary',
    Expired: 'warn',
    Cancelled: '',
  };

  constructor(
    private contractService: CrmContractService,
    private dialog: MatDialog,
    private snackBar: MatSnackBar,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.load(1);
  }

  load(page: number): void {
    this.loading = true;
    this.contractService.getAll(page, this.pageSize).subscribe({
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

  sign(contract: CrmContract): void {
    this.contractService.sign(contract.id).subscribe({
      next: () => {
        this.snackBar.open('Contract signed', 'Close', { duration: 3000 });
        this.load(1);
      },
      error: () => this.snackBar.open('Failed to sign contract', 'Close', { duration: 3000 }),
    });
  }

  delete(contract: CrmContract): void {
    const data: ConfirmDialogData = {
      title: 'Delete Contract',
      message: `Delete contract "${contract.contractNumber}"?`,
    };
    this.dialog
      .open(ConfirmDialogComponent, { data })
      .afterClosed()
      .subscribe((confirmed) => {
        if (!confirmed) return;
        this.contractService.delete(contract.id).subscribe({
          next: () => {
            this.snackBar.open('Contract deleted', 'Close', { duration: 3000 });
            this.load(1);
          },
          error: () => this.snackBar.open('Failed to delete', 'Close', { duration: 3000 }),
        });
      });
  }
}
