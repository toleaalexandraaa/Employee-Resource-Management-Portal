import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ClientService } from '../../../../core/services/client.service';
import { ClientStatus } from '../../../../core/models/crm.models';

@Component({
  selector: 'app-client-form',
  standalone: false,
  templateUrl: './client-form.component.html',
})
export class ClientFormComponent implements OnInit {
  form!: FormGroup;
  isEdit = false;
  clientId: string | null = null;
  loading = false;
  statuses: ClientStatus[] = ['Active', 'Inactive', 'Prospect'];

  constructor(
    private fb: FormBuilder,
    private clientService: ClientService,
    private route: ActivatedRoute,
    private router: Router,
    private snackBar: MatSnackBar,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.clientId = this.route.snapshot.paramMap.get('id');
    this.isEdit = !!this.clientId;
    this.form = this.fb.group({
      companyName: ['', Validators.required],
      industry: [''],
      website: [''],
      status: ['Active', Validators.required],
      accountManagerEmployeeId: [null],
    });
    if (this.isEdit && this.clientId) {
      this.clientService.getById(this.clientId).subscribe((c) => {
        this.form.patchValue(c);
        this.cdr.detectChanges();
      });
    }
  }

  save(): void {
    if (this.form.invalid) return;
    this.loading = true;
    const request$ =
      this.isEdit && this.clientId
        ? this.clientService.update(this.clientId, this.form.value)
        : this.clientService.create(this.form.value);
    request$.subscribe({
      next: () => {
        this.snackBar.open(`Client ${this.isEdit ? 'updated' : 'created'}`, 'Close', {
          duration: 3000,
        });
        this.router.navigate(['/crm/clients']);
      },
      error: () => {
        this.snackBar.open('Failed to save', 'Close', { duration: 3000 });
        this.loading = false;
        this.cdr.detectChanges();
      },
    });
  }
}
