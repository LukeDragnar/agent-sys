# Recipe — Lưu (create + update chung 1 method)

> **Trạng thái: ✅ Verify từ source SCRM** (quét 2026-06-19).

## Khi nào dùng
Lưu form: `Id` rỗng → create (`InsertAsync`); `Id` có giá trị → update (`GetByIdAsync` → set field → `MarkDirty` → `UpdateAsync`).

## BE — Luu/LuuAsync
```csharp
public async Task<IMethodResult<Guid>> Luu(ReqLuuThuChi req, UserLogin userLogin)
{
    var err = await ValidationSaveAsync(req);
    if (!string.IsNullOrEmpty(err)) return MethodResult<Guid>.ResultWithError(err, Contants.CODE_Error);

    var idDonVi = userLogin.DonViId; var idChiNhanh = userLogin.IdChiNhanh;
    if (userLogin.IsSuperUser || userLogin.IsAdminHeThong()) { idDonVi = req.IdDonVi; idChiNhanh = req.IdChiNhanh; }

    var db = await reposThuChi.TableWithNolock()
        .Where(x => !x.IsDelete && x.IdDonVi == idDonVi && x.IdChiNhanh == idChiNhanh && x.Id == req.Id)
        .FirstOrDefaultAsync();

    if (db is null) {                                    // CREATE
        db = new ThuChi { IdDonVi = idDonVi, IdChiNhanh = idChiNhanh,   // set tenant khi insert
                          NguoiTaoUserId = userLogin.Id, NguoiTaoUserName = userLogin.UserName };
    }
    db = await CoppyThuChi(req, db);

    if (db.Id == Guid.Empty) await base.InsertAsync(db);
    else {
        db.MarkDirty(nameof(db.SoTien)); db.MarkDirty(nameof(db.LoaiThuChi)); /* mọi field đổi */
        await base.UpdateAsync(db);
    }
    return MethodResult<Guid>.ResultWithData(db.Id);
}
```
Controller: `[HttpPost("Luu")]` → `ResponseResult(await _service.Luu(body, CurrentUser))`.

## FE — submit
```typescript
onLuu() {
  this.markFormGroupTouched(this.fDetail);
  if (this.fDetail.invalid) return;
  const body = { ...this.fDetail.value, id: this.inIdThuChi ?? Guid.EMPTY };   // EMPTY = create
  this.thuChiService.luu(body).then(res => {
    if (res.success) {
      this.notification.success(MESSAGE.SUCCESS, body.id == Guid.EMPTY ? MESSAGE.ADD_SUCCESS : MESSAGE.UPDATE_SUCCESS);
      this.modal.close(true);                                                  // đóng + reload cha
    } else this.notification.error(res.error, res.message);
  });
}
```

## Bẫy thường gặp
- **MarkDirty bắt buộc khi update** — thiếu thì field không lưu dù đã gán. Gọi cho MỌI field đã đổi.
- **Set `IdDonVi`/`IdChiNhanh` khi insert** — quên thì query sau không thấy data (cross-tenant).
- FE gửi `id = Guid.EMPTY` cho form mới; BE load không thấy → tạo mới.
- Validate đầu method, lỗi return ngay.

## File source mẫu
- BE: `Branch/Branch.Service/Generated/ThuChi/ThuChiService.cs:181`
- FE: `src/app/pages/major/thuchi/thu-chi-chi-tiet/thu-chi-chi-tiet.component.ts:344`
