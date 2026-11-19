import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { MatDialogRef } from '@angular/material/dialog';
import { EmployeeService } from '../../../../core/services/employee.service';
import { EmployeeListItem } from '../../../../core/models/hr.models';

@Component({
  selector: 'app-convert-lead-dialog',
  standalone: false,
  templateUrl: './convert-lead-dialog.component.html',
})
export class ConvertLeadDialogComponent implements OnInit {
  employees: EmployeeListItem[] = [];
  selectedAccountManagerId: string | null = null;
  loading = true;

  constructor(
    private dialogRef: MatDialogRef<ConvertLeadDialogComponent>,
    private employeeService: EmployeeService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.employeeService.getAll(1, 200).subscribe({
      next: (r) => {
        this.employees = r.items;
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.loading = false;
        this.cdr.detectChanges();
      },
    });
  }

  confirm(): void {
    if (this.selectedAccountManagerId) {
      this.dialogRef.close(this.selectedAccountManagerId);
    }
  }

  cancel(): void {
    this.dialogRef.close(null);
  }
}
