import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { KanbanBoard, ReorderColumnsRequest } from '../models/project.models';

@Injectable({ providedIn: 'root' })
export class KanbanService {
  private readonly base = '/api/kanban';

  constructor(private http: HttpClient) {}

  getBoard(projectId: string): Observable<KanbanBoard> {
    return this.http.get<KanbanBoard>(`${this.base}/${projectId}`);
  }

  reorderColumns(projectId: string, request: ReorderColumnsRequest): Observable<void> {
    return this.http.put<void>(`${this.base}/${projectId}/columns`, request);
  }
}
