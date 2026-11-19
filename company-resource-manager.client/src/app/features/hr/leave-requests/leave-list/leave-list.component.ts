import { Component, OnInit, ViewChild, ChangeDetectorRef } from '@angular/core';
import { MatTableDataSource } from '@angular/material/table';
import { MatPaginator } from '@angular/material/paginator';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { LeaveRequestService } from '../../../../core/services/leave-request.service';
import { LeaveRequest } from '../../../../core/models/hr.models';
import {
  ConfirmDialogComponent,
  ConfirmDialogData,
} from '../../../../shared/components/confirm-dialog/confirm-dialog.component';
import { AuthService } from '../../../../core/services/auth.service';

@Component({
  selector: 'app-leave-list',
  standalone: false,
  templateUrl: './leave-list.component.html',
})
export class LeaveListComponent implements OnInit {
  @ViewChild(MatPaginator) paginator!: MatPaginator;

  displayedColumns = ['employeeName', 'leaveType', 'startDate', 'endDate', 'status', 'actions'];
  dataSource = new MatTableDataSource<LeaveRequest>();
  totalCount = 0;
  pageSize = 10;
  loading = false;

  statusColors: Record<string, string> = {
    Pending: 'accent',
    Approved: 'primary',
    Rejected: 'warn',
    Cancelled: '',
  };

  constructor(
    private leaveService: LeaveRequestService,
    private dialog: MatDialog,
    private snackBar: MatSnackBar,
    private cdr: ChangeDetectorRef,
    private auth: AuthService
  ) {}

  get canApproveReject(): boolean {
    return this.auth.hasAnyRole(['Admin', 'HrManager']);
  }

  get canCancel(): boolean {
    return !this.auth.hasAnyRole(['Admin', 'HrManager']);
  }

  ngOnInit(): void {
    this.load(1);
  }

  load(page: number): void {
    this.loading = true;
    this.leaveService.getAll(page, this.pageSize).subscribe({
      next: (result) => {
        this.dataSource.data = result.items;
        this.totalCount = result.totalCount;
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

  approve(leave: LeaveRequest): void {
    this.leaveService.approve(leave.id).subscribe({
      next: () => {
        this.snackBar.open('Leave request approved', 'Close', { duration: 3000 });
        this.load(1);
      },
      error: () => this.snackBar.open('Failed to approve', 'Close', { duration: 3000 }),
    });
  }

  reject(leave: LeaveRequest): void {
    this.leaveService.reject(leave.id, { reason: null }).subscribe({
      next: () => {
        this.snackBar.open('Leave request rejected', 'Close', { duration: 3000 });
        this.load(1);
      },
      error: () => this.snackBar.open('Failed to reject', 'Close', { duration: 3000 }),
    });
  }

  cancel(leave: LeaveRequest): void {
    const data: ConfirmDialogData = {
      title: 'Cancel Request',
      message: 'Cancel this leave request?',
      confirmLabel: 'Cancel Request',
    };
    this.dialog
      .open(ConfirmDialogComponent, { data })
      .afterClosed()
      .subscribe((confirmed) => {
        if (!confirmed) return;
        this.leaveService.cancel(leave.id).subscribe({
          next: () => {
            this.snackBar.open('Cancelled', 'Close', { duration: 3000 });
            this.load(1);
          },
          error: () => this.snackBar.open('Failed to cancel', 'Close', { duration: 3000 }),
        });
      });
  }
}
