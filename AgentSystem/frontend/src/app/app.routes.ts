import { Routes } from '@angular/router';
import { authGuard } from './guards/auth.guard';
import { adminGuard } from './guards/admin.guard';

export const routes: Routes = [
  { path: 'login', loadComponent: () => import('./login/login.component').then(m => m.LoginComponent) },
  {
    path: '',
    loadComponent: () => import('./shell/shell.component').then(m => m.ShellComponent),
    canActivate: [authGuard],
    children: [
      { path: '', loadComponent: () => import('./chat/chat.component').then(m => m.ChatComponent) },
      { path: 'orchestrator', loadComponent: () => import('./orchestrator/orchestrator.component').then(m => m.OrchestratorComponent) },
      { path: 'dashboard', loadComponent: () => import('./dashboard/dashboard.component').then(m => m.DashboardComponent) },
      { path: 'logs', loadComponent: () => import('./logs/logs.component').then(m => m.LogsComponent) },
      { path: 'files', loadComponent: () => import('./files/files.component').then(m => m.FilesComponent) },
      { path: 'projects', loadComponent: () => import('./projects/projects.component').then(m => m.ProjectsComponent) },
      { path: 'admin', loadComponent: () => import('./admin/admin.component').then(m => m.AdminComponent), canActivate: [adminGuard] },
      { path: 'overview', loadComponent: () => import('./overview/overview.component').then(m => m.OverviewComponent) },
      { path: 'codemap', loadComponent: () => import('./codemap/codemap.component').then(m => m.CodeMapComponent) },
      { path: 'token-budget', loadComponent: () => import('./token-budget/token-budget.component').then(m => m.TokenBudgetComponent) },
      { path: 'specs', loadComponent: () => import('./specs/specs.component').then(m => m.SpecsComponent) },
      { path: 'specs/:specSlug/:taskSlug', loadComponent: () => import('./specs/specs.component').then(m => m.SpecsComponent) },
    ]
  },
  { path: '**', redirectTo: 'dashboard' }
];
