import { Component, Input, OnInit, OnChanges, ChangeDetectorRef } from '@angular/core';
import { CdkDragDrop, moveItemInArray, transferArrayItem } from '@angular/cdk/drag-drop';
import { FormControl } from '@angular/forms';
import { MatSnackBar } from '@angular/material/snack-bar';
import { KanbanService } from '../../../core/services/kanban.service';
import { TaskService } from '../../../core/services/task.service';
import { KanbanBoard, KanbanColumn, ProjectTask } from '../../../core/models/project.models';

@Component({
  selector: 'app-kanban-board',
  standalone: false,
  templateUrl: './kanban-board.component.html',
  styleUrls: ['./kanban-board.component.css'],
})
export class KanbanBoardComponent implements OnInit, OnChanges {
  @Input() projectId!: string;
  board: KanbanBoard | null = null;
  loading = false;
  addingTaskInColumn: string | null = null;
  newTaskTitleControl = new FormControl('');

  constructor(
    private kanbanService: KanbanService,
    private taskService: TaskService,
    private snackBar: MatSnackBar,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit(): void {
    this.loadBoard();
  }

  ngOnChanges(): void {
    if (this.projectId) this.loadBoard();
  }

  loadBoard(): void {
    if (!this.projectId) return;
    this.loading = true;
    this.kanbanService.getBoard(this.projectId).subscribe({
      next: (board) => {
        this.board = board;
        this.loading = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.loading = false;
        this.cdr.detectChanges();
      },
    });
  }

  get columnIds(): string[] {
    return this.board?.columns.map((c) => c.id) ?? [];
  }

  drop(event: CdkDragDrop<ProjectTask[]>, targetColumn: KanbanColumn): void {
    if (event.previousContainer === event.container) {
      moveItemInArray(event.container.data, event.previousIndex, event.currentIndex);
    } else {
      transferArrayItem(
        event.previousContainer.data,
        event.container.data,
        event.previousIndex,
        event.currentIndex
      );
    }
    const task = event.container.data[event.currentIndex];
    this.taskService
      .move(task.id, { targetColumnId: targetColumn.id, newSortOrder: event.currentIndex })
      .subscribe({
        error: () => this.snackBar.open('Failed to move task', 'Close', { duration: 2000 }),
      });
  }

  startAddTask(columnId: string): void {
    this.addingTaskInColumn = columnId;
    this.newTaskTitleControl.reset();
  }

  cancelAddTask(): void {
    this.addingTaskInColumn = null;
  }

  confirmAddTask(column: KanbanColumn): void {
    const title = this.newTaskTitleControl.value?.trim();
    if (!title) return;
    this.taskService
      .create({
        projectId: this.projectId,
        title,
        description: null,
        assignedToEmployeeId: null,
        priority: 'Medium',
        estimatedHours: 1,
        dueDate: null,
        kanbanColumnId: column.id,
      })
      .subscribe({
        next: (task) => {
          column.tasks.push(task);
          this.addingTaskInColumn = null;
          this.cdr.detectChanges();
        },
        error: () => this.snackBar.open('Failed to create task', 'Close', { duration: 2000 }),
      });
  }

  priorityColor(priority: string): string {
    const map: Record<string, string> = {
      Low: '#4caf50',
      Medium: '#ff9800',
      High: '#f44336',
      Critical: '#9c27b0',
    };
    return map[priority] ?? '#999';
  }
}
