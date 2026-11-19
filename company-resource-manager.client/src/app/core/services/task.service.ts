import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  ProjectTask,
  CreateTaskRequest,
  UpdateTaskRequest,
  MoveTaskRequest,
  TaskStatus,
} from '../models/project.models';
import { PagedResult } from '../models/shared.models';

const PRIORITY_ORDER = ['Low', 'Medium', 'High', 'Critical'];
const STATUS_ORDER = ['Backlog', 'InProgress', 'InReview', 'Done', 'Cancelled'];

@Injectable({ providedIn: 'root' })
export class TaskService {
  private readonly base = '/api/tasks';

  constructor(private http: HttpClient) {}

  getAll(
    page = 1,
    pageSize = 50,
    projectId?: string,
    assigneeId?: string,
    status?: TaskStatus
  ): Observable<PagedResult<ProjectTask>> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (projectId) params = params.set('projectId', projectId);
    if (assigneeId) params = params.set('assigneeId', assigneeId);
    if (status) params = params.set('status', status);
    return this.http.get<PagedResult<ProjectTask>>(this.base, { params });
  }

  getById(id: string): Observable<ProjectTask> {
    return this.http.get<ProjectTask>(`${this.base}/${id}`);
  }

  create(request: CreateTaskRequest): Observable<ProjectTask> {
    return this.http.post<ProjectTask>(this.base, {
      ...request,
      priority: PRIORITY_ORDER.indexOf(request.priority),
    });
  }

  update(id: string, request: UpdateTaskRequest): Observable<ProjectTask> {
    return this.http.put<ProjectTask>(`${this.base}/${id}`, {
      ...request,
      priority: PRIORITY_ORDER.indexOf(request.priority),
      status: STATUS_ORDER.indexOf(request.status),
    });
  }

  move(id: string, request: MoveTaskRequest): Observable<ProjectTask> {
    return this.http.put<ProjectTask>(`${this.base}/${id}/move`, request);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }
}
