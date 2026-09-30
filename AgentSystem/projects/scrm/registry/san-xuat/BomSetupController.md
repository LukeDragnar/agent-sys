---
controller: BomSetupController
module: san-xuat
service: SanXuat
man_hinh_ui: "Sản xuất > Quản lý danh mục > BOM (định mức / công thức sản phẩm) — khai báo công thức NVL + quy trình các công đoạn để sản xuất 1 sản phẩm"
muc_dich: "Quản lý BOM (Bill of Materials = công thức/định mức): header BomSetup (IdSanPham, SoLuongTheoCongThuc, KichThuoc, IsMacDinh) + quy trình BomStep (các bước/công đoạn) + BomProduct (NVL đầu vào Loai=0 / kết quả đầu ra Loai=1 mỗi bước). Hỗ trợ clone BOM và cập nhật toàn bộ quy trình."
alias: ["BOM", "định mức", "công thức sản phẩm", "bom setup", "BomSetup", "quy trình BOM", "clone BOM", "bill of materials"]
auth: public                                   # controller [ApiController]; CRUD có key riêng; CloneBomSetup để [SBAuthorize] rỗng
state_machine: []
deprecated: false
entities: [BomSetup, BomStep, BomProduct]
stored_procs: []
depends: [SanXuat, IGenCodeService, IWarehouseProductService (inject nhưng phần dùng bị comment), DmCongDoan (công đoạn — master data, step mang Ten/ThuTu)]
cross_module_in: [WorkOrderController (đọc BomSetup/BomStep/BomProduct khi ChuyenWOPlanner vật chất hóa quy trình), SaleOrderProductionController (đọc LotSize/KichThuoc)]
cross_module_out: []
---

# BomSetupController — BOM / Định mức công thức sản phẩm

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Thêm BOM | Thêm Bom | POST `api/BomSetup/ThemMoi` | `BomSetup_ThemMoi` (AD-TONG/AD-CN/SANXUAT) | public | màn BOM | Map DTO→entity; set org/branch; nếu `IsMacDinh` → bỏ cờ mặc định ở BOM khác cùng sản phẩm; InsertAsync(CurrentUser) | EF | InsertAsync (BomSetupService.cs:45) |
| 2 | Sửa BOM | Chỉnh sửa thông tin | POST `api/BomSetup/CapNhatThongTin` | `BomSetup_CapNhatThongTin` | public | BOM cùng đơn vị + chi nhánh | GetById null → "Không tìm thấy"; **chặn nếu `IdDonVi`≠user.DonViId hoặc `IdChiNhanh`≠user.IdChiNhanh** (trả NotFound); cập nhật field rồi UpdateAsync | EF | tenant guard ở controller L78-85 |
| 3 | Xóa BOM | Xóa BOM | POST `api/BomSetup/Xoa/{id}` | `BomSetup_Xoa` | public | - | GetById null → "Không tìm thấy"; DeleteAsync → true | EF | XoaAsync L106 |
| 4 | Tìm theo điều kiện | grid lọc | POST `api/BomSetup/TimTheoDieuKien` | `BomSetup_TimTheoDieuKien` | public | màn list | Phân trang BOM theo filter + CurrentUser | EF | - |
| 5 | Xem chi tiết | mở BOM | GET `api/BomSetup/TimTheoId/{id}` | `BomSetup_TimTheoId` | public | - | Trả 1 BOM theo id | EF | - |
| 6 | Clone BOM | Clone BOM | POST `api/BomSetup/CloneBomSetup/{id}` | `SBAuthorize` (chỉ đăng nhập) | public | - | Deep-copy BomSetup + BomStep + BomProduct với Id mới (remap stepId), sinh mã mới, tên "- Copy" | EF | CloneBomSetup (:58); key thật bị comment |
| 7 | Cập nhật quy trình BOM | Tạo quy trình BOM | POST `api/BomSetup/CapNhatQuyTrinh` | `BomSetup_CapNhatQuyTrinh` | public | màn quy trình | **Xóa toàn bộ BomStep+BomProduct rồi insert lại** danh sách step mới + NVL đầu vào (Loai=0) / kết quả đầu ra (Loai=1); transaction | EF | CapNhatQuyTrinh (:300) — replace-all |

## Quy tắc nghiệp vụ (blueprint để trace)
1. **IsMacDinh:** mỗi sản phẩm chỉ 1 BOM mặc định — set IsMacDinh BOM này → clear cờ ở BOM khác cùng IdSanPham (BomSetupService.cs:50).
2. **Tenant guard (chỉ ở CapNhatThongTin):** sửa BOM khác đơn vị/chi nhánh → trả NotFound (controller L78-85). Lưu ý Xoa/CloneBomSetup KHÔNG có guard này.
3. **CapNhatQuyTrinh = delete-all-reinsert** BomStep+BomProduct (:315-382) → mọi Id step/product đổi; WO đã vật chất hóa trước đó không tự đồng bộ.
4. **Loai BomProduct:** `0=nguyên liệu/đầu vào`, `1=kết quả/đầu ra`; cờ `IsSanPhamChinh`, `IsBanThanhPham`. Đây là blueprint mà WorkOrder.ChuyenWOPlanner sao chép sang step (scale theo `SoLuong/SoLuongTheoCongThuc`).

## Quan hệ
- **Gọi sang:** IGenCodeService (sinh mã BOM); `IWarehouseProductService` inject nhưng phần dùng bị comment (BomSetupService.cs:232).
- **Dữ liệu (entities):** `BomSetup {IdSanPham, SoLuongTheoCongThuc, KichThuoc, IsMacDinh, IdDonViTinh}`, `BomStep {ThuTu, Ten, time-estimates}`, `BomProduct {Loai, IsSanPhamChinh, IsBanThanhPham, SoLuong}`.
- **Ripple / đồng bộ:** BOM là nguồn cho `WorkOrderController.ChuyenWOPlanner` (vật chất hóa quy trình) và `SaleOrderProductionController` (LotSize/KichThuoc). Đổi BOM **không** retro-active lên WO đã tạo. Không RabbitMQ.

## Lỗi đã biết / lưu ý kỹ thuật
- `DmCongDoan` (công đoạn) là master-data riêng; BomStep mang `Ten`/`ThuTu` chứ **không có FK cứng** tới DmCongDoan trong entity → liên kết lỏng, cần xác nhận có map theo Ten/Ma hay không.
- Tenant guard chỉ áp ở `CapNhatThongTin`; `Xoa` và `CloneBomSetup` thiếu guard đơn vị/chi nhánh → rủi ro thao tác chéo tenant.
- `CloneBomSetup` để `[SBAuthorize]` rỗng (controller L135), key thật bị comment → chỉ cần đăng nhập.
- `CapNhatQuyTrinh` replace-all: chạy lại sẽ tạo step/product Id mới hoàn toàn.
