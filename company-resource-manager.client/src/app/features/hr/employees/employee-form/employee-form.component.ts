import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatDialog } from '@angular/material/dialog';
import { EmployeeService } from '../../../../core/services/employee.service';
import {
  EmployeeListItem,
  CreateEmployeeRequest,
  UpdateEmployeeRequest,
} from '../../../../core/models/hr.models';
import { TempPasswordDialogComponent } from '../temp-password-dialog/temp-password-dialog.component';

@Component({
  selector: 'app-employee-form',
  standalone: false,
  templateUrl: './employee-form.component.html',
})
export class EmployeeFormComponent implements OnInit {
  form!: FormGroup;
  isEdit = false;
  employeeId: string | null = null;
  managers: EmployeeListItem[] = [];
  loading = false;
  readonly roles = ['Employee', 'HrManager', 'ProjectManager', 'AccountManager', 'Admin'];

  constructor(
    private fb: FormBuilder,
    private employeeService: EmployeeService,
    private route: ActivatedRoute,
    private router: Router,
    private snackBar: MatSnackBar,
    private dialog: MatDialog,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.employeeId = this.route.snapshot.paramMap.get('id');
    this.isEdit = !!this.employeeId;

    this.form = this.fb.group({
      firstName: ['', Validators.required],
      lastName: ['', Validators.required],
      personalEmail: ['', [Validators.required, Validators.email]],
      workEmail: ['', [Validators.required, Validators.email]],
      phone: ['', Validators.required],
      birthDate: ['', Validators.required],
      hireDate: ['', Validators.required],
      department: ['', Validators.required],
      jobTitle: ['', Validators.required],
      managerId: [null],
      seniorityYears: [0, [Validators.required, Validators.min(0)]],
      ...(this.isEdit ? { isActive: [true] } : { role: ['Employee', Validators.required] }),
    });

    this.employeeService.getAll(1, 100).subscribe((result) => {
      this.managers = result.items.filter((e) => e.id !== this.employeeId);
      this.cdr.detectChanges();
    });

    if (this.isEdit && this.employeeId) {
      this.employeeService.getById(this.employeeId).subscribe((emp) => {
        this.form.patchValue({
          ...emp,
          birthDate: emp.birthDate ? new Date(emp.birthDate) : null,
          hireDate: emp.hireDate ? new Date(emp.hireDate) : null,
        });
        this.cdr.detectChanges();
      });
    }
  }

  save(): void {
    if (this.form.invalid) return;
    this.loading = true;
    const v = this.form.value;
    const fmtDate = (d: Date | string): string =>
      d instanceof Date ? d.toISOString().substring(0, 10) : d;

    if (this.isEdit && this.employeeId) {
      const payload: UpdateEmployeeRequest = {
        firstName: v.firstName,
        lastName: v.lastName,
        personalEmail: v.personalEmail,
        workEmail: v.workEmail,
        phone: v.phone,
        department: v.department,
        jobTitle: v.jobTitle,
        managerId: v.managerId,
        seniorityYears: v.seniorityYears,
        isActive: v.isActive,
      };
      this.employeeService.update(this.employeeId, payload).subscribe({
        next: () => {
          this.snackBar.open('Employee updated', 'Close', { duration: 3000 });
          this.router.navigate(['/hr/employees']);
        },
        error: () => {
          this.snackBar.open('Failed to save employee', 'Close', { duration: 3000 });
          this.loading = false;
          this.cdr.detectChanges();
        },
      });
    } else {
      const payload: CreateEmployeeRequest = {
        firstName: v.firstName,
        lastName: v.lastName,
        personalEmail: v.personalEmail,
        workEmail: v.workEmail,
        phone: v.phone,
        birthDate: fmtDate(v.birthDate),
        hireDate: fmtDate(v.hireDate),
        department: v.department,
        jobTitle: v.jobTitle,
        managerId: v.managerId,
        seniorityYears: v.seniorityYears,
        role: v.role,
      };
      this.employeeService.create(payload).subscribe({
        next: (result) => {
          this.dialog
            .open(TempPasswordDialogComponent, {
              data: { password: result.temporaryPassword },
              disableClose: true,
              width: '420px',
            })
            .afterClosed()
            .subscribe(() => this.router.navigate(['/hr/employees']));
        },
        error: () => {
          this.snackBar.open('Failed to create employee', 'Close', { duration: 3000 });
          this.loading = false;
          this.cdr.detectChanges();
        },
      });
    }
  }
}
