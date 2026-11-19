export type ProjectStatus = 'Planning' | 'Active' | 'OnHold' | 'Completed' | 'Cancelled';
export type TaskStatus = 'Backlog' | 'InProgress' | 'InReview' | 'Done' | 'Cancelled';
export type TaskPriority = 'Low' | 'Medium' | 'High' | 'Critical';

export interface ProjectListItem {
  id: string;
  name: string;
  clientName: string | null;
  status: ProjectStatus;
  startDate: string | null;
  endDate: string | null;
  estimatedBudget: number;
  projectManagerId: string;
  projectManagerName: string;
  memberCount: number;
  taskCount: number;
}

export interface Project {
  id: string;
  name: string;
  description: string | null;
  clientId: string | null;
  clientName: string | null;
  projectManagerId: string;
  projectManagerName: string;
  status: ProjectStatus;
  startDate: string | null;
  endDate: string | null;
  estimatedBudget: number;
  actualBudget: number;
  estimatedDurationDays: number;
  memberCount: number;
  taskCount: number;
}

export interface CreateProjectRequest {
  name: string;
  description: string | null;
  clientId: string | null;
  status: ProjectStatus;
  startDate: string | null;
  endDate: string | null;
  estimatedBudget: number;
  estimatedDurationDays: number;
}

export interface UpdateProjectRequest {
  name: string;
  description: string | null;
  clientId: string | null;
  status: ProjectStatus;
  startDate: string | null;
  endDate: string | null;
  estimatedBudget: number;
  actualBudget: number;
  estimatedDurationDays: number;
}

export interface ProjectMember {
  id: string;
  projectId: string;
  employeeId: string;
  employeeName: string;
  role: string;
  allocatedHoursPerWeek: number;
  joinDate: string;
  leaveDate: string | null;
}

export interface AddProjectMemberRequest {
  employeeId: string;
  role: string;
  allocatedHoursPerWeek: number;
  joinDate: string;
  leaveDate: string | null;
}

export interface ProjectTask {
  id: string;
  projectId: string;
  projectName: string;
  title: string;
  description: string | null;
  assignedToEmployeeId: string | null;
  assignedToEmployeeName: string | null;
  priority: TaskPriority;
  status: TaskStatus;
  estimatedHours: number;
  dueDate: string | null;
  kanbanColumnId: string | null;
  kanbanColumnName: string | null;
  sortOrder: number;
}

export interface CreateTaskRequest {
  projectId: string;
  title: string;
  description: string | null;
  assignedToEmployeeId: string | null;
  priority: TaskPriority;
  estimatedHours: number;
  dueDate: string | null;
  kanbanColumnId: string | null;
}

export interface UpdateTaskRequest {
  title: string;
  description: string | null;
  assignedToEmployeeId: string | null;
  priority: TaskPriority;
  status: TaskStatus;
  estimatedHours: number;
  dueDate: string | null;
}

export interface MoveTaskRequest {
  targetColumnId: string | null;
  newSortOrder: number;
}

export interface TimeEntry {
  id: string;
  employeeId: string;
  employeeName: string;
  taskId: string | null;
  taskTitle: string | null;
  projectId: string;
  projectName: string;
  date: string;
  hoursLogged: number;
  description: string | null;
}

export interface CreateTimeEntryRequest {
  taskId: string | null;
  projectId: string;
  date: string;
  hoursLogged: number;
  description: string | null;
}

export interface UpdateTimeEntryRequest {
  date: string;
  hoursLogged: number;
  description: string | null;
}

export interface KanbanColumn {
  id: string;
  name: string;
  order: number;
  tasks: ProjectTask[];
}

export interface KanbanBoard {
  id: string;
  projectId: string;
  projectName: string;
  columns: KanbanColumn[];
}

export interface ReorderColumnsRequest {
  columns: { columnId: string; order: number }[];
}

export interface GanttTask {
  id: string;
  title: string;
  startDate: string | null;
  dueDate: string | null;
  assignedToEmployeeId: string | null;
  assignedToEmployeeName: string | null;
  status: TaskStatus;
  priority: TaskPriority;
  estimatedHours: number;
}
