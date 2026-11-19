export interface MonthlyValue {
  label: string;
  value: number;
}

export interface LabelValue {
  label: string;
  value: number;
}

export interface OverviewKpi {
  activeEmployees: number;
  payrollThisMonth: number;
  activeProjects: number;
  budgetVariancePct: number;
  openLeadsCount: number;
  openLeadsValue: number;
  revenueThisMonth: number;
  overdueInvoicesCount: number;
  overdueInvoicesAmount: number;
  pendingLeaveRequests: number;
  revenueLast12Months: MonthlyValue[];
  hiresLast6Months: MonthlyValue[];
}

// ── HR — Workforce ────────────────────────────────────────────────────────────

export interface WorkforceData {
  employeesByDepartment: LabelValue[];
  employeesByContractType: LabelValue[];
  seniorityDistribution: LabelValue[];
  hiresLast24Months: MonthlyValue[];
}

// ── HR — Leave ────────────────────────────────────────────────────────────────

export interface LeaveBalanceItem {
  leaveType: string;
  totalDays: number;
  usedDays: number;
}

export interface PendingLeaveItem {
  id: string;
  employeeName: string;
  leaveType: string;
  startDate: string;
  endDate: string;
  workingDays: number;
  submittedAt: string;
}

export interface DepartmentHeatmapRow {
  department: string;
  values: number[];
}

export interface HrLeaveData {
  balanceUtilization: LeaveBalanceItem[];
  leaveMonthLabels: string[];
  leaveTypeLabels: string[];
  leaveByMonthMatrix: number[][];
  pendingCount: number;
  pendingRequests: PendingLeaveItem[];
  heatmapMonthLabels: string[];
  leaveByDepartment: DepartmentHeatmapRow[];
}

// ── Project — Health ──────────────────────────────────────────────────────────

export interface ProjectBudgetVariance {
  projectName: string;
  variance: number;
}

export interface DurationScatterPoint {
  projectName: string;
  estimatedDays: number;
  actualDays: number;
}

export interface ProjectHealthData {
  byStatus: LabelValue[];
  budgetVariances: ProjectBudgetVariance[];
  completedCount: number;
  onTimeCount: number;
  lateProjectNames: string[];
  durationScatter: DurationScatterPoint[];
}

// ── Project — Time ────────────────────────────────────────────────────────────

export interface ProjectHours {
  projectName: string;
  estimatedHours: number;
  loggedHours: number;
}

export interface MemberUtilization {
  memberName: string;
  allocatedHoursPerWeek: number;
  loggedHoursThisWeek: number;
}

export interface ProjectTimeData {
  hoursByProject: ProjectHours[];
  teamUtilization: MemberUtilization[];
  hoursByDayOfWeek: LabelValue[];
  weeklyHours: MonthlyValue[];
}

// ── Project — Tasks ───────────────────────────────────────────────────────────

export interface WipViolation {
  projectName: string;
  columnName: string;
  wipLimit: number;
  currentCount: number;
}

export interface ProjectTasksData {
  tasksByStatus: LabelValue[];
  tasksByPriority: LabelValue[];
  avgCycleTimeByPriority: LabelValue[];
  wipViolations: WipViolation[];
  completionVelocity: MonthlyValue[];
}

// ── HR — Payroll ──────────────────────────────────────────────────────────────

export interface PayrollComposition {
  avgGross: number;
  avgBonus: number;
  avgMealTickets: number;
  avgTax: number;
  avgSocial: number;
  avgNet: number;
}

export interface HrPayrollData {
  costLast12Months: MonthlyValue[];
  avgSalaryByDepartment: LabelValue[];
  composition: PayrollComposition;
  currentMonthDraft: number;
  currentMonthApproved: number;
  currentMonthPaid: number;
}

// ── CRM — Leads ───────────────────────────────────────────────────────────────

export interface LeadFunnelStage {
  stage: string;
  count: number;
  totalValue: number;
}

export interface CrmLeadsData {
  funnel: LeadFunnelStage[];
  wonCount: number;
  lostCount: number;
  leadsBySource: LabelValue[];
  avgDealSizeBySource: LabelValue[];
  avgConversionDaysBySource: LabelValue[];
}

// ── CRM — Revenue ─────────────────────────────────────────────────────────────

export interface InvoiceAgingBucket {
  bucket: string;
  count: number;
  totalAmount: number;
}

export interface ReceivablesByStatus {
  status: string;
  amount: number;
}

export interface CrmRevenueData {
  revenueLast12Months: MonthlyValue[];
  revenueByClient: LabelValue[];
  revenueByIndustry: LabelValue[];
  agingBuckets: InvoiceAgingBucket[];
  totalOutstanding: number;
  outstandingByStatus: ReceivablesByStatus[];
}

// ── CRM — Clients ─────────────────────────────────────────────────────────────

export interface TopClient {
  clientName: string;
  projectCount: number;
  totalInvoiced: number;
  totalPaid: number;
}

export interface CrmClientsData {
  clientsByStatus: LabelValue[];
  clientsByIndustry: LabelValue[];
  acquisitionByQuarter: MonthlyValue[];
  topClients: TopClient[];
}

// ── ML Clustering ─────────────────────────────────────────────────────────────

export interface EmployeeClusterPoint {
  employeeId: string;
  employeeName: string;
  hoursLogged: number;
  tasksCompleted: number;
  hoursVsEstimatedRatio: number;
  projectsActive: number;
  seniorityYears: number;
  cluster: number;
  clusterLabel: string;
}

export interface EmployeeClustersData {
  employees: EmployeeClusterPoint[];
  hasSufficientData: boolean;
  message: string;
}

export interface ClientSegmentPoint {
  clientId: string;
  clientName: string;
  totalRevenue: number;
  projectCount: number;
  avgPaymentDelayDays: number;
  openInvoiceAmount: number;
  leadCount: number;
  cluster: number;
  clusterLabel: string;
}

export interface ClientSegmentsData {
  clients: ClientSegmentPoint[];
  hasSufficientData: boolean;
  message: string;
}

export interface ProjectRiskScore {
  projectId: string;
  projectName: string;
  riskScore: number;
  riskLevel: 'OnTrack' | 'Watch' | 'AtRisk';
  budgetBurnRate: number;
  timeBurnRate: number;
  timeElapsedRatio: number;
  overdueTaskCount: number;
}

export interface ProjectRisksData {
  projects: ProjectRiskScore[];
  hasSufficientData: boolean;
  message: string;
}
