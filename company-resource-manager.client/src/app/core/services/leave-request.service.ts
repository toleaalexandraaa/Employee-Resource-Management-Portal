import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  LeaveRequest,
  CreateLeaveRequest,
  ReviewLeaveRequest,
  LeaveBalance,
} from '../models/hr.models';
import { PagedResult } from '../models/shared.models';

@Injectable({ providedIn: 'root' })
export class LeaveRequestService {
  private readonly base = '/api/leave-requests';

  constructor(private http: HttpClient) {}

  getAll(page = 1, pageSize = 20, employeeId?: string): Observable<PagedResult<LeaveRequest>> {
    let params = new HttpParams().set('page', page).set('pageSize', pageSize);
    if (employeeId) params = params.set('employeeId', employeeId);
    return this.http.get<PagedResult<LeaveRequest>>(this.base, { params });
  }

  getById(id: string): Observable<LeaveRequest> {
    return this.http.get<LeaveRequest>(`${this.base}/${id}`);
  }

  submit(request: CreateLeaveRequest): Observable<LeaveRequest> {
    return this.http.post<LeaveRequest>(this.base, request);
  }

  approve(id: string): Observable<LeaveRequest> {
    return this.http.put<LeaveRequest>(`${this.base}/${id}/approve`, {});
  }

  reject(id: string, request: ReviewLeaveRequest): Observable<LeaveRequest> {
    return this.http.put<LeaveRequest>(`${this.base}/${id}/reject`, request);
  }

  cancel(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }

  getCalendar(from: string, to: string): Observable<LeaveRequest[]> {
    const params = new HttpParams().set('from', from).set('to', to);
    return this.http.get<LeaveRequest[]>(`${this.base}/calendar`, { params });
  }

  getBalances(employeeId: string, year: number): Observable<LeaveBalance[]> {
    return this.http.get<LeaveBalance[]>(`${this.base}/balances/${employeeId}?year=${year}`);
  }
}
