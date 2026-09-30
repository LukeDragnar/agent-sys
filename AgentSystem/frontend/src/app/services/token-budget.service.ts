import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface DashboardTotals {
  totalTokens: number;
  totalInput: number;
  totalOutput: number;
  sessionCount: number;
  gitUserCount: number;
}

export interface GitUserUsageSummary {
  gitUser: string;
  totalInputTokens: number;
  totalOutputTokens: number;
  totalTokens: number;
  sessionCount: number;
}

export interface DailyTrend {
  date: string;
  totalTokens: number;
  inputTokens: number;
  outputTokens: number;
  conversationCount: number;
}

export interface SessionTokenSummary {
  sessionId: string;
  sessionTitle: string;
  gitUser: string;
  agentType: string;
  totalTokens: number;
  inputTokens: number;
  outputTokens: number;
  messageCount: number;
  createdAt: string;
}

export interface TokenBudgetDashboard {
  totals: DashboardTotals;
  users: GitUserUsageSummary[];
  dailyTrend: DailyTrend[];
  topSessions: SessionTokenSummary[];
  from: string;
  to: string;
}

@Injectable({ providedIn: 'root' })
export class TokenBudgetService {
  constructor(private http: HttpClient) {}

  getDashboard(from?: string, to?: string): Observable<TokenBudgetDashboard> {
    let params = new HttpParams();
    if (from) params = params.set('from', from);
    if (to) params = params.set('to', to);
    return this.http.get<TokenBudgetDashboard>('/api/token-budget/dashboard', { params });
  }
}
