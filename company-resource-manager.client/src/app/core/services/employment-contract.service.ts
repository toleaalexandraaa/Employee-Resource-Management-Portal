import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  EmploymentContract,
  CreateContractRequest,
  UpdateContractRequest,
} from '../models/hr.models';

@Injectable({ providedIn: 'root' })
export class EmploymentContractService {
  private readonly base = '/api/employment-contracts';

  constructor(private http: HttpClient) {}

  getByEmployee(employeeId: string): Observable<EmploymentContract[]> {
    return this.http.get<EmploymentContract[]>(`${this.base}/employee/${employeeId}`);
  }

  create(request: CreateContractRequest): Observable<EmploymentContract> {
    return this.http.post<EmploymentContract>(this.base, request);
  }

  update(id: string, request: UpdateContractRequest): Observable<EmploymentContract> {
    return this.http.put<EmploymentContract>(`${this.base}/${id}`, request);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }
}
