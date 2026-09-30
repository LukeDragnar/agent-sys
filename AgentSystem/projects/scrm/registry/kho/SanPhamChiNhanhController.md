---
controller: SanPhamChiNhanhController
module: kho
service: WarehouseProduct
man_hinh_ui: "Không có màn riêng — endpoint phục vụ màn Combo SPA (combo-spa-edit / combo-spa-chinhanh-edit): khi chọn nhóm SP, load SP của chi nhánh hiện tại theo danh mục"
muc_dich: "Tra cứu sản phẩm RIÊNG theo chi nhánh (bảng map SanPhamChiNhanh): lấy SP theo danh mục lọc đúng IdChiNhanh user, và tìm grid bảng map (chưa dùng UI)."
alias: ["sản phẩm chi nhánh", "sp riêng chi nhánh", "sp theo chi nhánh tạo", "lấy sp theo nhóm theo chi nhánh", "danh mục sp chi nhánh"]
auth: public
state_machine: []
deprecated: false
entities: [SanPhamChiNhanh, SanPham]
stored_procs: []
depends: [WarehouseProduct]
cross_module_in: [ComBoSpa (combo-spa-edit / combo-spa-chinhanh-edit gọi LayTheoDanhMucSanPham qua FE layTheoNhomSanPham)]
cross_module_out: []
---

# SanPhamChiNhanhController — Sản phẩm riêng theo chi nhánh

> Controller nhỏ (`partial class`, 2 endpoint). Đừng nhầm với `SanPhamController` (#SP cấp tổng + SP chi nhánh tạo). Đây thao tác trên bảng map **`SanPhamChiNhanh`** (IdSanPham ↔ IdChiNhanh ↔ IdDonVi) — tập SP được gán/khả dụng cho 1 chi nhánh cụ thể. Cùng tiện ích `MaTienIchSCRM.TI_Kho`.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lấy SP theo danh mục (chi nhánh hiện tại) | (ngầm) — combo-spa chọn nhóm SP | POST `api/SanPhamChiNhanh/LayTheoDanhMucSanPham` | `SanPhamChiNhanh_LayTheoDanhMucSanPham` | public | combo-spa edit: khi chọn nhóm SP | Body = `List<Guid>` ids nhóm SP. Join `SanPhamChiNhanh` (IdChiNhanh = `CurrentUser.IdChiNhanh`, !IsDelete) × `SanPham` (!IsDelete, SuDung=true, IdNhomSanPham ∈ ids) → trả `List<SanPhamDto>` (Id/Ten/Gia/Ma/...). Lọc ĐÚNG chi nhánh đăng nhập | EF (2 bảng join) | Throw `ArgumentException("Thông tin đăng nhập không hợp lệ")` nếu `CurrentUser == null`; mọi Exception bị bọc lại thành `ArgumentException(ex.Message)` (mất stacktrace gốc) |
| 2 | Tìm bảng map theo điều kiện (grid) | (chưa nối UI) | POST `api/SanPhamChiNhanh/TimTheoDieuKien` | `SanPhamChiNhanh_TimTheoDieuKien` | public | — | Paging bảng map `SanPhamChiNhanh` → `SanPhamChiNhanhDto`. **Không có filter/keyword thật** (chỉ skip/take + order NgayTao), total = Count toàn bảng | EF | filter rỗng: cả 2 nhánh if/else `Sorts` đều `OrderBy(NgayTao)` (no-op); không lọc IdDonVi/IdChiNhanh → trả cross-tenant nếu được gọi. Hiện FE chưa gọi endpoint này |

## Quy tắc nghiệp vụ
- `LayTheoDanhMucSanPham` khóa cứng theo `CurrentUser.IdChiNhanh` (không nhận tham số chi nhánh từ client) → chỉ trả SP thuộc đúng chi nhánh của user đăng nhập. Đây là điểm khác biệt cốt lõi với `SanPham/LayTheoNhomSanPham` (cấp tổng/đơn vị).

## Quan hệ
- **Gọi sang:** không (chỉ repository `SanPhamChiNhanh` + `SanPham` trong cùng service WarehouseProduct).
- **Dữ liệu (entities):** `SanPhamChiNhanh` (bảng map IdSanPham/IdChiNhanh/IdDonVi, kế thừa BaseEntity), `SanPham` (lấy thông tin SP để build `SanPhamDto`).
- **Ripple / đồng bộ:** không phát event/consumer. Chỉ đọc (read-only ở 2 endpoint hiện có; `UpdateAsync` override sẵn trong service nhưng controller chưa expose).

## Lỗi đã biết / lưu ý kỹ thuật
- **`TimTheoDieuKien` chưa lọc gì** (`SanPhamChiNhanhService.cs:57-74`): bỏ qua filter, `total = query.Count()` toàn bảng, không lọc `IdDonVi`/`IdChiNhanh` → nếu nối UI cần thêm tenant filter để tránh lộ chéo đơn vị. Hiện FE không gọi (`api/SanPhamChiNhanh/TimTheoDieuKien` không xuất hiện trong scrm_frontend).
- **Catch nuốt lỗi** (`SanPhamChiNhanhController.cs:54-57`): `LayTheoDanhMucSanPham` bọc mọi Exception thành `ArgumentException(ex.Message)` → mất loại lỗi gốc + stacktrace, triage khó.
- **FE entrypoint**: method FE `layTheoNhomSanPham(ids)` (`services/warehouseproduct/sanPham.service.ts:40-43`) trỏ tới endpoint #1; được gọi từ `combo-spa-edit.component.ts:818` và `combo-spa-chinhanh-edit.component.ts` khi chọn nhóm SP. Lưu ý: tên method FE là `layTheoNhomSanPham` (giống bên SanPham) nhưng URL là `SanPhamChiNhanh/LayTheoDanhMucSanPham`.
- **Service-side caller**: chỉ tìm thấy 2 file BE chứa `LayTheoDanhMucSanPham` (controller + service của chính WarehouseProduct) → không có service nào khác gọi private sang. Cạnh cross-module duy nhất đi qua FE từ màn Combo SPA.
