import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import { LeaveRequestService } from '../../../../core/services/leave-request.service';

@Component({
  selector: 'app-leave-form',
  standalone: false,
  templateUrl: './leave-form.component.html',
})
export class LeaveFormComponent implements OnInit {
  form!: FormGroup;
  loading = false;
  leaveTypes = ['Annual', 'Medical', 'Unpaid', 'Parental', 'Other'];

  constructor(
    private fb: FormBuilder,
    private leaveService: LeaveRequestService,
    private router: Router,
    private snackBar: MatSnackBar,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.form = this.fb.group({
      leaveType: ['', Validators.required],
      startDate: ['', Validators.required],
      endDate: ['', Validators.required],
      reason: [''],
    });
  }

  private countWorkingDays(start: Date, end: Date): number {
    let count = 0;
    const cur = new Date(start);
    cur.setHours(0, 0, 0, 0);
    const last = new Date(end);
    last.setHours(0, 0, 0, 0);
    while (cur <= last) {
      const day = cur.getDay();
      if (day !== 0 && day !== 6) count++;
      cur.setDate(cur.getDate() + 1);
    }
    return count;
  }

  save(): void {
    if (this.form.invalid) return;
    this.loading = true;
    const value = this.form.value;
    const toDate = (d: Date | string) => (d instanceof Date ? d : new Date(d));
    const formatDate = (d: Date | string) =>
      d instanceof Date ? d.toISOString().substring(0, 10) : d;
    const workingDaysCount = this.countWorkingDays(toDate(value.startDate), toDate(value.endDate));
    this.leaveService
      .submit({
        ...value,
        startDate: formatDate(value.startDate),
        endDate: formatDate(value.endDate),
        workingDaysCount,
      })
      .subscribe({
        next: () => {
          this.snackBar.open('Leave request submitted', 'Close', { duration: 3000 });
          this.router.navigate(['/hr/leave-requests']);
        },
        error: () => {
          this.snackBar.open('Failed to submit', 'Close', { duration: 3000 });
          this.loading = false;
          this.cdr.detectChanges();
        },
      });
  }
}
