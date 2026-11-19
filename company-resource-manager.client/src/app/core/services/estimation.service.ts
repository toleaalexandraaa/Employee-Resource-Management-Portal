import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { EstimateProjectRequest, EstimateProjectResponse } from '../models/estimation.models';

@Injectable({ providedIn: 'root' })
export class EstimationService {
  private readonly base = '/api/estimation';

  constructor(private http: HttpClient) {}

  estimate(request: EstimateProjectRequest): Observable<EstimateProjectResponse> {
    return this.http.post<EstimateProjectResponse>(`${this.base}/estimate`, request);
  }

  getStatus(): Observable<EstimateProjectResponse> {
    return this.http.get<EstimateProjectResponse>(`${this.base}/status`);
  }
}
