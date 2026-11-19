import { Component, OnInit, ChangeDetectorRef, ViewChild } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatTabChangeEvent } from '@angular/material/tabs';
import { ProjectService } from '../../../core/services/project.service';
import { TaskService } from '../../../core/services/task.service';
import { EmployeeService } from '../../../core/services/employee.service';
import { Project, ProjectTask, TaskPriority, ProjectMember } from '../../../core/models/project.models';
import { KanbanBoardComponent } from '../kanban-board/kanban-board.component';
import { EmployeeListItem } from '../../../core/models/hr.models';
import { AnalyticsService } from '../../../core/services/analytics.service';
import { ProjectRiskScore } from '../../../core/models/analytics.models';
import { PagedResult } from '../../../core/models/shared.models';

@Component({
  selector: 'app-project-detail',
  standalone: false,
  templateUrl: './project-detail.component.html',
})
export class ProjectDetailComponent implements OnInit {
  @ViewChild('kanbanBoard') kanbanBoard?: KanbanBoardComponent;
  projectId!: string;
  project: Project | null = null;
  tasks: ProjectTask[] = [];
  employees: EmployeeListItem[] = [];
  taskColumns = ['title', 'assignedToEmployeeName', 'priority', 'status', 'dueDate', 'actions'];
  showTaskForm = false;
  taskForm!: FormGroup;
  priorities: TaskPriority[] = ['Low', 'Medium', 'High', 'Critical'];

  members: ProjectMember[] = [];
  memberColumns = ['employeeName', 'role', 'joinDate', 'actions'];
  memberForm!: FormGroup;
  showMemberForm = false;

  riskScore: ProjectRiskScore | null = null;
  riskColors: Record<string, string> = { OnTrack: 'primary', Watch: 'accent', AtRisk: 'warn' };
  riskLabels: Record<string, string> = { OnTrack: 'On Track', Watch: 'Watch', AtRisk: 'At Risk' };

  priorityColors: Record<string, string> = {
    Low: '',
    Medium: 'accent',
    High: 'warn',
    Critical: 'warn',
  };

  constructor(
    private route: ActivatedRoute,
    private projectService: ProjectService,
    private taskService: TaskService,
    private employeeService: EmployeeService,
    private analytics: AnalyticsService,
    private fb: FormBuilder,
    private snackBar: MatSnackBar,
    private cdr: ChangeDetectorRef
  ) { }

  ngOnInit(): void {
    this.projectId = this.route.snapshot.paramMap.get('id')!;
    this.projectService.getById(this.projectId).subscribe((p: Project) => {
      this.project = p;
      this.cdr.detectChanges();
    });
    this.loadTasks();
    this.loadMembers();
    this.employeeService.getAll(1, 100).subscribe((r: PagedResult<EmployeeListItem>) => {
      this.employees = r.items;
      this.cdr.detectChanges();
    });
    this.analytics.getProjectRisks().subscribe({
      next: (r: any) => {
        if (r.hasSufficientData) {
          this.riskScore = r.projects.find((p: any) => p.projectId === this.projectId) ?? null;
          this.cdr.detectChanges();
        }
      },
      error: () => { }, // 403 for non-Admin/ProjectManager roles — ignore silently
    });

    this.taskForm = this.fb.group({
      title: ['', Validators.required],
      description: [''],
      assignedToEmployeeId: [null],
      priority: ['Medium'],
      estimatedHours: [1, [Validators.required, Validators.min(0.25)]],
      dueDate: [null],
    });

    this.memberForm = this.fb.group({
      employeeId: [null, Validators.required],
      role: ['Developer', Validators.required],
      allocatedHoursPerWeek: [40, [Validators.required, Validators.min(1)]],
      joinDate: [new Date(), Validators.required],
    });
  }

  loadTasks(): void {
    this.taskService.getAll(1, 100, this.projectId).subscribe((r: PagedResult<ProjectTask>) => {
      this.tasks = r.items;
      this.cdr.detectChanges();
    });
  }

  saveTask(): void {
    if (this.taskForm.invalid) return;
    const value = this.taskForm.value;
    const fmt = (d: Date | string | null) =>
      d instanceof Date ? d.toISOString().substring(0, 10) : d;
    this.taskService
      .create({
        ...value,
        projectId: this.projectId,
        kanbanColumnId: null,
        dueDate: fmt(value.dueDate),
      })
      .subscribe({
        next: () => {
          this.snackBar.open('Task created', 'Close', { duration: 3000 });
          this.showTaskForm = false;
          this.taskForm.reset({ priority: 'Medium', estimatedHours: 1 });
          this.loadTasks();
        },
        error: () => this.snackBar.open('Failed to create task', 'Close', { duration: 3000 }),
      });
  }

  deleteTask(task: ProjectTask): void {
    this.taskService.delete(task.id).subscribe({
      next: () => {
        this.snackBar.open('Task deleted', 'Close', { duration: 3000 });
        this.loadTasks();
      },
    });
  }

  loadMembers(): void {
    this.projectService.getMembers(this.projectId).subscribe((m: ProjectMember[]) => {
      this.members = m;
      this.cdr.detectChanges();
    });
  }

  saveMember(): void {
    if (this.memberForm.invalid) return;
    const value = this.memberForm.value;
    const fmt = (d: Date | string) =>
      d instanceof Date ? d.toISOString().substring(0, 10) : d;

    this.projectService.addMember(this.projectId, {
      ...value,
      joinDate: fmt(value.joinDate),
      leaveDate: null
    }).subscribe({
      next: () => {
        this.snackBar.open('Member added', 'Close', { duration: 3000 });
        this.showMemberForm = false;
        this.memberForm.reset({ role: 'Developer', allocatedHoursPerWeek: 40, joinDate: new Date() });
        this.loadMembers();
      },
      error: () => this.snackBar.open('Failed to add member', 'Close', { duration: 3000 })
    });
  }

  deleteMember(member: ProjectMember): void {
    console.log(member);

    this.projectService.removeMember(this.projectId, member.id).subscribe({
      next: () => {
        this.snackBar.open('Member removed', 'Close', { duration: 3000 });
        this.loadMembers();
      },
      error: () => this.snackBar.open('Failed to remove member', 'Close', { duration: 3000 })
    });
  }

  onTabChange(event: MatTabChangeEvent): void {
    if (event.index === 0) {
      this.kanbanBoard?.loadBoard();
    }
  }
}
