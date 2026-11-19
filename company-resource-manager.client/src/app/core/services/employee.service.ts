import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  Employee,
  EmployeeListItem,
  CreateEmployeeRequest,
  CreateEmployeeResult,
  UpdateEmployeeRequest,
} from '../models/hr.models';
import { PagedResult } from '../models/shared.models';

@Injectable({ providedIn: 'root' })
export class EmployeeService {
  private readonly base = '/api/employees';

  constructor(private http: HttpClient) {}

  getAll(page = 1, pageSize = 20): Observable<PagedResult<EmployeeListItem>> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.http.get<PagedResult<EmployeeListItem>>(this.base, { params });
  }

  getById(id: string): Observable<Employee> {
    return this.http.get<Employee>(`${this.base}/${id}`);
  }

  create(request: CreateEmployeeRequest): Observable<CreateEmployeeResult> {
    return this.http.post<CreateEmployeeResult>(this.base, request);
  }

  update(id: string, request: UpdateEmployeeRequest): Observable<Employee> {
    return this.http.put<Employee>(`${this.base}/${id}`, request);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }
}
