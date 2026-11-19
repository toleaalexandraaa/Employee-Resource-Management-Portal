import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { ClientListComponent } from './clients/client-list/client-list.component';
import { ClientFormComponent } from './clients/client-form/client-form.component';
import { ClientDetailComponent } from './clients/client-detail/client-detail.component';
import { CrmContractListComponent } from './crm-contracts/crm-contract-list/crm-contract-list.component';
import { CrmContractFormComponent } from './crm-contracts/crm-contract-form/crm-contract-form.component';
import { InvoiceListComponent } from './invoices/invoice-list/invoice-list.component';
import { InvoiceFormComponent } from './invoices/invoice-form/invoice-form.component';
import { LeadListComponent } from './leads/lead-list/lead-list.component';
import { LeadFormComponent } from './leads/lead-form/lead-form.component';

const routes: Routes = [
  { path: 'clients', component: ClientListComponent },
  { path: 'clients/new', component: ClientFormComponent },
  { path: 'clients/:id/edit', component: ClientFormComponent },
  { path: 'clients/:id', component: ClientDetailComponent },
  { path: 'contracts', component: CrmContractListComponent },
  { path: 'contracts/new', component: CrmContractFormComponent },
  { path: 'invoices', component: InvoiceListComponent },
  { path: 'invoices/new', component: InvoiceFormComponent },
  { path: 'leads', component: LeadListComponent },
  { path: 'leads/new', component: LeadFormComponent },
  { path: 'leads/:id/edit', component: LeadFormComponent },
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule],
})
export class CrmRoutingModule {}
