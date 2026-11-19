import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  Project,
  ProjectListItem,
  CreateProjectRequest,
  UpdateProjectRequest,
  ProjectMember,
  AddProjectMemberRequest,
  GanttTask,
} from '../models/project.models';
import { PagedResult } from '../models/shared.models';

@Injectable({ providedIn: 'root' })
export class ProjectService {
  private readonly base = '/api/projects';

  constructor(private http: HttpClient) { }

  getAll(page = 1, pageSize = 20): Observable<PagedResult<ProjectListItem>> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.http.get<PagedResult<ProjectListItem>>(this.base, { params });
  }

  getById(id: string): Observable<Project> {
    return this.http.get<Project>(`${this.base}/${id}`);
  }

  create(request: CreateProjectRequest): Observable<Project> {
    return this.http.post<Project>(this.base, request);
  }

  update(id: string, request: UpdateProjectRequest): Observable<Project> {
    return this.http.put<Project>(`${this.base}/${id}`, request);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }

  getMembers(id: string): Observable<ProjectMember[]> {
    return this.http.get<ProjectMember[]>(`${this.base}/${id}/members`);
  }

  addMember(id: string, request: AddProjectMemberRequest): Observable<ProjectMember> {
    return this.http.post<ProjectMember>(`${this.base}/${id}/members`, request);
  }

  removeMember(id: string, memberId: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}/members/${memberId}`);
  }

  getGantt(id: string): Observable<GanttTask[]> {
    return this.http.get<GanttTask[]>(`${this.base}/${id}/gantt`);
  }
}
