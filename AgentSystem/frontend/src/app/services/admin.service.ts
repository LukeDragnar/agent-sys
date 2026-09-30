import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface AdminUser {
  id: string;
  username: string;
  role: string;
  weeklyTokenLimit: number;
  isActive: boolean;
  createdAt: string;
}

export interface UsageLog {
  id: string;
  userId: string;
  sessionId: string;
  inputTokens: number;
  outputTokens: number;
  isoWeek: number;
  isoYear: number;
  createdAt: string;
}

export interface UserUsageSummary {
  userId: string;
  username: string;
  totalInputTokens: number;
  totalOutputTokens: number;
  totalTokens: number;
  weeklyLimit: number;
  usagePercent: number;
}

export interface TopUsersReport {
  week: number;
  year: number;
  data: UserUsageSummary[];
}

export interface SpecSummary {
  id: string;
  name: string;
  taskCount: number;
}

export interface AdminSession {
  id: string;
  title: string;
  agentType: string;
  userId: string;
  username: string;
  claudeModel: string;
  createdAt: string;
  isFavorite: boolean;
  isArchived: boolean;
  specCount: number;
  specs: SpecSummary[];
}

@Injectable({ providedIn: 'root' })
export class AdminService {
  private readonly base = 'http://10.0.161.21:50888/api/admin';

  constructor(private http: HttpClient) {}

  getUsers(): Observable<AdminUser[]> {
    return this.http.get<AdminUser[]>(`${this.base}/users`);
  }

  createUser(req: { username: string; password: string; role: string; weeklyTokenLimit: number }): Observable<AdminUser> {
    return this.http.post<AdminUser>(`${this.base}/users`, req);
  }

  updateUser(id: string, req: { password?: string; role?: string; weeklyTokenLimit?: number; isActive?: boolean }): Observable<AdminUser> {
    return this.http.put<AdminUser>(`${this.base}/users/${id}`, req);
  }

  deleteUser(id: string): Observable<void> {
    return this.http.delete<void>(`${this.base}/users/${id}`);
  }

  getUsage(userId: string, from?: string, to?: string): Observable<UsageLog[]> {
    let params = new HttpParams().set('userId', userId);
    if (from) params = params.set('from', from);
    if (to) params = params.set('to', to);
    return this.http.get<UsageLog[]>(`${this.base}/usage`, { params });
  }

  getTopUsers(week?: number, year?: number, top = 20): Observable<TopUsersReport> {
    let params = new HttpParams().set('top', top);
    if (week != null) params = params.set('week', week);
    if (year != null) params = params.set('year', year);
    return this.http.get<TopUsersReport>(`${this.base}/reports/top-users`, { params });
  }

  getAllSessions(userId?: string, from?: string, to?: string): Observable<AdminSession[]> {
    let params = new HttpParams();
    if (userId) params = params.set('userId', userId);
    if (from) params = params.set('from', from);
    if (to) params = params.set('to', to);
    return this.http.get<AdminSession[]>(`${this.base}/sessions`, { params });
  }
}
