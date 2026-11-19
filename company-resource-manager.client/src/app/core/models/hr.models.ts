export interface EmployeeListItem {
  id: string;
  fullName: string;
  department: string;
  jobTitle: string;
  workEmail: string;
  isActive: boolean;
  seniorityYears: number;
}

export interface Employee {
  id: string;
  firstName: string;
  lastName: string;
  fullName: string;
  personalEmail: string;
  workEmail: string;
  phone: string;
  birthDate: string;
  hireDate: string;
  department: string;
  jobTitle: string;
  managerId: string | null;
  managerName: string | null;
  seniorityYears: number;
  isActive: boolean;
}

export interface CreateEmployeeRequest {
  firstName: string;
  lastName: string;
  personalEmail: string;
  workEmail: string;
  phone: string;
  birthDate: string;
  hireDate: string;
  department: string;
  jobTitle: string;
  managerId: string | null;
  seniorityYears: number;
  role: string;
}

export interface CreateEmployeeResult {
  employee: Employee;
  temporaryPassword: string;
}

export interface UpdateEmployeeRequest {
  firstName: string;
  lastName: string;
  personalEmail: string;
  workEmail: string;
  phone: string;
  department: string;
  jobTitle: string;
  managerId: string | null;
  seniorityYears: number;
  isActive: boolean;
}

export interface EmploymentContract {
  id: string;
  employeeId: string;
  employeeName: string;
  contractType: ContractType;
  startDate: string;
  endDate: string | null;
  grossSalary: number;
  currency: string;
  isActive: boolean;
}

export type ContractType = 'FullTime' | 'PartTime' | 'Contractor' | 'Internship';

export interface CreateContractRequest {
  employeeId: string;
  contractType: ContractType;
  startDate: string;
  endDate: string | null;
  grossSalary: number;
  currency: string;
}

export interface UpdateContractRequest {
  contractType: ContractType;
  startDate: string;
  endDate: string | null;
  grossSalary: number;
  currency: string;
  isActive: boolean;
}

export type LeaveType = 'Annual' | 'Medical' | 'Unpaid' | 'Parental' | 'Other';
export type LeaveStatus = 'Pending' | 'Approved' | 'Rejected' | 'Cancelled';

export interface LeaveRequest {
  id: string;
  employeeId: string;
  employeeName: string;
  leaveType: LeaveType;
  startDate: string;
  endDate: string;
  reason: string | null;
  status: LeaveStatus;
  reviewedAt: string | null;
  reviewerName: string | null;
  rejectionReason: string | null;
}

export interface CreateLeaveRequest {
  leaveType: LeaveType;
  startDate: string;
  endDate: string;
  workingDaysCount: number;
  reason: string | null;
}

export interface ReviewLeaveRequest {
  reason: string | null;
}

export interface LeaveBalance {
  leaveType: LeaveType;
  year: number;
  totalDays: number;
  usedDays: number;
  remainingDays: number;
}

export type PayrollStatus = 'Draft' | 'Approved';

export interface PayrollRecord {
  id: string;
  employeeId: string;
  employeeName: string;
  month: number;
  year: number;
  grossSalary: number;
  bonusAmount: number;
  taxDeductions: number;
  socialContributions: number;
  mealTicketTotal: number;
  netSalary: number;
  currency: string;
  status: PayrollStatus;
  approvedAt: string | null;
  approverName: string | null;
}

export interface GeneratePayrollRequest {
  employeeId: string;
  month: number;
  year: number;
  bonusAmount: number;
  mealTicketCount: number;
  mealTicketValue: number;
}
