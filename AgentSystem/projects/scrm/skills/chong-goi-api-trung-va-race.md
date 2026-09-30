# Recipe: Chống gọi API trùng + race khi đổi filter / chuyển tab (FE Angular)

**Khi nào dùng:** màn có nhiều tab / nhiều API cùng phụ thuộc 1 bộ filter (đơn vị + sale + ngày...), hay bị:
- Gọi API **2 lần** khi chuyển tab / khi set filter bằng code.
- **Race:** đổi filter nhanh → response cũ về sau đè lên kết quả mới → data lệch filter.

Rút từ màn Ticket hỗ trợ (`ticket-ho-tro.component.ts`, session 2026-07-20).

## 1. Debounce search server + cờ bỏ qua search programmatic
Search-select gọi server mỗi lần gõ → debounce; và khi **set giá trị bằng code** (không phải user gõ) thì bỏ qua để khỏi gọi thừa:
```ts
private boQuaSearchSaleKe = false;

onOpenSale(open: boolean) { if (open) this.boQuaSearchSaleKe = false; }

onSearchSale(kw: string) {
  if (this.boQuaSearchSaleKe) { this.boQuaSearchSaleKe = false; return; } // set bằng code → bỏ
  this.searchSale$.next(kw);   // Subject + debounceTime(300) ở ngOnInit
}
```

## 2. Signature-guard chống refetch trùng (`_key`)
Trước khi gọi, so **chữ ký state**; trùng thì bỏ. Có `force` cho case bắt buộc refetch (sau tạo/sửa):
```ts
private _tongQuanKey = '';
loadTongQuan(force = false) {
  const key = `${this.donViChon}|${this.saleChon}|${(this.idDonViLoc||[]).join(',')}|${this.tuNgay}|${this.denNgay}`;
  if (!force && key === this._tongQuanKey) return;  // trùng chữ ký → skip
  this._tongQuanKey = key;
  // ... gọi API
}
```

## 3. Seq-guard chống race async (chụp state trước await, so lại sau)
```ts
doiSale() {
  this.donViChon = null;
  const saleLucGoi = this.saleChon;                  // chụp mốc TRƯỚC await
  this.napDanhSachDonVi(this.saleChon).then(() => {
    if (saleLucGoi !== this.saleChon) return;        // sale đã đổi tiếp → bỏ response cũ
    this.reloadTabHienTai();
  });
}
napDanhSachDonVi(saleId) {
  return this._adDonViService.LayTatCaCoBan(saleId).then((rs: any) => {
    if ((saleId||null) !== (this.saleChon||null)) return;  // out-of-order → bỏ
    this.danhSachDonVi = ...;
  });
}
```

## Bẫy thường gặp
- **Reload theo "sự kiện" thay vì "chữ ký"** → 2 tab cùng subscribe 1 event → gọi 2 lần. Dùng signature-guard (#2) là triệt để nhất.
- **Set filter mặc định trong ngOnInit** kích hoạt `valueChanges` → gọi API sớm/thừa → dùng cờ `boQuaSearchSaleKe` hoặc `{emitEvent:false}`.
- Quên `force=true` sau khi tạo/sửa bản ghi → chữ ký không đổi → list không refresh.

Liên quan: `search-filter.md`, `dropdown-cache.md`, `autocomplete-select.md`.
