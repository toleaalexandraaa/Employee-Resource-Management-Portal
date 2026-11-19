export type ClientStatus = 'Active' | 'Inactive' | 'Prospect';
export type CrmContractStatus = 'Draft' | 'Signed' | 'Expired' | 'Cancelled';
export type InvoiceStatus = 'Draft' | 'Sent' | 'PartiallyPaid' | 'Paid' | 'Overdue' | 'Cancelled';
export type LeadStage = 'New' | 'Contacted' | 'Qualified' | 'ProposalSent' | 'Won' | 'Lost';

export interface ClientListItem {
  id: string;
  companyName: string;
  industry: string | null;
  status: ClientStatus;
  accountManagerName: string | null;
}

export interface Client {
  id: string;
  companyName: string;
  industry: string | null;
  website: string | null;
  status: ClientStatus;
  accountManagerEmployeeId: string | null;
  accountManagerName: string | null;
  contacts: ClientContact[];
}

export interface CreateClientRequest {
  companyName: string;
  industry: string | null;
  website: string | null;
  status: ClientStatus;
  accountManagerEmployeeId: string | null;
}

export interface UpdateClientRequest {
  companyName: string;
  industry: string | null;
  website: string | null;
  status: ClientStatus;
  accountManagerEmployeeId: string | null;
}

export interface ClientContact {
  id: string;
  clientId: string;
  firstName: string;
  lastName: string;
  fullName: string;
  email: string | null;
  phone: string | null;
  jobTitle: string | null;
  isPrimary: boolean;
}

export interface CreateClientContactRequest {
  firstName: string;
  lastName: string;
  email: string | null;
  phone: string | null;
  jobTitle: string | null;
  isPrimary: boolean;
}

export interface CrmContract {
  id: string;
  contractNumber: string;
  clientId: string;
  clientName: string;
  title: string;
  value: number;
  currency: string;
  startDate: string | null;
  endDate: string | null;
  status: CrmContractStatus;
  signedDate: string | null;
}

export interface CreateCrmContractRequest {
  clientId: string;
  title: string;
  value: number;
  currency: string;
  startDate: string | null;
  endDate: string | null;
}

export interface UpdateCrmContractRequest {
  title: string;
  value: number;
  currency: string;
  startDate: string | null;
  endDate: string | null;
}

export interface InvoiceLineItem {
  id: string;
  description: string;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
}

export interface Invoice {
  id: string;
  invoiceNumber: string;
  clientId: string;
  clientName: string;
  contractId: string | null;
  issueDate: string;
  dueDate: string;
  currency: string;
  taxRate: number;
  subTotal: number;
  taxAmount: number;
  totalAmount: number;
  paidAmount: number;
  status: InvoiceStatus;
  lineItems: InvoiceLineItem[];
}

export interface InvoiceListItem {
  id: string;
  invoiceNumber: string;
  clientName: string;
  issueDate: string;
  dueDate: string;
  totalAmount: number;
  paidAmount: number;
  currency: string;
  status: InvoiceStatus;
}

export interface CreateInvoiceLineItemRequest {
  description: string;
  quantity: number;
  unitPrice: number;
}

export interface CreateInvoiceRequest {
  clientId: string;
  contractId: string | null;
  dueDate: string;
  currency: string;
  taxRate: number;
  lineItems: CreateInvoiceLineItemRequest[];
}

export interface GenerateInvoiceFromTimeEntriesRequest {
  projectId: string;
  clientId: string;
  contractId: string | null;
  dateFrom: string;
  dateTo: string;
  hourlyRate: number;
  currency: string;
  dueDays: number;
}

export interface RecordPaymentRequest {
  amount: number;
  paidAt: string;
}

export interface Lead {
  id: string;
  companyName: string;
  contactName: string | null;
  contactEmail: string | null;
  contactPhone: string | null;
  stage: LeadStage;
  estimatedValue: number | null;
  assignedToEmployeeId: string | null;
  assignedToEmployeeName: string | null;
  notes: string | null;
  convertedToClientId: string | null;
}

export interface CreateLeadRequest {
  companyName: string;
  contactName: string | null;
  contactEmail: string | null;
  contactPhone: string | null;
  estimatedValue: number | null;
  assignedToEmployeeId: string | null;
  notes: string | null;
}

export interface UpdateLeadRequest {
  companyName: string;
  contactName: string | null;
  contactEmail: string | null;
  contactPhone: string | null;
  estimatedValue: number | null;
  assignedToEmployeeId: string | null;
  notes: string | null;
}

export interface ConvertLeadRequest {
  accountManagerId: string | null;
}
