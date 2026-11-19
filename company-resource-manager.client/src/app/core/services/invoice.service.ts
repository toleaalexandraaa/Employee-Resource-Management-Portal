import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  Invoice,
  InvoiceListItem,
  CreateInvoiceRequest,
  GenerateInvoiceFromTimeEntriesRequest,
  RecordPaymentRequest,
} from '../models/crm.models';
import { PagedResult } from '../models/shared.models';

@Injectable({ providedIn: 'root' })
export class InvoiceService {
  private readonly base = '/api/invoices';

  constructor(private http: HttpClient) {}

  getAll(page = 1, pageSize = 20): Observable<PagedResult<InvoiceListItem>> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.http.get<PagedResult<InvoiceListItem>>(this.base, { params });
  }

  getById(id: string): Observable<Invoice> {
    return this.http.get<Invoice>(`${this.base}/${id}`);
  }

  create(request: CreateInvoiceRequest): Observable<Invoice> {
    return this.http.post<Invoice>(this.base, request);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }

  generateFromTimeEntries(request: GenerateInvoiceFromTimeEntriesRequest): Observable<Invoice> {
    return this.http.post<Invoice>(`${this.base}/generate-from-timeentries`, request);
  }

  send(id: string): Observable<Invoice> {
    return this.http.put<Invoice>(`${this.base}/${id}/send`, {});
  }

  recordPayment(id: string, request: RecordPaymentRequest): Observable<Invoice> {
    return this.http.post<Invoice>(`${this.base}/${id}/payments`, request);
  }
}
