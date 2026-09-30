# Recipe — Phân quyền màn hình (Page + MaTinhNang + [SBAuthorize])

> **Trạng thái: ✅ Verify từ source SCRM** (quét 2026-06-19).

## Khi nào dùng
Thêm màn hình mới / đổi route → phải khai báo quyền BE + insert bảng `Page` + FE ẩn/hiện theo quyền.

## BE — [SBAuthorize] + MaTinhNang
```csharp
[SBAuthorize("DonHang_TimTheoDieuKien", "Danh sách đơn hàng", "DV-AD-TONG, DV-AD-CN, DV-NV")]
[HttpPost("TimTheoDieuKien")]
public async Task<IActionResult> TimTheoDieuKienAsync(DonHangFilterDto gridInfo)
    => ResponseResult(await _service.TimTheoDieuKienAsync(gridInfo, CurrentUser));
```
- Constructor: `SBAuthorize(quyen, moTa, motaRole)` — `quyen` prefix = `MaTinhNang` (vd `DonHang_*` ↔ `MaTinhNang='DonHang'`).
- `[PrivateSBAuthorize]` cho internal service-to-service (chỉ check SystemAuthorization token).
- `[TienIch("...", MaTienIchSCRM.TI_HeThong, ...)]` đặt trên class controller gắn metadata tính năng.

## DB — bảng `Page` (menu/quyền)
Entity `Core.Data/Entity/Page.cs`: `DisplayName`, `Url`, **`MaTinhNang`**, `ParentId`, `Icon`, `Type`, `IsWebsite`, `IsMobile`.
```sql
INSERT INTO [Page] (Id, DisplayName, Url, MaTinhNang, Type, IsWebsite, IsMobile)
VALUES (NEWID(), 'Quản lý đơn hàng', '/pages/don-hang', 'DonHang', 'menu', 1, 1);
-- MaTinhNang PHẢI khớp prefix trong [SBAuthorize("DonHang_...")]
```

## FE — ẩn/hiện theo quyền
```typescript
// AuthGuard.canActivate → check quyền theo URL
this._pageService.CheckUserAccess({ url: state.url }).subscribe(rs => {
  if (rs.data) { obs.next(true); } else { obs.next(false); this.router.navigate(['/']); }
});
// menu.service.ts: CheckUserAccess(model) → POST {table}/CheckUserAccess
```
BE `MenuController.CheckUserAccess` → `PageService` kiểm tra role + hợp đồng + thiết bị → trả `data: true/false`.

## Bẫy thường gặp
- **Thêm màn mới phải insert `Page`** kèm `MaTinhNang` khớp `[SBAuthorize]` — quên thì Guard chặn / menu không hiện.
- **Đổi route phải `UPDATE Page.Url`** cho khớp (xem [qa-code.md](../qa-code.md) mục 13).
- `MaTinhNang` không khớp giữa attribute và `Page` → mất quyền.
- Sau update quyền/Page → clear cache menu (`RemoveWithPatternAsync(Cache_MenuPrefix)`).
- Áp dụng cho **mọi môi trường** có DB riêng (staging/production).

## File source mẫu
- BE: `Libs/SB.Core/Token/AuthorizeAttribute.cs` · `Core/Core.Data/Entity/Page.cs` · `Core/Core.API/Controllers/MenuController.cs`
- FE: `src/app/guard/auth.guard.ts` · `src/app/services/core/menu.service.ts`
