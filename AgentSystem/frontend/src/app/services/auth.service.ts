import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { tap } from 'rxjs';

export interface CurrentUser {
  userId: string;
  username: string;
  role: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly TOKEN_KEY = 'token';

  constructor(private http: HttpClient, private router: Router) {}

  login(username: string, password: string) {
    return this.http.post<{ token: string }>('http://10.0.161.21:50888/api/auth/login', { username, password }).pipe(
      tap(res => localStorage.setItem(this.TOKEN_KEY, res.token))
    );
  }

  logout() {
    localStorage.removeItem(this.TOKEN_KEY);
    this.router.navigate(['/login']);
  }

  getToken(): string | null {
    return localStorage.getItem(this.TOKEN_KEY);
  }

  isLoggedIn(): boolean {
    return !!this.getToken();
  }

  getCurrentUser(): CurrentUser | null {
    const token = this.getToken();
    if (!token) return null;
    try {
      const p = JSON.parse(atob(token.split('.')[1]));
      return {
        userId: p['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'],
        username: p['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'],
        role: p['http://schemas.microsoft.com/ws/2008/06/identity/claims/role']
      };
    } catch {
      return null;
    }
  }

  isAdmin(): boolean {
    return this.getCurrentUser()?.role === 'admin';
  }

  getGitUser(): string {
    return localStorage.getItem('gitUser') ?? '';
  }

  setGitUser(name: string): void {
    localStorage.setItem('gitUser', name.trim());
  }

  hasGitUser(): boolean {
    return !!this.getGitUser();
  }
}
