import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import { CrmContractService } from '../../../../core/services/crm-contract.service';

@Component({
  selector: 'app-crm-contract-form',
  standalone: false,
  templateUrl: './crm-contract-form.component.html',
})
export class CrmContractFormComponent implements OnInit {
  form!: FormGroup;
  loading = false;

  constructor(
    private fb: FormBuilder,
    private contractService: CrmContractService,
    private router: Router,
    private snackBar: MatSnackBar,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.form = this.fb.group({
      clientId: ['', Validators.required],
      title: ['', Validators.required],
      value: [0, [Validators.required, Validators.min(0)]],
      currency: ['USD'],
      startDate: [null],
      endDate: [null],
    });
  }

  save(): void {
    if (this.form.invalid) return;
    this.loading = true;
    const value = this.form.value;
    const fmt = (d: Date | string | null) =>
      d instanceof Date ? d.toISOString().substring(0, 10) : d;
    this.contractService
      .create({ ...value, startDate: fmt(value.startDate), endDate: fmt(value.endDate) })
      .subscribe({
        next: () => {
          this.snackBar.open('Contract created', 'Close', { duration: 3000 });
          this.router.navigate(['/crm/contracts']);
        },
        error: () => {
          this.snackBar.open('Failed to create contract', 'Close', { duration: 3000 });
          this.loading = false;
          this.cdr.detectChanges();
        },
      });
  }
}
