import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { FormBuilder, FormGroup, FormArray, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import { InvoiceService } from '../../../../core/services/invoice.service';

@Component({
  selector: 'app-invoice-form',
  standalone: false,
  templateUrl: './invoice-form.component.html',
})
export class InvoiceFormComponent implements OnInit {
  form!: FormGroup;
  loading = false;

  constructor(
    private fb: FormBuilder,
    private invoiceService: InvoiceService,
    private router: Router,
    private snackBar: MatSnackBar,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.form = this.fb.group({
      clientId: ['', Validators.required],
      contractId: [null],
      dueDate: ['', Validators.required],
      currency: ['USD'],
      taxRate: [0.19, [Validators.required, Validators.min(0), Validators.max(1)]],
      lineItems: this.fb.array([this.createLineItem()]),
    });
  }

  get lineItems(): FormArray {
    return this.form.get('lineItems') as FormArray;
  }

  createLineItem(): FormGroup {
    return this.fb.group({
      description: ['', Validators.required],
      quantity: [1, [Validators.required, Validators.min(0.01)]],
      unitPrice: [0, [Validators.required, Validators.min(0)]],
    });
  }

  addLineItem(): void {
    this.lineItems.push(this.createLineItem());
  }

  removeLineItem(index: number): void {
    if (this.lineItems.length > 1) this.lineItems.removeAt(index);
  }

  lineTotal(index: number): number {
    const item = this.lineItems.at(index).value;
    return (item.quantity || 0) * (item.unitPrice || 0);
  }

  get subTotal(): number {
    return this.lineItems.controls.reduce((sum, _, i) => sum + this.lineTotal(i), 0);
  }

  save(): void {
    if (this.form.invalid) return;
    this.loading = true;
    const value = this.form.value;
    const fmt = (d: Date | string) => (d instanceof Date ? d.toISOString().substring(0, 10) : d);
    this.invoiceService.create({ ...value, dueDate: fmt(value.dueDate) }).subscribe({
      next: () => {
        this.snackBar.open('Invoice created', 'Close', { duration: 3000 });
        this.router.navigate(['/crm/invoices']);
      },
      error: () => {
        this.snackBar.open('Failed to create invoice', 'Close', { duration: 3000 });
        this.loading = false;
        this.cdr.detectChanges();
      },
    });
  }
}
