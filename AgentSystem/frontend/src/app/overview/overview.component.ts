import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

type Tab = 'intro' | 'architecture' | 'setup' | 'usage' | 'codemap' | 'roadmap';

@Component({
  selector: 'app-overview',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './overview.component.html',
  styleUrl: './overview.component.scss'
})
export class OverviewComponent {
  activeTab: Tab = 'intro';

  tabs: { id: Tab; label: string }[] = [
    { id: 'intro', label: 'Giới thiệu' },
    { id: 'architecture', label: 'Kiến trúc' },
    { id: 'setup', label: 'Hướng dẫn Setup' },
    { id: 'usage', label: 'Cách sử dụng' },
    { id: 'codemap', label: 'CodeMap' },
    { id: 'roadmap', label: 'Roadmap' },
  ];
}
