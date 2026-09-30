---
controller: CongViecBaoCaoController
module: cong-viec
service: WorkFlow
man_hinh_ui: "Công việc > Báo cáo (dashboard công việc): card tổng quan (tổng/quá hạn/theo trạng thái), báo cáo theo người dùng, báo cáo theo mức độ ưu tiên"
muc_dich: "Báo cáo thống kê công việc thuần-đọc qua EF: tổng quan trạng thái + quá hạn, gom theo người dùng, gom theo mức độ ưu tiên (lọc theo khoảng NgayTao)."
alias: [CongViecBaoCao, "báo cáo công việc", "dashboard công việc", "thống kê công việc", "công việc quá hạn"]
auth: internal
state_machine: []
deprecated: false
entities: [CongViec, CongViecUser, User, DmMucDoUuTien, DmTrangThai]
stored_procs: []
depends: [ICongViecBaoCaoService]
cross_module_in: []
cross_module_out: []
---

# CongViecBaoCao — Báo cáo thống kê công việc (read-only, EF)

> Controller VIEW/REPORT thuần đọc bằng **EF** (không SP). KHÔNG có `[KiemTraTienIch]`. Mọi truy vấn scope `IdDonVi == userLogin.DonViId && IdChiNhanh == userLogin.IdChiNhanh` (cố định theo chi nhánh user) + lọc khoảng `NgayTao` (TuNgay/DenNgay). "Quá hạn" = `NgayBatDauDuKien + Estimate(giờ) < now`.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Báo cáo tổng quan | Card tổng quan | `POST api/CongViecBaoCao/TongQuan` | `[SBAuthorize]` | internal | dashboard | Trả TongSo, QuaHan (estimate+ngày bắt đầu < now), TheoTrangThai (đếm theo DmTrangThai active, ẩn nhóm 0). | EF | Quá hạn tính theo `NgayBatDauDuKien + Estimate giờ`, KHÔNG dùng NgayKetThucDuKien → CV chỉ có hạn kết thúc mà thiếu estimate sẽ không bị tính quá hạn. |
| 2 | Báo cáo theo người dùng | Lưới theo người dùng | `POST api/CongViecBaoCao/TheoNguoiDung` | `[SBAuthorize]` | internal | dashboard | Join CongViecUser → group theo user, mỗi user có TongSo + TheoTrangThai, sort TongSo DESC. | EF | 1 CV nhiều người thực hiện → đếm trùng giữa các user (TongSo từng user, không phải distinct CV). |
| 3 | Báo cáo theo mức độ ưu tiên | Biểu đồ theo ưu tiên | `POST api/CongViecBaoCao/TheoMucDoUuTien` | `[SBAuthorize]` | internal | dashboard | GroupJoin DmMucDoUuTien → gom theo mức độ (null → "Chưa phân loại"/"Chưa phân loại"), mỗi nhóm có TheoTrangThai, sort TongSo DESC. | EF | — |

## Quan hệ
- **Gọi sang:** `ICongViecBaoCaoService`.
- **Dữ liệu (entities):** đọc `CongViec` (+ `CongViecUser` cho báo cáo theo người, `User` enrich tên/avatar, `DmMucDoUuTien`/`DmTrangThai` map nhãn). KHÔNG ghi.
- **Ripple/đồng bộ:** KHÔNG có (read-only).

## Lỗi đã biết / lưu ý kỹ thuật
- `CongViecBaoCaoService.cs:208-211` (`BuildQuery`) — scope cứng theo `IdChiNhanh` của user → admin tổng KHÔNG xem được số liệu toàn đơn vị (chỉ chi nhánh mình), khác convention một số báo cáo khác.
- `CongViecBaoCaoService.cs:55-56` — "quá hạn" chỉ dựa NgayBatDauDuKien+Estimate (giờ), bỏ qua NgayKetThucDuKien.
- Controller KHÔNG có `[KiemTraTienIch]` (khác các controller khác cùng module) — chỉ cần đăng nhập.
