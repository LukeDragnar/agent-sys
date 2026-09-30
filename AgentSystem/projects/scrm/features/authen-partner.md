# AuthenPartner — Xác Thực Đối Tác (Sandbox/Partner API)

> Xác thực và phục vụ dữ liệu cho các đối tác bên ngoài truy cập SCRM qua API (sandbox). Đối tác quản lý lead, contact, đơn hàng logistics qua giao diện riêng.

---

## Vai trò

- ← **Partner systems** (TingTong, sandbox partner...): đăng nhập, truy cập dữ liệu
- → **Contact**: cập nhật kết quả tác nghiệp lead
- → **Elasticsearch**: tìm kiếm contact
- → **MongoDB**: ghi audit log mọi request/response

---

## Entities chính

| Entity | Ý nghĩa |
|--------|---------|
| `User` | Tài khoản partner: tương tự nhân viên nhưng cho đối tác |
| `CauHinh` | Cấu hình partner: `IdDonVi`, `JsonSetting`, `JsonSetting_SuperAdmin` |
| `LogTingTong` (MongoDB) | Log mọi API call: `idDonVi`, `method`, `endpoint`, `RequestBody`, `ResponseBody`, `ErrorMessage`, timestamp |

---

## API Endpoints

| Endpoint | Mô tả |
|----------|-------|
| `POST /api/lead/UpdateLeadResult` [SBAuthorizePartner] | Đối tác cập nhật kết quả xử lý lead |
| `/api/contact/*` | Tìm kiếm contact (filter theo IdChiNhanh, nhóm sale) |
| `/api/donhanglogistic/*` | Xem/cập nhật đơn logistics |
| `/api/thukhotacnghiep/*` | Tác nghiệp kho |
| `/api/tongdai/*` | Tích hợp tổng đài |

---

## Luồng

```
[Đối tác cập nhật lead]
Partner POST /api/lead/UpdateLeadResult
  ↓
LeadController → Ghi log MongoDB (LogTingTong)
  ↓
LeadService.UpdateLeadResult()
  └→ Gọi Contact service → cập nhật tác nghiệp (TacNghiepCan)
       EnumTypeTingTong, EnumActivity

[Đối tác tìm kiếm contact]
Partner POST /api/contact/TimTheoDieuKien
  ├→ Validate IdChiNhanh (bắt buộc)
  ├→ Filter theo nhóm sale / user sale
  ├→ Check cấu hình → Elasticsearch hoặc SQL
  └→ Return danh sách contact (kèm cấu hình quyền)

[Đồng bộ tài khoản đối tác]
CRM gọi UserService.ThemMoiTaiKhoanAsync()
  └→ Transaction + ExecutionStrategy (database resilience)
```

---

## Đặc điểm quan trọng

- **SBAuthorizePartner**: token riêng cho partner (khác với SBAuthorize nhân viên)
- **Mọi lead action** đều log vào MongoDB (`LogTingTong`) để audit
- Contact filter **bắt buộc** `IdChiNhanh` — partner chỉ thấy data chi nhánh được cấp quyền
- Dùng **Elasticsearch** nếu đơn vị đã bật (`DonViElasticSearch`)
- **ExecutionStrategy**: tránh lỗi mất consistency khi đồng bộ tài khoản

---

## Checklist fix bug

- [ ] Partner không đăng nhập được → kiểm tra `User.IsLockedOut`, cấu hình SBAuthorizePartner
- [ ] Cập nhật lead lỗi → xem `LogTingTong.ErrorMessage` trong MongoDB
- [ ] Không thấy contact → kiểm tra filter IdChiNhanh, quyền cấu hình
- [ ] Search chậm → kiểm tra Elasticsearch có được bật cho đơn vị chưa
