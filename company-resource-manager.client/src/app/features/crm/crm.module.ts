import { NgModule } from '@angular/core';
import { SharedModule } from '../../shared/shared.module';
import { CrmRoutingModule } from './crm-routing.module';
import { ClientListComponent } from './clients/client-list/client-list.component';
import { ClientFormComponent } from './clients/client-form/client-form.component';
import { ClientDetailComponent } from './clients/client-detail/client-detail.component';
import { CrmContractListComponent } from './crm-contracts/crm-contract-list/crm-contract-list.component';
import { CrmContractFormComponent } from './crm-contracts/crm-contract-form/crm-contract-form.component';
import { InvoiceListComponent } from './invoices/invoice-list/invoice-list.component';
import { InvoiceFormComponent } from './invoices/invoice-form/invoice-form.component';
import { LeadListComponent } from './leads/lead-list/lead-list.component';
import { LeadFormComponent } from './leads/lead-form/lead-form.component';
import { ConvertLeadDialogComponent } from './leads/convert-lead-dialog/convert-lead-dialog.component';
import { MatIconModule } from '@angular/material/icon';

@NgModule({
  declarations: [
    ClientListComponent,
    ClientFormComponent,
    ClientDetailComponent,
    CrmContractListComponent,
    CrmContractFormComponent,
    InvoiceListComponent,
    InvoiceFormComponent,
    LeadListComponent,
    LeadFormComponent,
    ConvertLeadDialogComponent,
  ],
  imports: [SharedModule, CrmRoutingModule, MatIconModule],
})
export class CrmModule {}
