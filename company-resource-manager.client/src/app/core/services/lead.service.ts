import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  Lead,
  CreateLeadRequest,
  UpdateLeadRequest,
  ConvertLeadRequest,
} from '../models/crm.models';
import { PagedResult } from '../models/shared.models';

@Injectable({ providedIn: 'root' })
export class LeadService {
  private readonly base = '/api/leads';

  constructor(private http: HttpClient) {}

  getAll(page = 1, pageSize = 20): Observable<PagedResult<Lead>> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.http.get<PagedResult<Lead>>(this.base, { params });
  }

  getById(id: string): Observable<Lead> {
    return this.http.get<Lead>(`${this.base}/${id}`);
  }

  create(request: CreateLeadRequest): Observable<Lead> {
    return this.http.post<Lead>(this.base, request);
  }

  update(id: string, request: UpdateLeadRequest): Observable<Lead> {
    return this.http.put<Lead>(`${this.base}/${id}`, request);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }

  advanceStage(id: string): Observable<Lead> {
    return this.http.put<Lead>(`${this.base}/${id}/advance-stage`, {});
  }

  convertToClient(id: string, request: ConvertLeadRequest): Observable<{ clientId: string }> {
    return this.http.post<{ clientId: string }>(`${this.base}/${id}/convert`, request);
  }
}
