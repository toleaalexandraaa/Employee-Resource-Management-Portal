import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { PayrollRecord, GeneratePayrollRequest } from '../models/hr.models';
import { PagedResult } from '../models/shared.models';

@Injectable({ providedIn: 'root' })
export class PayrollService {
  private readonly base = '/api/payroll';

  constructor(private http: HttpClient) { }

  getAll(page = 1, pageSize = 20): Observable<PagedResult<PayrollRecord>> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.http.get<PagedResult<PayrollRecord>>(this.base, { params });
  }

  getById(id: string): Observable<PayrollRecord> {
    return this.http.get<PayrollRecord>(`${this.base}/${id}`);
  }

  generate(request: GeneratePayrollRequest): Observable<PayrollRecord> {
    return this.http.post<PayrollRecord>(`${this.base}/generate`, request);
  }

  approve(id: string): Observable<PayrollRecord> {
    return this.http.put<PayrollRecord>(`${this.base}/${id}/approve`, {});
  }

  markAsPaid(id: string): Observable<PayrollRecord> {
    return this.http.put<PayrollRecord>(`${this.base}/${id}/pay`, {});
  }
}
