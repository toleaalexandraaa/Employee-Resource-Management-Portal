import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  TimeEntry,
  CreateTimeEntryRequest,
  UpdateTimeEntryRequest,
} from '../models/project.models';
import { PagedResult } from '../models/shared.models';

@Injectable({ providedIn: 'root' })
export class TimeEntryService {
  private readonly base = '/api/time-entries';

  constructor(private http: HttpClient) {}

  getAll(page = 1, pageSize = 20, projectId?: string): Observable<PagedResult<TimeEntry>> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (projectId) params = params.set('projectId', projectId);
    return this.http.get<PagedResult<TimeEntry>>(this.base, { params });
  }

  create(request: CreateTimeEntryRequest): Observable<TimeEntry> {
    return this.http.post<TimeEntry>(this.base, request);
  }

  update(id: string, request: UpdateTimeEntryRequest): Observable<TimeEntry> {
    return this.http.put<TimeEntry>(`${this.base}/${id}`, request);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }

  getProjectSummary(projectId: string): Observable<{ totalHours: number }> {
    return this.http.get<{ totalHours: number }>(`${this.base}/summary?projectId=${projectId}`);
  }
}
