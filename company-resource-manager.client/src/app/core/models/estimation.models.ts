export interface EstimateProjectRequest {
  teamSize: number;
  estimatedHoursTotal: number;
  taskCount: number;
  hasFixedDeadline: boolean;
  averageSeniorityYears: number;
  isFirstProjectWithClient: boolean;
}

export interface EstimateProjectResponse {
  isModelReady: boolean;
  estimatedCost: number | null;
  estimatedDurationDays: number | null;
  costModelR2: number;
  durationModelR2: number;
  modelVersion: string;
  message: string | null;
}
