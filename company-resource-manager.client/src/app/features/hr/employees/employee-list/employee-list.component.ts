import { Component, OnInit, ViewChild, ChangeDetectorRef } from '@angular/core';
import { MatTableDataSource } from '@angular/material/table';
import { MatPaginator } from '@angular/material/paginator';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { Router } from '@angular/router';
import { EmployeeService } from '../../../../core/services/employee.service';
import { EmployeeListItem } from '../../../../core/models/hr.models';
import {
  ConfirmDialogComponent,
  ConfirmDialogData,
} from '../../../../shared/components/confirm-dialog/confirm-dialog.component';

@Component({
  selector: 'app-employee-list',
  standalone: false,
  templateUrl: './employee-list.component.html',
})
export class EmployeeListComponent implements OnInit {
  @ViewChild(MatPaginator) paginator!: MatPaginator;

  displayedColumns = ['fullName', 'department', 'jobTitle', 'workEmail', 'isActive', 'actions'];
  dataSource = new MatTableDataSource<EmployeeListItem>();
  totalCount = 0;
  pageSize = 10;
  loading = false;

  constructor(
    private employeeService: EmployeeService,
    private dialog: MatDialog,
    private snackBar: MatSnackBar,
    private router: Router,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.load(1);
  }

  load(page: number): void {
    this.loading = true;
    this.employeeService.getAll(page, this.pageSize).subscribe({
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

  delete(employee: EmployeeListItem): void {
    const data: ConfirmDialogData = {
      title: 'Delete Employee',
      message: `Are you sure you want to delete ${employee.fullName}?`,
    };
    this.dialog
      .open(ConfirmDialogComponent, { data })
      .afterClosed()
      .subscribe((confirmed) => {
        if (!confirmed) return;
        this.employeeService.delete(employee.id).subscribe({
          next: () => {
            this.snackBar.open('Employee deleted', 'Close', { duration: 3000 });
            this.load(1);
          },
          error: () => this.snackBar.open('Failed to delete employee', 'Close', { duration: 3000 }),
        });
      });
  }
}
