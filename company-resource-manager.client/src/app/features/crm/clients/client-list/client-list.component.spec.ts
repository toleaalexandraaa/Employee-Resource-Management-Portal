import { vi } from 'vitest';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { NO_ERRORS_SCHEMA, ChangeDetectorRef } from '@angular/core';
import { MatDialog, MatDialogRef } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';
import { of, throwError, Subject } from 'rxjs';
import { TranslateModule } from '@ngx-translate/core';

import { ClientListComponent } from './client-list.component';
import { ClientService } from '../../../../core/services/client.service';
import { AnalyticsService } from '../../../../core/services/analytics.service';
import { ClientListItem } from '../../../../core/models/crm.models';
import { ClientSegmentsData } from '../../../../core/models/analytics.models';
import { PagedResult } from '../../../../core/models/shared.models';

const mockClients: ClientListItem[] = [
  { id: 'c1', companyName: 'Acme Corp', industry: 'Tech', status: 'Active', accountManagerName: 'Jane' },
  { id: 'c2', companyName: 'Beta LLC', industry: 'Finance', status: 'Prospect', accountManagerName: null },
];

const mockPagedResult: PagedResult<ClientListItem> = {
  items: mockClients,
  totalCount: 2,
  page: 1,
  pageSize: 10,
};

const mockSegmentsNoData: ClientSegmentsData = {
  clients: [],
  hasSufficientData: false,
  message: 'Not enough data',
};

describe('ClientListComponent', () => {
  let component: ClientListComponent;
  let fixture: ComponentFixture<ClientListComponent>;
  let clientService: { getAll: ReturnType<typeof vi.fn>; delete: ReturnType<typeof vi.fn> };
  let analyticsService: { getClientSegments: ReturnType<typeof vi.fn> };
  let dialog: { open: ReturnType<typeof vi.fn> };
  let snackBar: { open: ReturnType<typeof vi.fn> };

  beforeEach(async () => {
    clientService = { getAll: vi.fn(), delete: vi.fn() };
    analyticsService = { getClientSegments: vi.fn() };
    dialog = { open: vi.fn() };
    snackBar = { open: vi.fn() };

    clientService.getAll.mockReturnValue(of(mockPagedResult));
    analyticsService.getClientSegments.mockReturnValue(of(mockSegmentsNoData));

    await TestBed.configureTestingModule({
      declarations: [ClientListComponent],
      imports: [TranslateModule.forRoot()],
      providers: [
        { provide: ClientService, useValue: clientService },
        { provide: AnalyticsService, useValue: analyticsService },
        { provide: MatDialog, useValue: dialog },
        { provide: MatSnackBar, useValue: snackBar },
        ChangeDetectorRef,
      ],
      schemas: [NO_ERRORS_SCHEMA],
    }).compileComponents();

    fixture = TestBed.createComponent(ClientListComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  describe('ngOnInit', () => {
    it('should load clients on init', () => {
      expect(clientService.getAll).toHaveBeenCalledWith(1, 10);
      expect(component.dataSource.data).toEqual(mockClients);
      expect(component.totalCount).toBe(2);
    });

    it('should set loading to false after successful load', () => {
      expect(component.loading).toBe(false);
    });

    it('should call getClientSegments on init', () => {
      expect(analyticsService.getClientSegments).toHaveBeenCalled();
    });

    it('should populate segmentMap when segments have sufficient data', () => {
      const segmentsData: ClientSegmentsData = {
        hasSufficientData: true,
        message: '',
        clients: [
          {
            clientId: 'c1',
            clientName: 'Acme Corp',
            totalRevenue: 100000,
            projectCount: 3,
            avgPaymentDelayDays: 5,
            openInvoiceAmount: 0,
            leadCount: 1,
            cluster: 0,
            clusterLabel: 'Key Account',
          },
        ],
      };
      analyticsService.getClientSegments.mockReturnValue(of(segmentsData));

      component.ngOnInit();

      expect(component.segmentMap['c1']).toBeDefined();
      expect(component.segmentMap['c1'].clusterLabel).toBe('Key Account');
    });

    it('should leave segmentMap empty when insufficient data', () => {
      expect(component.segmentMap).toEqual({});
    });
  });

  describe('load', () => {
    it('should set loading to true then false', () => {
      const subject = new Subject<PagedResult<ClientListItem>>();
      clientService.getAll.mockReturnValue(subject.asObservable());

      component.load(2);
      expect(component.loading).toBe(true);

      subject.next(mockPagedResult);
      expect(component.loading).toBe(false);
    });

    it('should set loading to false on error', () => {
      clientService.getAll.mockReturnValue(throwError(() => new Error('Server error')));
      component.load(1);
      expect(component.loading).toBe(false);
    });
  });

  describe('onPage', () => {
    it('should update pageSize and load next page', () => {
      vi.spyOn(component, 'load');
      component.onPage({ pageIndex: 1, pageSize: 5 });
      expect(component.pageSize).toBe(5);
      expect(component.load).toHaveBeenCalledWith(2);
    });
  });

  describe('delete', () => {
    const client = mockClients[0];

    it('should open a confirm dialog', () => {
      const afterClosed$ = new Subject<boolean>();
      dialog.open.mockReturnValue({ afterClosed: () => afterClosed$.asObservable() } as MatDialogRef<any>);

      component.delete(client);

      expect(dialog.open).toHaveBeenCalled();
      const dialogArgs = dialog.open.mock.calls[dialog.open.mock.calls.length - 1];
      expect(dialogArgs[1]?.data?.title).toBe('Delete Client');
      expect(dialogArgs[1]?.data?.message).toContain('Acme Corp');
    });

    it('should call clientService.delete and show snackbar on confirm', () => {
      dialog.open.mockReturnValue({ afterClosed: () => of(true) } as MatDialogRef<any>);
      clientService.delete.mockReturnValue(of(undefined));

      component.delete(client);

      expect(clientService.delete).toHaveBeenCalledWith('c1');
      expect(snackBar.open).toHaveBeenCalledWith('Client deleted', 'Close', { duration: 3000 });
    });

    it('should reload from page 1 after successful delete', () => {
      dialog.open.mockReturnValue({ afterClosed: () => of(true) } as MatDialogRef<any>);
      clientService.delete.mockReturnValue(of(undefined));
      vi.spyOn(component, 'load');

      component.delete(client);

      expect(component.load).toHaveBeenCalledWith(1);
    });

    it('should NOT call delete when user cancels the dialog', () => {
      dialog.open.mockReturnValue({ afterClosed: () => of(false) } as MatDialogRef<any>);

      component.delete(client);

      expect(clientService.delete).not.toHaveBeenCalled();
    });

    it('should show error snackbar when delete fails', () => {
      dialog.open.mockReturnValue({ afterClosed: () => of(true) } as MatDialogRef<any>);
      clientService.delete.mockReturnValue(throwError(() => new Error('Delete failed')));

      component.delete(client);

      expect(snackBar.open).toHaveBeenCalledWith('Failed to delete', 'Close', { duration: 3000 });
    });
  });
});
