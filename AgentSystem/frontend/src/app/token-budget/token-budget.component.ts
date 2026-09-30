import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import {
  TokenBudgetService,
  TokenBudgetDashboard,
  DashboardTotals,
  GitUserUsageSummary,
  DailyTrend,
  SessionTokenSummary
} from '../services/token-budget.service';

@Component({
  selector: 'app-token-budget',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './token-budget.component.html',
  styleUrl: './token-budget.component.scss'
})
export class TokenBudgetComponent implements OnInit {
  readonly Math = Math;

  totals: DashboardTotals | null = null;
  users: GitUserUsageSummary[] = [];
  dailyTrend: DailyTrend[] = [];
  topSessions: SessionTokenSummary[] = [];
  loading = true;
  maxDailyTokens = 0;
  maxUserTokens = 0;

  // Date filters
  filterFrom = '';
  filterTo = '';
  rangeLabel = '';

  // Quick range
  activeRange: 'week' | 'month' | 'custom' = 'week';

  constructor(private svc: TokenBudgetService) {}

  ngOnInit(): void {
    this.setThisWeek();
  }

  setThisWeek(): void {
    const now = new Date();
    const day = now.getDay();
    const monday = new Date(now);
    monday.setDate(now.getDate() - (day === 0 ? 6 : day - 1));
    this.filterFrom = this.toDateStr(monday);
    this.filterTo = this.toDateStr(now);
    this.activeRange = 'week';
    this.load();
  }

  setThisMonth(): void {
    const now = new Date();
    const first = new Date(now.getFullYear(), now.getMonth(), 1);
    this.filterFrom = this.toDateStr(first);
    this.filterTo = this.toDateStr(now);
    this.activeRange = 'month';
    this.load();
  }

  applyCustom(): void {
    if (!this.filterFrom || !this.filterTo) return;
    this.activeRange = 'custom';
    this.load();
  }

  load(): void {
    this.loading = true;
    this.svc.getDashboard(this.filterFrom, this.filterTo).subscribe({
      next: data => this.applyData(data),
      error: () => this.loading = false
    });
  }

  formatTokens(n: number): string {
    if (n >= 1_000_000) return (n / 1_000_000).toFixed(1) + 'M';
    if (n >= 1_000) return (n / 1_000).toFixed(1) + 'k';
    return String(n);
  }

  barHeight(tokens: number): number {
    if (this.maxDailyTokens === 0) return 0;
    return Math.max(2, (tokens / this.maxDailyTokens) * 100);
  }

  userBarWidth(tokens: number): number {
    if (this.maxUserTokens === 0) return 0;
    return Math.max(2, (tokens / this.maxUserTokens) * 100);
  }

  dayLabel(dateStr: string): string {
    const d = new Date(dateStr);
    const days = ['CN', 'T2', 'T3', 'T4', 'T5', 'T6', 'T7'];
    return days[d.getDay()];
  }

  dayDate(dateStr: string): string {
    const d = new Date(dateStr);
    return `${d.getDate()}/${d.getMonth() + 1}`;
  }

  private toDateStr(d: Date): string {
    return d.toISOString().slice(0, 10);
  }

  private formatRange(from: string, to: string): string {
    const f = new Date(from);
    const t = new Date(to);
    const fmt = (d: Date) => `${d.getDate()}/${d.getMonth() + 1}/${d.getFullYear()}`;
    return `${fmt(f)} — ${fmt(t)}`;
  }

  private applyData(data: TokenBudgetDashboard): void {
    this.totals = data.totals;
    this.users = data.users;
    this.dailyTrend = data.dailyTrend;
    this.topSessions = data.topSessions;
    this.rangeLabel = this.formatRange(this.filterFrom, this.filterTo);
    this.maxDailyTokens = Math.max(...data.dailyTrend.map(d => d.totalTokens), 1);
    this.maxUserTokens = data.users.length ? data.users[0].totalTokens : 1;
    this.loading = false;
  }
}
