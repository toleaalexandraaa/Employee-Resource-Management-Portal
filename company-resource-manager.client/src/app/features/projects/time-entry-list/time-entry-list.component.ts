import { Component, Input, OnInit, OnChanges, ChangeDetectorRef } from '@angular/core';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';
import { TimeEntryService } from '../../../core/services/time-entry.service';
import { TaskService } from '../../../core/services/task.service';
import { ProjectTask, TimeEntry } from '../../../core/models/project.models';

@Component({
  selector: 'app-time-entry-list',
  standalone: false,
  templateUrl: './time-entry-list.component.html',
})
export class TimeEntryListComponent implements OnInit, OnChanges {
  @Input() projectId!: string;
  entries: TimeEntry[] = [];
  totalCount = 0;
  tasks: ProjectTask[] = [];
  displayedColumns = ['date', 'employeeName', 'taskTitle', 'hoursLogged', 'description', 'actions'];
  showForm = false;
  form!: FormGroup;
  loading = false;

  constructor(
    private timeEntryService: TimeEntryService,
    private taskService: TaskService,
    private fb: FormBuilder,
    private snackBar: MatSnackBar,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.form = this.fb.group({
      date: ['', Validators.required],
      hoursLogged: [1, [Validators.required, Validators.min(0.25), Validators.max(24)]],
      description: [''],
      taskId: [null],
    });
    this.load();
    this.loadTasks();
  }

  ngOnChanges(): void {
    if (this.projectId) {
      this.load();
      this.loadTasks();
    }
  }

  loadTasks(): void {
    if (!this.projectId) return;
    this.taskService.getAll(1, 100, this.projectId).subscribe({
      next: (r) => {
        this.tasks = r.items;
        this.cdr.detectChanges();
      },
    });
  }

  load(): void {
    if (!this.projectId) return;
    this.loading = true;
    this.timeEntryService.getAll(1, 50, this.projectId).subscribe({
      next: (r) => {
        this.entries = r.items;
        this.totalCount = r.totalCount;
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.loading = false;
        this.cdr.detectChanges();
      },
    });
  }

  toggleForm(): void {
    this.showForm = !this.showForm;
    if (this.showForm) {
      this.loadTasks();
    }
  }

  save(): void {
    if (this.form.invalid) return;
    const value = this.form.value;
    const fmt = (d: Date | string) =>
      d instanceof Date ? d.toISOString().substring(0, 10) : d;
    this.timeEntryService
      .create({ ...value, projectId: this.projectId, date: fmt(value.date) })
      .subscribe({
        next: () => {
          this.snackBar.open('Time logged', 'Close', { duration: 3000 });
          this.showForm = false;
          this.form.reset({ hoursLogged: 1 });
          this.load();
        },
        error: () => this.snackBar.open('Failed to log time', 'Close', { duration: 3000 }),
      });
  }

  delete(entry: TimeEntry): void {
    this.timeEntryService.delete(entry.id).subscribe({
      next: () => {
        this.snackBar.open('Entry deleted', 'Close', { duration: 3000 });
        this.load();
      },
    });
  }
}
