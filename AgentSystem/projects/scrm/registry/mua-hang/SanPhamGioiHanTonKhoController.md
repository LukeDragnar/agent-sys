---
controller: SanPhamGioiHanTonKhoController
module: mua-hang
service: WarehouseProduct
man_hinh_ui: "Kho/Mua hàng > màn 'Giới hạn tồn kho' (/warehouse-product/gioi-han-ton-kho). Lưới list + cột Tồn kho tối thiểu/tối đa (sửa nội dòng → nút Cập nhật). Nút: Thêm mới (popup), Khởi tạo (popup), Nhập Excel (popup import). Lọc: Nhóm SP, Sản phẩm, Chi nhánh, Keyword."
muc_dich: "Cấu hình ngưỡng giới hạn tồn kho theo (sản phẩm × chi nhánh): tồn tối thiểu/tối đa/khẩn cấp + số ngày tồn tương ứng + SL bán TB nhỏ nhất + khoảng ngày tính giá trị TB. Là input cho cảnh báo dự báo hết hàng / dự kiến thêm hàng."
alias: ["giới hạn tồn kho", "gioi han ton kho", "tồn tối thiểu", "tồn kho tối thiểu", "ngưỡng tồn kho", "tồn kho khẩn cấp", "dự báo hết hàng", "dự kiến thêm hàng", "cảnh báo tồn kho", "min max tồn kho"]
auth: public
state_machine: []
deprecated: false
entities: [SanPhamGioiHanTonKho, SanPham, ChiNhanh]
stored_procs: [PRC_SCRM_WarehouseProduct_GioiHanTonKho_Search]
depends: [CommonService/GetAllChiNhanh, CommonService/DropdownSanPham, EFCore.BulkExtensions (BulkInsert/BulkUpdate), SqlSugar (search SP), GemBox.Spreadsheet (import/template)]
cross_module_in: []
cross_module_out: []
---

# SanPhamGioiHanTonKhoController — Cấu hình giới hạn tồn kho (min/tối đa/khẩn cấp theo SP × chi nhánh)

> CRUD cấu hình thuần: 1 bản ghi = 1 cặp (IdSanPham × IdChiNhanh), gắn IdDonVi (multi-tenant). Mọi ghi (Create/Update/Initial/Import) đều quy về `CreatePrivate` = upsert theo (IdSanPham, IdChiNhanh): tồn tại → BulkUpdate (giữ NgayTao/NguoiTao), chưa có → BulkInsert. **Phân quyền chi nhánh ở tầng service**: user KHÔNG phải admin bị ép `IdChiNhanh = userLogin.IdChiNhanh` và `IsApDungTatCaChiNhanh=false` (chỉ thao tác chi nhánh của mình). Lưới đọc qua stored proc `PRC_SCRM_WarehouseProduct_GioiHanTonKho_Search`; các thao tác ghi đi EF/Bulk. **Controller hằng `[SBAuthorize(key,...)]` với role mặc định `DV-AD-TONG, DV-AD-CN, DV-MCN`.**

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm kiếm / load lưới | Lưới list + bộ lọc (Nhóm SP, SP, Chi nhánh, Keyword) | `POST api/SanPhamGioiHanTonKho/SearchByConditions` | `SanPhamGioiHanTonKho_SearchByConditions` | public | màn /warehouse-product/gioi-han-ton-kho | Non-admin ép `ListChiNhanh=[user.IdChiNhanh]`. Build `SugarParameter` (IdDonVi, ListIdChiNhanhStr nối `;`, IdSanPham, IdNhomSanPham, Keyword, paging, OrderBy mặc định `NgayCapNhat DESC`). Trả grid + total; enrich TenChiNhanh (GetAllChiNhanh) + default null→0/1 cho các cột số. | **SP** `PRC_SCRM_WarehouseProduct_GioiHanTonKho_Search` (table0=data, table1=total) | (a) `if (... \|\| true)` L627 → nhánh log objectLog luôn chạy (điều kiện vô nghĩa). (b) `catch` trả lỗi rồi còn `throw;` chết sau return (dead code). |
| 2 | Thêm mới (nhiều dòng) | Popup "Thêm mới giới hạn tồn kho" / Lưu | `POST api/SanPhamGioiHanTonKho/Create` | `SanPhamGioiHanTonKho_Create` | public | nút "Thêm mới" | `CreateOrUpdate(CREATE)`. Validate từng dòng (≤100 dòng): các trường tồn/số ngày/SL bán TB không null, không âm, ToiThieu≤ToiDa, KhoangNgay∈enum [1..13], IdSanPham bắt buộc. `IsApDungTatCaChiNhanh=true` → nhân bản cho tất cả chi nhánh đơn vị; chống trùng (SP×CN) trong batch. Non-admin ép về chi nhánh mình. Upsert qua `CreatePrivate` (isImport=false → không quy đổi ĐVT). | **EF** BulkInsert/BulkUpdate (`SanPhamGioiHanTonKho`) | Lỗi insert/update bị nuốt trong `try/catch{}` rỗng; nếu total != listData.Count → trả "Chưa hoàn thành tiến độ N/M". |
| 3 | Cập nhật (sửa nội dòng + nút) | Cột Tồn tối thiểu/tối đa sửa trực tiếp → nút "Cập nhật" | `POST api/SanPhamGioiHanTonKho/Update` | `SanPhamGioiHanTonKho_Update` | public | có dòng trên lưới | `CreateOrUpdate(UPDATE)`. Ép `IsApDungTatCaChiNhanh=false`, bắt buộc `IdChiNhanh` (thiếu → lỗi "Thiếu thông tin chi nhánh!"), `ListChiNhanh=[IdChiNhanh]`. Validate giống #2. Upsert theo (SP×CN). FE chỉ gửi idSanPham/idChiNhanh/tonKhoToiThieu/tonKhoToiDa (Root đã quy đổi ĐVT phía FE). | **EF** BulkInsert/BulkUpdate | FE `Update` payload KHÔNG gửi các trường SoNgay*/TonKhoKhanCap/SoLuongBanTB/KhoangNgay → service vẫn validate "không null" các trường này (L91-184) → **Update từ lưới luôn fail validate** vì payload thiếu (chỉ truyền 4 field). Cần kiểm chứng lại (rủi ro lệch FE↔BE). |
| 4 | Xóa 1 bản ghi | Nút "Xóa" / confirm "Bạn có chắc..." | `POST api/SanPhamGioiHanTonKho/Delete/{id:Guid}` | `SanPhamGioiHanTonKho_Delete` | public | mỗi dòng lưới | Tìm theo Id; khác `userLogin.DonViId` → "Bạn không có quyền xóa!"; non-admin & khác chi nhánh → từ chối. OK → `DeleteAsync(data, true)` (hard delete). | **EF** `SanPhamGioiHanTonKho` | Delete là xóa cứng (không soft-delete dù entity có cờ IsDelete). |
| 5 | Khởi tạo hàng loạt | Popup "Khởi tạo giới hạn tồn kho" / Lưu | `POST api/SanPhamGioiHanTonKho/Initial` | `SanPhamGioiHanTonKho_Initial` | public | nút "Khởi tạo" | `Initial`. Validate cấu hình 1 lần (như #2). Quét toàn bộ SP của đơn vị (paging 400), lọc theo Nhóm SP nếu `IsTatCaSanPham!=true`. Mỗi SP nhân theo chi nhánh (all/chỉ định) → upsert `CreatePrivate(isImport=true)`. Non-admin ép chi nhánh mình. | **EF** đọc `SanPham` + BulkInsert/BulkUpdate | (a) L577 `soBanGhiDaHoanThanh += soBanGhiDaHoanThanh;` cộng chính nó (luôn 0) → biến đếm vô dụng (nhưng kết quả vẫn trả "Hoàn thành!" cứng). (b) Initial luôn trả success kể cả khi mọi batch fail. |
| 6 | Tải template import | (trong popup Nhập Excel) nút tải mẫu | `POST api/SanPhamGioiHanTonKho/DownloadFileTemplateImport` | `SanPhamGioiHanTonKho_DownloadFileTemplateImport` | public | popup import | Load `Template/ImportSanPhamGioiHanTonKho_Template.xlsx`, bind dropdown SP + Chi nhánh (non-admin chỉ chi nhánh mình) vào sheet "Store". File `ImportSanPhamGioiHanTonKho.xlsx`. Max 2000 dòng/lần. | File template (GemBox) | messErr nội bộ bị nuốt (chỉ set biến, không trả). |
| 7 | Import Excel | Popup "Nhập Excel" / chọn file | `POST api/SanPhamGioiHanTonKho/Import` | `SanPhamGioiHanTonKho_Import` | public | popup import | Đọc 11 cột từ dòng 4. Validate từng dòng (SP theo TÊN, chi nhánh theo TÊN, các trường số, KhoangNgay map theo text "1 Ngày trước"/... ); `ApDungTatCaChiNhanh` chỉ admin, giá trị hợp lệ duy nhất `AP_DUNG`. Non-admin gắn chi nhánh mình. Dòng OK → upsert `CreatePrivate(isImport=true)` (có quy đổi ĐVT theo BangQuyDoiJson); dòng lỗi → xuất file lỗi. Trả "Đã import N, M lỗi" + Id file lỗi. | **EF** BulkInsert/BulkUpdate; file lỗi lưu `wwwroot/ImportFail/SanPhamGioiHanTonKho/{user}` | (a) `SaveDataWrongImport` L703/704 ghi 2 lần `SoNgayTonKhoToiDa` (cột 7 & 8) → cột "Số ngày tối thiểu" trong file lỗi sai. (b) Text KhoangNgay so khớp ("1 Ngày trước") khác Description enum BE ("1 ngày trước", thường) → lệ thuộc đúng template, dễ lệch hoa/thường. |
| 8 | Tải file dòng import lỗi | nút tải file lỗi (sau import) | `GET api/SanPhamGioiHanTonKho/DownloadDataWrong/{id}` | `SanPhamGioiHanTonKho_DownloadDataWrong` | public | sau khi import có lỗi | Đọc file đã lưu theo `{id}` trong `wwwroot/ImportFail/.../{user}`, trả `DataImportSanPhamGioiHanTonKhoFail.xlsx`. | File (GemBox) | — |
| 9 | Dropdown khoảng ngày tính TB | combobox "Khoảng ngày tính giá trị TB" (popup create/initial) | `POST api/SanPhamGioiHanTonKho/GetListKhoangNgayTinhGiaTriTrungBinh` | `SBAuthorize()` (any login) | public | popup create/initial | Trả list `EnumKhoangNgayTinhGiaTriTrungBinh` (13 mục: 1/3/5/7/30 ngày trước, tuần/tháng/quý trước, quý hiện tại, *NamTruoc...). | enum (in-memory) | `[SBAuthorize()]` trống — chỉ cần đăng nhập. |

## Quy tắc nghiệp vụ (gating / tier / ngưỡng)
- **Phân quyền chi nhánh (mọi endpoint ghi/đọc):** `FunctionCommon.CheckUserLoginIsRoleAdmin(userLogin)==false` → ép `IsApDungTatCaChiNhanh=false` + chỉ `userLogin.IdChiNhanh`. Admin mới được `IsApDungTatCaChiNhanh=true` (nhân ra mọi chi nhánh đơn vị).
- **Validate ngưỡng (Create/Update/Initial):** mỗi field (TonKhoToiThieu/ToiDa/KhanCap, SoNgayTonKho*, SoLuongBanTrungBinhNhoNhat, KhoangNgayTinhGiaTriTrungBinh) → null=lỗi, <0=lỗi; `TonKhoToiThieu>TonKhoToiDa`=lỗi; `SoNgayTonKhoToiThieu>SoNgayTonKhoToiDa`=lỗi; `KhoangNgay` ∉ [MotNgayTruoc(1) .. QuyTiepTheoNamTruoc(13)] = lỗi.
- **Giới hạn batch:** Create ≤ 100 dòng; Import ≤ 2000 dòng/file.
- **Upsert key:** (IdDonVi, IdSanPham, IdChiNhanh). Trùng trong cùng batch → chặn "Trước đó đã có bản ghi trùng sản phẩm và chi nhánh!".
- **Quy đổi ĐVT (chỉ isImport=true):** nếu SP `IsSuDungQuyDoiDonViTinh` + có `BangQuyDoiJson` → nhân `TonKhoToiDa/ToiThieu` theo `SoLuongToDvNho` về đơn vị nhỏ. (Create thường isImport=false → KHÔNG quy đổi.)

## Quan hệ
- **Gọi sang:** `CommonService.GetAllChiNhanh` (map/nhân chi nhánh), `CommonService.DropdownSanPham` (template/import), SqlSugar SP search, EFCore.BulkExtensions (ghi).
- **Dữ liệu (entities):** ghi `SanPhamGioiHanTonKho` (kế thừa `BaseEntity`: Id, NgayTao/CapNhat, NguoiTao/CapNhat, IsDelete). Đọc `SanPham` (Initial/quy đổi). Tham chiếu ChiNhanh (qua CommonService, không entity nội bộ).
- **Ripple / đồng bộ:** KHÔNG bắn message/event. Là bảng cấu hình ngưỡng — được các báo cáo/cảnh báo tồn kho (dự báo hết hàng / dự kiến thêm hàng) đọc tham chiếu, nhưng controller này không tự đẩy đi đâu.

## Lỗi đã biết / lưu ý kỹ thuật
- BE `WarehouseProduct/WarehouseProduct.API/Controllers/SanPhamGioiHanTonKhoController.cs`
  - L110 `GetListKhoangNgayTinhGiaTriTrungBinh` = `[SBAuthorize()]` trống; các endpoint còn lại có key + role `DV-AD-TONG, DV-AD-CN, DV-MCN`.
- BE `WarehouseProduct/WarehouseProduct.Service/Generated/SanPhamGioiHanTonKhoService.cs`
  - L627 `if (userLogin.IsSuperUser || (userLogin.IsCSKH ?? false) || true)` → luôn true (điều kiện thừa).
  - L664-665: `return ...ResultWithError(...)` rồi `throw;` ngay sau → `throw` là dead code.
  - L577 (Initial) `soBanGhiDaHoanThanh += soBanGhiDaHoanThanh;` (cộng chính nó, đáng lẽ `+= total`) → biến đếm luôn 0; Initial vẫn trả "Hoàn thành!" cứng dù batch có thể fail (try/catch{} rỗng).
  - L262-272 / L347-364: ghi bằng Bulk trong `try/catch{}` rỗng → lỗi DB bị nuốt; Create dựa vào `soBanGhiDaHoanThanh != listData.Count` để báo "Chưa hoàn thành tiến độ".
  - Update (#3): service validate đầy đủ mọi field nhưng FE `onUpdate` chỉ gửi 4 field (idSanPham/idChiNhanh/tonKhoToiThieu/tonKhoToiDa) → **payload thiếu các trường bắt buộc → Update lưới dễ trả lỗi validate**; cần verify lại hợp đồng FE↔BE.
- BE `WarehouseProduct/WarehouseProduct.Service/Generated/Imports/SanPhamGioiHanTonKhoImportService.cs`
  - L703-704: `SaveDataWrongImport` set cột 7 và cột 8 cùng `SoNgayTonKhoToiDa` (quên `SoNgayTonKhoToiThieu`) → file lỗi hiển thị sai cột "Số ngày tồn kho tối thiểu".
  - Map `KhoangNgayTinhGiaTriTrungBinh` theo chuỗi hoa "1 Ngày trước"/... (L496+) khác `[Description]` enum thường "1 ngày trước" → phụ thuộc text template, lệch hoa/thường sẽ báo "không hợp lệ".
- DTO/enum: `Libs/Models.Core/DTO/WarehouseProduct.Client/SanPham/SanPhamGioiHanTonKhoDto.cs` (Dto/InitialDto/Filter), `Libs/Models.Core/Enum/Warehouse/WarehouseEnum.cs:145` (`EnumKhoangNgayTinhGiaTriTrungBinh` 1..13).
- FE `scrm_frontend/src/app/pages/WarehouseProduct/gioi-han-ton-kho/` (list + create/initial/import popup); service `services/warehouseproduct/gioi-han-ton-kho.service.ts` → `SERVICE_API_WarehouseProduct + api/SanPhamGioiHanTonKho`; route `warehouse-product/gioi-han-ton-kho` (`warehouseProduct-routing.module.ts:212`).
