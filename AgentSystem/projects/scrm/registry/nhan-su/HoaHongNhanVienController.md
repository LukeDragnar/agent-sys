---
controller: HoaHongNhanVienController
module: nhan-su
service: Branch
man_hinh_ui: "Cấu hình > Hoa hồng nhân viên (tabs: HH theo đối tượng/NV, HH tối đa, HH đối tượng mặc định, HH theo nhóm hoa hồng/custom) + lịch sử HH theo NV"
muc_dich: "Cấu hình hoa hồng nhân viên đa tầng: HH theo đối tượng-NV, HH tối đa (CaiDatHoaHongMacDinh), HH đối tượng mặc định, HH custom theo nhóm hoa hồng; cung cấp logic tính HH áp cho đơn hàng (internal) + lịch sử HH."
alias: [HoaHongNhanVien, "hoa hồng nhân viên", "HH nhân viên", "cấu hình hoa hồng NV", "hoa hồng NV theo đối tượng"]
auth: mixed
state_machine: []
deprecated: false
entities: [HoaHongNhanVien, CaiDatHoaHongMacDinh, NhomHoaHong]
stored_procs: []
depends: [IHoaHongNhanVienService, ICaiDatHoaHongMacDinhService, IHoaHongNhanVienCustomService, IHoaHongNhanVienLogicService]
cross_module_in: [Order (XepNhanVien_LayHoaHongNhanVienCauHinh + GetHoaHongApDungByListMaDon — tính/lấy HH áp cho đơn)]
cross_module_out: []
---

# HoaHongNhanVien — Cấu hình & tính hoa hồng nhân viên (đa tầng)

> Controller lớn (379 dòng) gom 4 service: `HoaHongNhanVienService` (HH đối tượng-NV, HH mặc định), `CaiDatHoaHongMacDinhService` (HH tối đa), `HoaHongNhanVienCustomService` (HH theo nhóm hoa hồng + lấy HH áp theo đơn), `HoaHongNhanVienLogicService` (tính HH khi xếp NV vào đơn). Kế thừa `BaseController` (không phải SBBaseController). Phần lớn action có key; 3 endpoint enum không key; 3 endpoint internal `[PrivateSBAuthorize]`/login dùng cho pipeline đơn hàng.

## Hành động (UI / cấu hình)
| # | Hành động | Nhãn UI/nút | Endpoint | Quyền (key) | Auth | Hành vi đúng (expected) | Nguồn | Known-issue |
|---|---|---|---|---|---|---|---|---|
| 1 | Lịch sử HH theo NV | Xem lịch sử | POST `GetListHoaHongLichSuByNhanVien` | `HoaHongNhanVien_GetListHoaHongLichSuByNhanVien` | public | List lịch sử hoa hồng đã phát sinh của NV. | EF | — |
| 2 | HH đối tượng theo NV (list / 1) | Danh sách / chi tiết | POST `LayDSHHDoiTuongTheoNhanVien`, `LayHHDoiTuongTheoNhanVien` | `HoaHongNhanVien_LayDSHHDoiTuongTheoNhanVien`, `..._LayHHDoiTuongTheoNhanVien` | public | Lấy cấu hình HH đối tượng gắn 1 NV / nhiều NV. | EF | — |
| 3 | Lưu HH nhân viên | Lưu | POST `LuuHoaHongNhanVien` | `HoaHongNhanVien_LuuHoaHongNhanVien` (AD/QLDV — bỏ KT) | public | Lưu HH cho nhóm NV hoặc nhóm đối tượng. | EF | Ghi: không cho KT |
| 4 | HH nhân viên tối đa (list/1/lưu) | DS / chi tiết / Lưu | POST `LayDSHHNhanVienToiDa`, `LayHHNhanVienToiDa`, `LuuHHNhanVienToiDa` | `..._LayDSHHNhanVienToiDa`, `..._LayHHNhanVienToiDa`, `..._LuuHHNhanVienToiDa` (lưu: AD/QLDV) | public | Cấu hình trần HH tối đa (`CaiDatHoaHongMacDinh`). | EF | — |
| 5 | HH đối tượng mặc định (list/1/lưu) | DS / chi tiết / Lưu | POST `LayDSHHDoiTuongMacDinh`, `LayHHDoiTuongMacDinh`, `LuuHHDoiTuongMacDinh` | `..._LayDSHHDoiTuongMacDinh`, `..._LayHHDoiTuongMacDinh`, `..._LuuHHDoiTuongMacDinh` (lưu: AD/QLDV) | public | HH mặc định theo đối tượng (IdNhanVien = Guid.Empty). | EF | — |
| 6 | HH custom theo nhóm hoa hồng (list/1) | DS / chi tiết | POST `LayDSHHDoiTuongTheoNhomHoaHong`, `LayHHDoiTuongTheoNhomHoaHong` | `..._LayDSHHDoiTuongTheoNhomHoaHong`, `..._LayHHDoiTuongTheoNhomHoaHong` | public | Lấy HH đối tượng theo nhóm hoa hồng (NhomHoaHong bắt buộc). | EF | — |
| 7 | Lưu HH theo nhóm HH (mặc định / đầy đủ) | Lưu | POST `LuuHHDoiTuongMacDinhTheoNhomHoaHong`, `LuuHHDoiTuongTheoNhomHoaHong` | `..._LuuHHDoiTuongMacDinhTheoNhomHoaHong`, `..._LuuHHDoiTuongTheoNhomHoaHong` | public | Lưu HH mặc định / lưu cả mặc định + theo danh sách NV (insert-or-update). | EF | — |
| 8 | Tạo HH đối tượng-NV với giá trị mặc định | Áp mặc định | POST `TaoHHDoiTuongNhanVienVoiGiaTriMacDinh` | `..._TaoHHDoiTuongNhanVienVoiGiaTriMacDinh` | public | Sinh bản ghi HH cho NV chưa có, lấy giá trị từ HH mặc định. | EF | — |
| 9 | Lấy NV chưa có HH đối tượng | (combo) | POST `LayDanhSachNhanVienChuaCoHHDoiTuong` | `..._LayDanhSachNhanVienChuaCoHHDoiTuong` | public | List NV chưa cấu hình HH đối tượng. | EF | — |
| 10 | Xóa HH custom | Xóa | GET `XoaHH/{id:Guid}` | `HoaHongNhanVien_XoaHH` | public | Xóa 1 bản ghi HH custom. | EF | GET cho thao tác xóa |
| 11 | Xóa HH thuộc tính mở rộng | Xóa | GET `XoaHoaHongThuocTinhMoRong?loaiThuocTinh` | `HoaHongNhanVien_XoaHoaHongThuocTinhMoRong` (AD-TONG) | public | Xóa HH theo loại thuộc tính mở rộng. Chỉ AD-TONG. | EF | GET cho thao tác xóa; label desc copy nhầm "Lưu hoa hồng..." |
| 12 | Enum (đối tượng/thời điểm/chia đều) | (combo) | GET `GetListEnumGiaTriDoiTuongTinhHoaHong`, `GetListEnumThoiDiemTinhHoaHong`, `GetListEnumLuaChonChiaDeu` | (không attribute) | anonymous | Trả danh sách enum cấu hình HH. | EnumFunction | KHÔNG có `[SBAuthorize]` → public anonymous |

## Endpoint nội bộ (private / service-to-service)
- [internal] Order gọi `XepNhanVien_LayHoaHongNhanVienCauHinh` (POST, `[PrivateSBAuthorize]`, body `List<DonHangChiTietModel>`) khi xếp NV vào đơn → service tính HH từng NV cho từng mặt hàng.
- [login] `XepNhanVien_LayHoaHongNhanVienCauHinh` (GET, `[SBAuthorize]` login-only, query `loaiDoiTuong/idDoiTuong/idNhanVien/loaiHanhDong`) — biến thể tra cứu 1 NV (overload cùng tên action, khác verb).
- [internal] Order gọi `GetHoaHongApDungByListMaDon` (POST, `[PrivateSBAuthorize]`, `ReqHoaHongTheoDon`) → lấy HH đã áp theo danh sách mã đơn.

## Quy tắc nghiệp vụ (gating / tier / ngưỡng)
Logic tính HH cho đơn (`HoaHongNhanVienLogicService.cs`):
1. **Gating quyền xem HH:** nếu `userLogin.IsQLDV()` hoặc các vai Admin (AdminChiNhanh/NhieuChiNhanh/Tong/HeThong) → được lấy HH của TẤT CẢ nhân viên trong đơn; ngược lại chỉ lấy HH của chính `userLogin.Id` (lọc `lstIdNhanVien`).
2. **Nạp cấu hình:** load HH của các NV liên quan trong (`IdDonVi`,`IdChiNhanh`), `IdLoaiObject ∈ lstIdObj`, và `IdNhanVien == Guid.Empty` (HH mặc định) HOẶC `IdNhanVien ∈ lstIdNhanVien`.
3. **Map đối tượng nạp thẻ → thẻ:** `LoaiDoiTuong == NapThe` được quy về `The` (nạp thẻ ăn chung HH thẻ).
4. **Chọn cấu hình áp dụng cho từng NV:** ưu tiên HH theo NV (`IdNhanVien == NV` + đúng `LoaiObject`+`IdLoaiObject`); fallback HH mặc định (`IdNhanVien == Guid.Empty`) khi NV chưa có cấu hình riêng.
5. Bản ghi mặc định = `IdNhanVien == Guid.Empty`; đây là invariant phân biệt "mặc định nhóm/đối tượng" với "HH riêng từng NV" — không được phá khi lưu.

## Luồng xuyên module
- Tạo/xếp đơn (Order) → gọi internal `XepNhanVien_LayHoaHongNhanVienCauHinh(List<DonHangChiTietModel>)` → service tính HH từng NV cho từng mặt hàng (theo quy tắc gating + mặc định ở trên) → trả về đơn đã gắn HH. Khi cần đối soát: Order gọi `GetHoaHongApDungByListMaDon`.

## Quan hệ
- **Gọi sang:** không gọi RA service khác (toàn EF nội bộ Branch + enum).
- **Dữ liệu (entities):** `HoaHongNhanVien` (HH theo NV/đối tượng, `IdNhanVien=Guid.Empty`=mặc định), `CaiDatHoaHongMacDinh` (HH tối đa), `NhomHoaHong`.
- **Ripple/đồng bộ:** là điểm cấu hình + nguồn logic tính HH cho pipeline đơn hàng (Order gọi vào).

## Lỗi đã biết / lưu ý kỹ thuật
- `HoaHongNhanVienController.cs:292,303,319` — 3 endpoint enum KHÔNG có `[SBAuthorize]` → truy cập anonymous.
- `HoaHongNhanVienController.cs:277,335` — `XoaHH` và `XoaHoaHongThuocTinhMoRong` dùng HTTP GET cho thao tác xóa.
- `HoaHongNhanVienController.cs:336` — label/desc của `XoaHoaHongThuocTinhMoRong` copy nhầm "Lưu hoa hồng đối tượng mặc định" (mô tả sai, key vẫn đúng).
- `HoaHongNhanVienController.cs:346,354` — 2 overload `XepNhanVien_LayHoaHongNhanVienCauHinh` cùng tên action, phân biệt verb POST(internal)/GET(login) → khi map route/grep cần lưu ý.
