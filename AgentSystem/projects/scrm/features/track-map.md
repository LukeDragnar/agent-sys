# TrackMap — Tìm Kiếm Bản Đồ

> Tích hợp Track Asia API để cung cấp autocomplete địa điểm cho toàn hệ thống. Ghi nhận thống kê sử dụng theo đơn vị.

---

## Vai trò

- ← **Frontend / các service**: gọi API tìm kiếm địa chỉ
- → **Track Asia** (bên thứ 3): API autocomplete địa điểm

---

## Entities chính

| Entity | Ý nghĩa |
|--------|---------|
| `TrackMapStatistics` | Thống kê: `ServerCode`, `IdDonVi`, `IdChiNhanh`, `NgayThucHien`, `SoLuong` |

---

## API Endpoints

| Endpoint | Mô tả |
|----------|-------|
| `GET /api/SearchMap/TimMapAsiaAutoComplete?text={text}` | Autocomplete tìm kiếm địa điểm (yêu cầu min 8 ký tự) |

---

## Luồng

```
Frontend nhập địa chỉ (≥8 ký tự)
  ↓
SearchMapController → SBAuthorize (token required)
  ↓
MapAsiaService.TimMapAsiaAutoComplete()
  ├→ Gọi Track Asia API: maps.track-asia.com/api/v1/autocomplete/
  ├→ Ghi TrackMapStatistics (IdDonVi, SoLuong++)
  └→ Return SearchMapDto
```

---

## Checklist fix bug

- [ ] Autocomplete không trả kết quả → kiểm tra kết nối Track Asia API
- [ ] Lỗi 401 → kiểm tra token SBAuthorize
- [ ] Không đủ 8 ký tự → client cần validate trước khi gọi
