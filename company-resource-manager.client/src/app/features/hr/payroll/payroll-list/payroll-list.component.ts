import { Component, OnInit, ViewChild, ChangeDetectorRef } from '@angular/core';
import { MatTableDataSource } from '@angular/material/table';
import { MatPaginator } from '@angular/material/paginator';
import { MatSnackBar } from '@angular/material/snack-bar';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { PayrollService } from '../../../../core/services/payroll.service';
import { EmployeeService } from '../../../../core/services/employee.service';
import { PayrollRecord, EmployeeListItem } from '../../../../core/models/hr.models';

@Component({
  selector: 'app-payroll-list',
  standalone: false,
  templateUrl: './payroll-list.component.html',
})
export class PayrollListComponent implements OnInit {
  @ViewChild(MatPaginator) paginator!: MatPaginator;

  displayedColumns = ['employeeName', 'period', 'grossSalary', 'netSalary', 'status', 'actions'];
  dataSource = new MatTableDataSource<PayrollRecord>();
  totalCount = 0;
  pageSize = 10;
  loading = false;
  showGenerateForm = false;
  generateForm!: FormGroup;
  employees: EmployeeListItem[] = [];

  months = [
    { value: 1, label: 'January' },
    { value: 2, label: 'February' },
    { value: 3, label: 'March' },
    { value: 4, label: 'April' },
    { value: 5, label: 'May' },
    { value: 6, label: 'June' },
    { value: 7, label: 'July' },
    { value: 8, label: 'August' },
    { value: 9, label: 'September' },
    { value: 10, label: 'October' },
    { value: 11, label: 'November' },
    { value: 12, label: 'December' },
  ];

  currentYear = new Date().getFullYear();

  constructor(
    private payrollService: PayrollService,
    private employeeService: EmployeeService,
    private fb: FormBuilder,
    private snackBar: MatSnackBar,
    private cdr: ChangeDetectorRef
  ) { }

  ngOnInit(): void {
    this.load(1);
    this.generateForm = this.fb.group({
      employeeId: ['', Validators.required],
      month: [new Date().getMonth() + 1, Validators.required],
      year: [this.currentYear, [Validators.required, Validators.min(2000)]],
      bonusAmount: [0, [Validators.required, Validators.min(0)]],
      mealTicketCount: [0, [Validators.required, Validators.min(0)]],
      mealTicketValue: [0, [Validators.required, Validators.min(0)]],
    });
    this.employeeService.getAll(1, 100).subscribe((r) => {
      this.employees = r.items;
      this.cdr.detectChanges();
    });
  }

  load(page: number): void {
    this.loading = true;
    this.payrollService.getAll(page, this.pageSize).subscribe({
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

  approve(record: PayrollRecord): void {
    this.payrollService.approve(record.id).subscribe({
      next: () => {
        this.snackBar.open('Payroll approved', 'Close', { duration: 3000 });
        this.load(1);
      },
      error: () => this.snackBar.open('Failed to approve', 'Close', { duration: 3000 }),
    });
  }

  generate(): void {
    if (this.generateForm.invalid) return;
    this.payrollService.generate(this.generateForm.value).subscribe({
      next: () => {
        this.snackBar.open('Payroll generated', 'Close', { duration: 3000 });
        this.showGenerateForm = false;
        this.load(1);
      },
      error: () => this.snackBar.open('Failed to generate payroll', 'Close', { duration: 3000 }),
    });
  }

  markAsPaid(record: PayrollRecord): void {
    this.payrollService.markAsPaid(record.id).subscribe({
      next: () => {
        this.snackBar.open('Payroll marked as paid', 'Close', { duration: 3000 });
        this.load(1);
      },
      error: () => this.snackBar.open('Failed to mark as paid', 'Close', { duration: 3000 }),
    });
  }
}
