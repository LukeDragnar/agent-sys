import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AdminService, AdminUser, AdminSession, UsageLog, UserUsageSummary } from '../services/admin.service';
import { AuthService } from '../services/auth.service';

@Component({
  selector: 'app-admin',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './admin.component.html',
  styleUrl: './admin.component.scss'
})
export class AdminComponent implements OnInit {
  activeTab: 'users' | 'usage' | 'reports' | 'sessions' = 'users';
  readonly Math = Math;

  // ── Users tab ──────────────────────────────────────────────
  users: AdminUser[] = [];
  showUserForm = false;
  editingUser: AdminUser | null = null;
  userForm = { username: '', password: '', role: 'user', weeklyTokenLimit: 100000, isActive: true };
  userFormError = '';

  // ── Usage tab ─────────────────────────────────────────────
  usageLogs: UsageLog[] = [];
  usageFilterUserId = '';
  usageFilterFrom = '';
  usageFilterTo = '';

  // ── Sessions tab ──────────────────────────────────────────
  sessions: AdminSession[] = [];
  sessionFilterUserId = '';
  sessionFilterFrom = '';
  sessionFilterTo = '';
  expandedSessionId: string | null = null;

  // ── Reports tab ───────────────────────────────────────────
  reportData: UserUsageSummary[] = [];
  reportWeek: number;
  reportYear: number;
  reportWeekLabel = '';

  constructor(
    private adminSvc: AdminService,
    private authSvc: AuthService,
    private router: Router
  ) {
    const now = new Date();
    // Simple ISO week approximation for default values
    const jan4 = new Date(now.getFullYear(), 0, 4);
    const startOfWeek = new Date(jan4);
    startOfWeek.setDate(jan4.getDate() - jan4.getDay() + 1);
    this.reportWeek = Math.ceil(((now.getTime() - startOfWeek.getTime()) / 86400000 + 1) / 7);
    this.reportYear = now.getFullYear();
  }

  ngOnInit(): void {
    this.loadUsers();
    this.loadReport();
    this.loadSessions();
  }

  // ── Navigation ────────────────────────────────────────────

  goToChat(): void { this.router.navigate(['/']); }

  logout(): void {
    if (confirm('Bạn có chắc muốn đăng xuất?')) this.authSvc.logout();
  }

  get currentUser() { return this.authSvc.getCurrentUser(); }

  // ── Users ─────────────────────────────────────────────────

  loadUsers(): void {
    this.adminSvc.getUsers().subscribe(u => this.users = u);
  }

  openCreateForm(): void {
    this.editingUser = null;
    this.userForm = { username: '', password: '', role: 'user', weeklyTokenLimit: 100000, isActive: true };
    this.userFormError = '';
    this.showUserForm = true;
  }

  openEditForm(user: AdminUser): void {
    this.editingUser = user;
    this.userForm = {
      username: user.username,
      password: '',
      role: user.role,
      weeklyTokenLimit: user.weeklyTokenLimit,
      isActive: user.isActive
    };
    this.userFormError = '';
    this.showUserForm = true;
  }

  saveUser(): void {
    this.userFormError = '';
    if (this.editingUser) {
      const req: any = { role: this.userForm.role, weeklyTokenLimit: this.userForm.weeklyTokenLimit, isActive: this.userForm.isActive };
      if (this.userForm.password) req.password = this.userForm.password;
      this.adminSvc.updateUser(this.editingUser.id, req).subscribe({
        next: () => { this.showUserForm = false; this.loadUsers(); },
        error: (e) => this.userFormError = e.error?.message ?? 'Lỗi cập nhật.'
      });
    } else {
      if (!this.userForm.password) { this.userFormError = 'Mật khẩu không được để trống.'; return; }
      this.adminSvc.createUser(this.userForm).subscribe({
        next: () => { this.showUserForm = false; this.loadUsers(); },
        error: (e) => this.userFormError = e.error?.message ?? 'Lỗi tạo user.'
      });
    }
  }

  deleteUser(user: AdminUser): void {
    if (!confirm(`Xóa user "${user.username}"?`)) return;
    this.adminSvc.deleteUser(user.id).subscribe(() => this.loadUsers());
  }

  toggleActive(user: AdminUser): void {
    this.adminSvc.updateUser(user.id, { isActive: !user.isActive }).subscribe(() => this.loadUsers());
  }

  // ── Usage ─────────────────────────────────────────────────

  loadUsage(): void {
    if (!this.usageFilterUserId) return;
    this.adminSvc.getUsage(
      this.usageFilterUserId,
      this.usageFilterFrom || undefined,
      this.usageFilterTo || undefined
    ).subscribe(logs => this.usageLogs = logs);
  }

  getUserName(id: string): string {
    return this.users.find(u => u.id === id)?.username ?? id.slice(0, 8) + '…';
  }

  // ── Sessions ─────────────────────────────────────────────

  loadSessions(): void {
    this.adminSvc.getAllSessions(
      this.sessionFilterUserId || undefined,
      this.sessionFilterFrom || undefined,
      this.sessionFilterTo || undefined
    ).subscribe(s => this.sessions = s);
  }

  toggleSession(id: string): void {
    this.expandedSessionId = this.expandedSessionId === id ? null : id;
  }

  // ── Reports ───────────────────────────────────────────────

  loadReport(): void {
    this.adminSvc.getTopUsers(this.reportWeek, this.reportYear).subscribe(res => {
      this.reportData = res.data;
      this.reportWeekLabel = `Tuần ${res.week}/${res.year}`;
    });
  }

  prevWeek(): void {
    this.reportWeek--;
    if (this.reportWeek < 1) { this.reportWeek = 52; this.reportYear--; }
    this.loadReport();
  }

  nextWeek(): void {
    this.reportWeek++;
    if (this.reportWeek > 52) { this.reportWeek = 1; this.reportYear++; }
    this.loadReport();
  }

  formatTokens(n: number): string {
    return n >= 1000 ? (n / 1000).toFixed(1) + 'k' : String(n);
  }
}
