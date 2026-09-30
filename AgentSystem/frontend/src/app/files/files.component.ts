import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { OrchestratorService, DirEntry } from '../services/orchestrator.service';

export interface FileNode {
  name: string;
  path: string;
  isFolder: boolean;
  children?: FileNode[];   // undefined = not loaded yet
  expanded: boolean;
  loading: boolean;
}

@Component({
  selector: 'app-files',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './files.component.html',
  styleUrl: './files.component.scss'
})
export class FilesComponent implements OnInit {
  rootDir = localStorage.getItem('files_rootDir') ?? localStorage.getItem('orch_outputDir') ?? '';
  rootLoading = false;
  rootError = '';

  tree: FileNode[] = [];

  selectedPath = '';
  selectedContent = '';
  fileLoading = false;

  constructor(private service: OrchestratorService, private router: Router) {}

  ngOnInit(): void {
    if (this.rootDir) this.openRoot();
  }

  openRoot(): void {
    if (!this.rootDir.trim()) return;
    localStorage.setItem('files_rootDir', this.rootDir);
    this.rootLoading = true;
    this.rootError = '';
    this.tree = [];
    this.selectedPath = '';
    this.selectedContent = '';

    this.service.listDir(this.rootDir).subscribe({
      next: entries => {
        this.tree = this.toNodes(entries);
        this.rootLoading = false;
        if (!entries.length) this.rootError = 'Thư mục trống hoặc không tồn tại.';
      },
      error: err => {
        this.rootError = err.error ?? 'Không thể đọc thư mục';
        this.rootLoading = false;
      }
    });
  }

  toggleFolder(node: FileNode): void {
    if (!node.isFolder) return;
    if (node.expanded) {
      node.expanded = false;
      return;
    }
    if (node.children !== undefined) {
      node.expanded = true;
      return;
    }
    // Load children lazily
    node.loading = true;
    this.service.listDir(node.path).subscribe({
      next: entries => {
        node.children = this.toNodes(entries);
        node.expanded = true;
        node.loading = false;
      },
      error: () => {
        node.children = [];
        node.expanded = true;
        node.loading = false;
      }
    });
  }

  selectFile(node: FileNode): void {
    if (node.isFolder) return;
    this.selectedPath = node.path;
    this.selectedContent = '';
    this.fileLoading = true;
    this.service.readFile(node.path).subscribe({
      next: r => { this.selectedContent = r.content; this.fileLoading = false; },
      error: () => { this.selectedContent = '(Không đọc được file)'; this.fileLoading = false; }
    });
  }

  private toNodes(entries: DirEntry[]): FileNode[] {
    return entries.map(e => ({
      name: e.name,
      path: e.path,
      isFolder: e.isFolder,
      children: undefined,
      expanded: false,
      loading: false
    }));
  }

  getFileIcon(name: string): string {
    const ext = name.split('.').pop()?.toLowerCase() ?? '';
    const icons: Record<string, string> = {
      cs: '◈', ts: '◆', html: '◇',
      scss: '◉', css: '◉', sql: '▣',
      json: '{}', md: '≡', txt: '≡'
    };
    return icons[ext] ?? '·';
  }

  get language(): string {
    const ext = this.selectedPath.split('.').pop()?.toLowerCase() ?? '';
    const map: Record<string, string> = {
      cs: 'csharp', ts: 'typescript', html: 'html',
      scss: 'scss', css: 'css', sql: 'sql',
      json: 'json', md: 'markdown'
    };
    return map[ext] ?? 'plaintext';
  }

  back(): void { this.router.navigate(['/dashboard']); }
}
