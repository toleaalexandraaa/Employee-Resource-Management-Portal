import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { MatSnackBar } from '@angular/material/snack-bar';
import { ProjectService } from '../../../core/services/project.service';
import { ClientService } from '../../../core/services/client.service';
import { ProjectStatus } from '../../../core/models/project.models';
import { ClientListItem } from '../../../core/models/crm.models';

@Component({
  selector: 'app-project-form',
  standalone: false,
  templateUrl: './project-form.component.html',
})
export class ProjectFormComponent implements OnInit {
  form!: FormGroup;
  isEdit = false;
  projectId: string | null = null;
  loading = false;
  statuses: ProjectStatus[] = ['Planning', 'Active', 'OnHold', 'Completed', 'Cancelled'];
  clients: ClientListItem[] = [];

  constructor(
    private fb: FormBuilder,
    private projectService: ProjectService,
    private clientService: ClientService,
    private route: ActivatedRoute,
    private router: Router,
    private snackBar: MatSnackBar,
    private cdr: ChangeDetectorRef
  ) { }

  ngOnInit(): void {
    this.projectId = this.route.snapshot.paramMap.get('id');
    this.isEdit = !!this.projectId;

    this.form = this.fb.group({
      name: ['', Validators.required],
      description: [''],
      clientId: [null],
      status: ['Planning', Validators.required],
      startDate: [null],
      endDate: [null],
      estimatedBudget: [0],
      estimatedDurationDays: [0],
    });

    this.loadClients();

    if (this.isEdit && this.projectId) {
      this.projectService.getById(this.projectId).subscribe((p) => {
        this.form.patchValue({
          ...p,
          startDate: p.startDate ? new Date(p.startDate) : null,
          endDate: p.endDate ? new Date(p.endDate) : null,
        });
        this.cdr.detectChanges();
      });
    }
  }

  save(): void {
    if (this.form.invalid) return;
    this.loading = true;
    const value = this.form.value;
    const fmt = (d: Date | string | null) =>
      d instanceof Date ? d.toISOString().substring(0, 10) : d;
    const payload = { ...value, startDate: fmt(value.startDate), endDate: fmt(value.endDate) };

    const request$ =
      this.isEdit && this.projectId
        ? this.projectService.update(this.projectId, payload)
        : this.projectService.create(payload);

    request$.subscribe({
      next: () => {
        this.snackBar.open(`Project ${this.isEdit ? 'updated' : 'created'}`, 'Close', {
          duration: 3000,
        });
        this.router.navigate(['/projects']);
      },
      error: () => {
        this.snackBar.open('Failed to save project', 'Close', { duration: 3000 });
        this.loading = false;
        this.cdr.detectChanges();
      },
    });
  }

  loadClients(): void {
    this.clientService.getAll(1, 100).subscribe((r) => {
      this.clients = r.items;
      this.cdr.detectChanges();
    });
  }
}
