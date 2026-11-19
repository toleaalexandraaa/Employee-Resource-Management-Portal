export type NotificationType =
  | 'TaskAssigned'
  | 'LeaveRequestSubmitted'
  | 'LeaveRequestApproved'
  | 'LeaveRequestRejected'
  | 'PayrollApproved'
  | 'InvoicePaymentRecorded';

export interface Notification {
  id: string;
  type: NotificationType;
  title: string;
  body: string;
  isRead: boolean;
  createdAt: string;
}
