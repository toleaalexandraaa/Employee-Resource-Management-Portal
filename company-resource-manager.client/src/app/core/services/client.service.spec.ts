import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting, HttpTestingController } from '@angular/common/http/testing';
import { ClientService } from './client.service';
import { Client, ClientContact, ClientListItem } from '../models/crm.models';
import { PagedResult } from '../models/shared.models';

const mockClientItem: ClientListItem = {
  id: 'c1',
  companyName: 'Acme Corp',
  industry: 'Tech',
  status: 'Active',
  accountManagerName: 'Jane Doe',
};

const mockClient: Client = {
  id: 'c1',
  companyName: 'Acme Corp',
  industry: 'Tech',
  website: 'https://acme.com',
  status: 'Active',
  accountManagerEmployeeId: 'e1',
  accountManagerName: 'Jane Doe',
  contacts: [],
};

describe('ClientService', () => {
  let service: ClientService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [ClientService, provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(ClientService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  describe('getAll', () => {
    it('should GET /api/clients with default page params', () => {
      const pagedResult: PagedResult<ClientListItem> = {
        items: [mockClientItem],
        totalCount: 1,
        page: 1,
        pageSize: 20,
      };

      service.getAll().subscribe((r) => expect(r).toEqual(pagedResult));

      const req = httpMock.expectOne((r) => r.url === '/api/clients');
      expect(req.request.method).toBe('GET');
      expect(req.request.params.get('page')).toBe('1');
      expect(req.request.params.get('pageSize')).toBe('20');
      req.flush(pagedResult);
    });

    it('should pass custom page and pageSize', () => {
      service.getAll(3, 5).subscribe();

      const req = httpMock.expectOne((r) => r.url === '/api/clients');
      expect(req.request.params.get('page')).toBe('3');
      expect(req.request.params.get('pageSize')).toBe('5');
      req.flush({ items: [], totalCount: 0, page: 3, pageSize: 5 });
    });
  });

  describe('getById', () => {
    it('should GET /api/clients/:id', () => {
      service.getById('c1').subscribe((c) => expect(c).toEqual(mockClient));

      const req = httpMock.expectOne('/api/clients/c1');
      expect(req.request.method).toBe('GET');
      req.flush(mockClient);
    });
  });

  describe('create', () => {
    it('should POST to /api/clients', () => {
      const body = { companyName: 'New Co', industry: null, website: null, status: 'Prospect' as const, accountManagerEmployeeId: null };

      service.create(body).subscribe((c) => expect(c).toEqual(mockClient));

      const req = httpMock.expectOne('/api/clients');
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(body);
      req.flush(mockClient);
    });
  });

  describe('update', () => {
    it('should PUT to /api/clients/:id', () => {
      const body = { companyName: 'Updated', industry: null, website: null, status: 'Active' as const, accountManagerEmployeeId: null };

      service.update('c1', body).subscribe((c) => expect(c).toEqual(mockClient));

      const req = httpMock.expectOne('/api/clients/c1');
      expect(req.request.method).toBe('PUT');
      req.flush(mockClient);
    });
  });

  describe('delete', () => {
    it('should DELETE /api/clients/:id', () => {
      service.delete('c1').subscribe();

      const req = httpMock.expectOne('/api/clients/c1');
      expect(req.request.method).toBe('DELETE');
      req.flush(null);
    });
  });

  describe('addContact', () => {
    it('should POST to /api/clients/:id/contacts', () => {
      const contactReq = { firstName: 'John', lastName: 'Smith', email: null, phone: null, jobTitle: null, isPrimary: true };
      const mockContact: ClientContact = { id: 'ct1', clientId: 'c1', firstName: 'John', lastName: 'Smith', fullName: 'John Smith', email: null, phone: null, jobTitle: null, isPrimary: true };

      service.addContact('c1', contactReq).subscribe((c) => expect(c).toEqual(mockContact));

      const req = httpMock.expectOne('/api/clients/c1/contacts');
      expect(req.request.method).toBe('POST');
      req.flush(mockContact);
    });
  });

  describe('removeContact', () => {
    it('should DELETE /api/clients/:clientId/contacts/:contactId', () => {
      service.removeContact('c1', 'ct1').subscribe();

      const req = httpMock.expectOne('/api/clients/c1/contacts/ct1');
      expect(req.request.method).toBe('DELETE');
      req.flush(null);
    });
  });
});
