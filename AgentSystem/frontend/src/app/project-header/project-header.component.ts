import { Component, OnInit, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ProjectService, Project, UserProjectConfig } from '../services/project.service';

const SELECTED_KEY = 'selected_project_ids';
const MODULE_SELECTED_PREFIX = 'selected_modules_';

export interface PhModule { name: string; description: string; }

@Component({
  selector: 'app-project-header',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './project-header.component.html',
  styleUrl: './project-header.component.scss'
})
export class ProjectHeaderComponent implements OnInit {
  projects: Project[] = [];
  checked: Record<string, boolean> = {};
  dropdownOpen = false;

  // per-project: expanded state + loaded modules + checked modules
  expanded: Record<string, boolean> = {};
  moduleMap: Record<string, PhModule[]> = {};       // loaded from API
  checkedModules: Record<string, Record<string, boolean>> = {};  // projectId → {name: bool}
  loadingModules: Record<string, boolean> = {};

  constructor(private projectService: ProjectService) {}

  ngOnInit(): void {
    try {
      const saved = localStorage.getItem(SELECTED_KEY);
      if (saved) this.checked = JSON.parse(saved);
    } catch {}
    this.projectService.getAll().subscribe(list => {
      this.projects = list;
      // Pre-load checked module state from localStorage for all projects
      list.forEach(p => {
        try {
          const saved = localStorage.getItem(MODULE_SELECTED_PREFIX + p.id);
          if (saved) this.checkedModules[p.id] = JSON.parse(saved);
        } catch {}
      });
    });
  }

  get selectedProjects(): Project[] {
    return this.projects.filter(p => this.checked[p.id]);
  }

  // Summary label for the header button
  get headerLabel(): string {
    const sel = this.selectedProjects;
    if (sel.length === 0) return '';
    const first = sel[0];
    const mods = this.checkedModules[first.id];
    const modCount = mods ? Object.values(mods).filter(v => v).length : 0;
    let label = first.name;
    if (modCount > 0) label += ` · ${modCount} module`;
    if (sel.length > 1) label += ` +${sel.length - 1}`;
    return label;
  }

  toggleProject(id: string): void {
    this.checked = { ...this.checked, [id]: !this.checked[id] };
    localStorage.setItem(SELECTED_KEY, JSON.stringify(this.checked));
  }

  toggleExpand(projectId: string): void {
    this.expanded[projectId] = !this.expanded[projectId];
    if (this.expanded[projectId] && !this.moduleMap[projectId]) {
      this.loadModules(projectId);
    }
  }

  loadModules(projectId: string): void {
    this.loadingModules[projectId] = true;
    this.projectService.getMyConfig(projectId).subscribe({
      next: cfg => {
        this.loadingModules[projectId] = false;
        try {
          const mods: PhModule[] = cfg?.localModulesJson ? JSON.parse(cfg.localModulesJson) : [];
          this.moduleMap[projectId] = mods;
          // If no saved selection yet → select all by default
          if (!this.checkedModules[projectId] && mods.length) {
            const all: Record<string, boolean> = {};
            mods.forEach(m => { if (m.name) all[m.name] = true; });
            this.checkedModules[projectId] = all;
            localStorage.setItem(MODULE_SELECTED_PREFIX + projectId, JSON.stringify(all));
          }
        } catch {
          this.moduleMap[projectId] = [];
        }
      },
      error: () => { this.loadingModules[projectId] = false; this.moduleMap[projectId] = []; }
    });
  }

  toggleModule(projectId: string, moduleName: string): void {
    const cur = this.checkedModules[projectId] ?? {};
    const next = { ...cur, [moduleName]: !cur[moduleName] };
    this.checkedModules = { ...this.checkedModules, [projectId]: next };
    localStorage.setItem(MODULE_SELECTED_PREFIX + projectId, JSON.stringify(next));
  }

  isModuleChecked(projectId: string, name: string): boolean {
    return !!this.checkedModules[projectId]?.[name];
  }

  allModulesChecked(projectId: string): boolean {
    const mods = this.moduleMap[projectId] ?? [];
    return mods.length > 0 && mods.every(m => this.checkedModules[projectId]?.[m.name]);
  }

  someModulesChecked(projectId: string): boolean {
    return (this.moduleMap[projectId] ?? []).some(m => this.checkedModules[projectId]?.[m.name]);
  }

  toggleAllModules(projectId: string): void {
    const mods = this.moduleMap[projectId] ?? [];
    const next = !this.allModulesChecked(projectId);
    const result: Record<string, boolean> = {};
    mods.forEach(m => { if (m.name) result[m.name] = next; });
    this.checkedModules = { ...this.checkedModules, [projectId]: result };
    localStorage.setItem(MODULE_SELECTED_PREFIX + projectId, JSON.stringify(result));
  }

  moduleCountLabel(projectId: string): string {
    const mods = this.moduleMap[projectId] ?? [];
    const selected = Object.values(this.checkedModules[projectId] ?? {}).filter(v => v).length;
    const total = mods.length || Object.keys(this.checkedModules[projectId] ?? {}).length;
    if (!total) return '';
    return `${selected}/${total}`;
  }

  @HostListener('document:click', ['$event'])
  onDocClick(e: MouseEvent): void {
    const host = (e.target as HTMLElement).closest('app-project-header');
    if (!host) this.dropdownOpen = false;
  }
}
