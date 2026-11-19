import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  Client,
  ClientListItem,
  CreateClientRequest,
  UpdateClientRequest,
  ClientContact,
  CreateClientContactRequest,
} from '../models/crm.models';
import { PagedResult } from '../models/shared.models';

@Injectable({ providedIn: 'root' })
export class ClientService {
  private readonly base = '/api/clients';

  constructor(private http: HttpClient) {}

  getAll(page = 1, pageSize = 20): Observable<PagedResult<ClientListItem>> {
    const params = new HttpParams().set('page', page).set('pageSize', pageSize);
    return this.http.get<PagedResult<ClientListItem>>(this.base, { params });
  }

  getById(id: string): Observable<Client> {
    return this.http.get<Client>(`${this.base}/${id}`);
  }

  create(request: CreateClientRequest): Observable<Client> {
    return this.http.post<Client>(this.base, request);
  }

  update(id: string, request: UpdateClientRequest): Observable<Client> {
    return this.http.put<Client>(`${this.base}/${id}`, request);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${id}`);
  }

  addContact(clientId: string, request: CreateClientContactRequest): Observable<ClientContact> {
    return this.http.post<ClientContact>(`${this.base}/${clientId}/contacts`, request);
  }

  removeContact(clientId: string, contactId: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/${clientId}/contacts/${contactId}`);
  }
}
