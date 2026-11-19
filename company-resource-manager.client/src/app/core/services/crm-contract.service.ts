import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  CrmContract,
  CreateCrmContractRequest,
  UpdateCrmContractRequest,
} from '../models/crm.models';
import { PagedResult } from '../models/shared.models';

@Injectable({ providedIn: 'root' })
export class CrmContractService {
  private readonly base = '/api/crm-contracts';

  constructor(private http: HttpClient) {}

  getAll(page = 1, pageSize = 20): Observable<PagedResult<CrmContract>> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.http.get<PagedResult<CrmContract>>(this.base, { params });
  }

  getById(id: string): Observable<CrmContract> {
    return this.http.get<CrmContract>(`${this.base}/${id}`);
  }

  create(request: CreateCrmContractRequest): Observable<CrmContract> {
    return this.http.post<CrmContract>(this.base, request);
  }

  update(id: string, request: UpdateCrmContractRequest): Observable<CrmContract> {
    return this.http.put<CrmContract>(`${this.base}/${id}`, request);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }

  sign(id: string): Observable<CrmContract> {
    return this.http.put<CrmContract>(`${this.base}/${id}/sign`, {});
  }
}
