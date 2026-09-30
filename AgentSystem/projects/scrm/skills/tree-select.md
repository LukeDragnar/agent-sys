# Recipe — Tree / TreeSelect

> **Trạng thái: ✅ Verify từ source SCRM** (quét 2026-06-19).

## Khi nào dùng
Cây phân cấp cha-con (đơn vị, menu, danh mục). `nz-tree-select` để chọn 1 node; `nz-tree` để hiển thị + click.

## Build cây từ list phẳng (parentId)
```typescript
interface TreeNode { key: string; title: string; idNodeCha: string | null; children: TreeNode[]; }
private buildTree(flat: any[]): TreeNode[] {
  const map = new Map<string, TreeNode>();
  flat.forEach(n => map.set(n.id, { key: n.id, title: n.ten, idNodeCha: n.idNodeCha, children: [] }));
  const roots: TreeNode[] = [];
  map.forEach(node => {
    const pid = node.idNodeCha;
    if (pid && pid !== '' && map.has(pid)) map.get(pid)!.children.push(node);   // có cha
    else roots.push(node);                                                       // root
  });
  return roots;
}
```

## Template
```html
<!-- chọn 1 node -->
<nz-tree-select nzShowSearch formControlName="parentId" [nzNodes]="gridData"
                [nzDropdownStyle]="{ 'max-height': '300px' }"></nz-tree-select>

<!-- hiển thị + click -->
<nz-tree [nzData]="gridData" nzShowLine (nzClick)="onViewPage($event)"></nz-tree>
```
```typescript
onViewPage(e: NzFormatEmitEvent) { const id = e.node.key; /* load detail... */ }
```

## Bẫy thường gặp
- **`NzTreeSelectModule` KHÔNG nằm trong `antdModule`** — phải import riêng (`import { NzTreeSelectModule } from 'ng-zorro-antd/tree-select'`). Tương tự `NzTreeModule`.
- Ant Design tree dùng **`key`/`title`** (không phải `id`/`ten`) — transform khi build.
- `key` phải **duy nhất** (đừng dùng tên làm key — trùng tên → sai).
- Normalize `idNodeCha` rỗng: `null`/`''`/`'undefined'` đều coi là root.
- Cây lớn: lazy load children qua `(nzExpandChange)` + `node.addChildren(...)`.

## File source mẫu
- `pages/website/quanly-menu/quanly-menu-edit/quanly-menu-edit.component.ts` (nz-tree-select + nz-tree)
- Build tree: `pages/work/node-tree/node-tree.component.ts`
- Import module: `pages/caidat-hethong/caidat-hethong.module.ts`
