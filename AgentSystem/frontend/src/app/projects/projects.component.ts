import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ProjectService, Project, UserProjectConfig } from '../services/project.service';

export interface SelectedProjects {
  [projectId: string]: boolean;
}

export interface ProjectModule {
  name: string;
  description: string;
  sourcePath: string;
  outputPath: string;
}

const SELECTED_KEY = 'selected_project_ids';
const MODULE_SELECTED_PREFIX = 'selected_modules_';

@Component({
  selector: 'app-projects',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './projects.component.html',
  styleUrl: './projects.component.scss'
})
export class ProjectsComponent implements OnInit {
  projects: Project[] = [];
  loading = false;
  error = '';

  // Multi-select via checkbox
  checked: SelectedProjects = {};

  // Selected for detail view (single click)
  detailProject: Project | null = null;
  myConfig: UserProjectConfig | null = null;
  configLoading = false;

  // Inline path editing
  editBePath = '';
  editFePath = '';
  editOutputDir = '';
  savingConfig = false;

  // Modules (sub-projects)
  modules: ProjectModule[] = [];
  checkedModules: Record<string, boolean> = {};
  get allModulesChecked(): boolean {
    return this.modules.length > 0 && this.modules.every(m => m.name && this.checkedModules[m.name]);
  }
  get someModulesChecked(): boolean {
    return this.modules.some(m => m.name && this.checkedModules[m.name]);
  }

  // Path picker dialog
  dialogOpen = false;
  dialogField: 'bePath' | 'fePath' | 'outputDir' | null = null;
  dialogLabel = '';
  dialogValue = '';
  dialogModuleIndex: number | null = null;
  dialogModuleField: 'sourcePath' | 'outputPath' | null = null;

  // Create project
  showCreate = false;
  newName = '';
  newDesc = '';
  creating = false;

  // Scan
  scanning = false;
  scanResult = '';

  constructor(private projectService: ProjectService) {}

  ngOnInit(): void {
    // Restore checked state
    try {
      const saved = localStorage.getItem(SELECTED_KEY);
      if (saved) this.checked = JSON.parse(saved);
    } catch {}
    this.loadProjects();
  }

  loadProjects(): void {
    this.loading = true;
    this.projectService.getAll().subscribe({
      next: list => {
        this.projects = list;
        this.loading = false;
        // Auto-select first if none checked
        if (list.length && !list.some(p => this.checked[p.id])) {
          this.setChecked(list[0].id, true);
        }
        // Auto-open detail for first checked
        const firstChecked = list.find(p => this.checked[p.id]);
        if (firstChecked && !this.detailProject) this.openDetail(firstChecked);
      },
      error: err => { this.error = err.error ?? 'Lỗi tải'; this.loading = false; }
    });
  }

  setChecked(id: string, value: boolean): void {
    this.checked = { ...this.checked, [id]: value };
    localStorage.setItem(SELECTED_KEY, JSON.stringify(this.checked));
  }

  toggleCheck(id: string): void {
    this.setChecked(id, !this.checked[id]);
  }

  get selectedIds(): string[] {
    return Object.keys(this.checked).filter(k => this.checked[k]);
  }

  openDetail(p: Project): void {
    this.detailProject = p;
    this.scanResult = '';
    this.error = '';
    this.configLoading = true;
    this.projectService.getMyConfig(p.id).subscribe({
      next: cfg => {
        this.myConfig = cfg;
        this.editBePath = cfg?.localBePath ?? '';
        this.editFePath = cfg?.localFePath ?? '';
        this.editOutputDir = cfg?.localOutputDir ?? '';
        try {
          this.modules = cfg?.localModulesJson ? JSON.parse(cfg.localModulesJson) : [];
        } catch { this.modules = []; }
        this.loadCheckedModules(p.id);
        this.configLoading = false;
      },
      error: () => {
        this.myConfig = null;
        this.editBePath = '';
        this.editFePath = '';
        this.editOutputDir = '';
        this.modules = [];
        this.checkedModules = {};
        this.configLoading = false;
      }
    });
  }

  // Path dialog — for main paths
  openPathDialog(field: 'bePath' | 'fePath' | 'outputDir'): void {
    this.dialogField = field;
    this.dialogModuleIndex = null;
    this.dialogModuleField = null;
    this.dialogLabel = field === 'bePath' ? 'Backend path' : field === 'fePath' ? 'Frontend path' : 'Output directory';
    this.dialogValue = field === 'bePath' ? this.editBePath : field === 'fePath' ? this.editFePath : this.editOutputDir;
    this.dialogOpen = true;
  }

  // Path dialog — for module paths
  openModulePathDialog(index: number, field: 'sourcePath' | 'outputPath'): void {
    this.dialogField = null;
    this.dialogModuleIndex = index;
    this.dialogModuleField = field;
    this.dialogLabel = field === 'sourcePath' ? `Module source — ${this.modules[index].name}` : `Module output — ${this.modules[index].name}`;
    this.dialogValue = this.modules[index][field];
    this.dialogOpen = true;
  }

  confirmDialog(): void {
    if (this.dialogModuleIndex !== null && this.dialogModuleField) {
      this.modules[this.dialogModuleIndex][this.dialogModuleField] = this.dialogValue;
      this.dialogOpen = false;
      this.saveMyConfig();
      return;
    }
    if (!this.dialogField) return;
    if (this.dialogField === 'bePath') this.editBePath = this.dialogValue;
    else if (this.dialogField === 'fePath') this.editFePath = this.dialogValue;
    else this.editOutputDir = this.dialogValue;
    this.dialogOpen = false;
    this.saveMyConfig();
  }

  cancelDialog(): void {
    this.dialogOpen = false;
    this.dialogField = null;
    this.dialogModuleIndex = null;
    this.dialogModuleField = null;
  }

  loadCheckedModules(projectId: string): void {
    try {
      const saved = localStorage.getItem(MODULE_SELECTED_PREFIX + projectId);
      this.checkedModules = saved ? JSON.parse(saved) : {};
    } catch { this.checkedModules = {}; }
    // If nothing saved yet, select all by default
    if (!Object.keys(this.checkedModules).length && this.modules.length) {
      const all: Record<string, boolean> = {};
      this.modules.forEach(m => { if (m.name) all[m.name] = true; });
      this.checkedModules = all;
      localStorage.setItem(MODULE_SELECTED_PREFIX + projectId, JSON.stringify(all));
    }
  }

  toggleModule(name: string): void {
    if (!this.detailProject || !name) return;
    this.checkedModules = { ...this.checkedModules, [name]: !this.checkedModules[name] };
    localStorage.setItem(MODULE_SELECTED_PREFIX + this.detailProject.id, JSON.stringify(this.checkedModules));
  }

  toggleAllModules(): void {
    if (!this.detailProject) return;
    const next = !this.allModulesChecked;
    const result: Record<string, boolean> = {};
    this.modules.forEach(m => { if (m.name) result[m.name] = next; });
    this.checkedModules = result;
    localStorage.setItem(MODULE_SELECTED_PREFIX + this.detailProject.id, JSON.stringify(result));
  }

  get selectedModuleNames(): string[] {
    return Object.keys(this.checkedModules).filter(k => this.checkedModules[k]);
  }

  addModule(): void {
    this.modules = [...this.modules, { name: '', description: '', sourcePath: '', outputPath: '' }];
  }

  removeModule(index: number): void {
    this.modules = this.modules.filter((_, i) => i !== index);
    this.saveMyConfig();
  }

  saveMyConfig(): void {
    if (!this.detailProject) return;
    this.savingConfig = true;
    const modulesJson = this.modules.length ? JSON.stringify(this.modules) : null;
    this.projectService.upsertMyConfig(this.detailProject.id, this.editBePath, this.editFePath, this.editOutputDir, modulesJson).subscribe({
      next: cfg => { this.myConfig = cfg; this.savingConfig = false; },
      error: err => { this.error = err.error ?? 'Lỗi lưu'; this.savingConfig = false; }
    });
  }

  scanProject(): void {
    if (!this.detailProject) return;
    const bePath = this.editBePath;
    const fePath = this.editFePath;
    if (!bePath && !fePath) { this.error = 'Cần nhập đường dẫn BE hoặc FE trước khi scan'; return; }
    this.scanning = true;
    this.scanResult = '';
    this.error = '';
    this.projectService.scan(this.detailProject.id, bePath, fePath).subscribe({
      next: r => {
        this.scanResult = r.profile;
        this.scanning = false;
        // Refresh to get lastScannedAt
        this.projectService.getById(this.detailProject!.id).subscribe(updated => {
          const idx = this.projects.findIndex(p => p.id === updated.id);
          if (idx >= 0) this.projects[idx] = updated;
          this.detailProject = updated;
        });
      },
      error: err => { this.error = err.error ?? 'Lỗi scan'; this.scanning = false; }
    });
  }

  createProject(): void {
    if (!this.newName.trim()) return;
    this.creating = true;
    this.projectService.create(this.newName.trim(), this.newDesc.trim()).subscribe({
      next: p => {
        this.projects = [...this.projects, p];
        this.newName = '';
        this.newDesc = '';
        this.showCreate = false;
        this.creating = false;
        this.setChecked(p.id, true);
        this.openDetail(p);
      },
      error: err => { this.error = err.error ?? 'Lỗi tạo'; this.creating = false; }
    });
  }

  deleteProject(p: Project, event: Event): void {
    event.stopPropagation();
    if (!confirm(`Xoá project "${p.name}"?`)) return;
    this.projectService.delete(p.id).subscribe({
      next: () => {
        this.projects = this.projects.filter(x => x.id !== p.id);
        const { [p.id]: _, ...rest } = this.checked;
        this.checked = rest;
        localStorage.setItem(SELECTED_KEY, JSON.stringify(this.checked));
        if (this.detailProject?.id === p.id) this.detailProject = null;
      },
      error: err => { this.error = err.error ?? 'Lỗi xoá'; }
    });
  }
}
