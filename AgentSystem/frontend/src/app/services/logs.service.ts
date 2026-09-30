import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface OrchestratorLog {
  id: string;
  category: string;
  agentType: string;
  userId?: string;
  sessionId?: string;
  input: string;
  output: string;
  durationMs: number;
  createdAt: string;
}

@Injectable({ providedIn: 'root' })
export class LogsService {

   private readonly base = 'http://10.0.161.21:50888/api/logs';
  constructor(private http: HttpClient) {}

  getLogs(category?: string, agentType?: string, userId?: string, sessionId?: string, from?: string, to?: string): Observable<OrchestratorLog[]> {
    let params = new HttpParams();
    if (category)  params = params.set('category', category);
    if (agentType) params = params.set('agentType', agentType);
    if (userId)    params = params.set('userId', userId);
    if (sessionId) params = params.set('sessionId', sessionId);
    if (from)      params = params.set('from', from);
    if (to)        params = params.set('to', to);
    return this.http.get<OrchestratorLog[]>(this.base, { params });
  }
}
