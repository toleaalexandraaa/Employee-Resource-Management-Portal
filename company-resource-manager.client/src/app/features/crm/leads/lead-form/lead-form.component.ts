import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import { LeadService } from '../../../../core/services/lead.service';

@Component({
  selector: 'app-lead-form',
  standalone: false,
  templateUrl: './lead-form.component.html',
})
export class LeadFormComponent implements OnInit {
  form!: FormGroup;
  isEdit = false;
  leadId: string | null = null;
  loading = false;
  stages = ['New', 'Contacted', 'Qualified', 'ProposalSent', 'Won', 'Lost'];

  constructor(
    private fb: FormBuilder,
    private leadService: LeadService,
    private route: ActivatedRoute,
    private router: Router,
    private snackBar: MatSnackBar,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.leadId = this.route.snapshot.paramMap.get('id');
    this.isEdit = !!this.leadId;
    this.form = this.fb.group({
      companyName: ['', Validators.required],
      contactName: [''],
      contactEmail: ['', Validators.email],
      contactPhone: [''],
      estimatedValue: [null],
      assignedToEmployeeId: [null],
      notes: [''],
      stage: ['New'],
    });
    if (this.isEdit && this.leadId) {
      this.leadService.getById(this.leadId).subscribe((l) => {
        this.form.patchValue(l);
        this.cdr.detectChanges();
      });
    }
  }

  save(): void {
    if (this.form.invalid) return;
    this.loading = true;
    const request$ =
      this.isEdit && this.leadId
        ? this.leadService.update(this.leadId, this.form.value)
        : this.leadService.create(this.form.value);
    request$.subscribe({
      next: () => {
        this.snackBar.open(`Lead ${this.isEdit ? 'updated' : 'created'}`, 'Close', {
          duration: 3000,
        });
        this.router.navigate(['/crm/leads']);
      },
      error: () => {
        this.snackBar.open('Failed to save lead', 'Close', { duration: 3000 });
        this.loading = false;
        this.cdr.detectChanges();
      },
    });
  }
}
