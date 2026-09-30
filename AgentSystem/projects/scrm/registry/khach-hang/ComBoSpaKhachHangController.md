---
controller: ComBoSpaKhachHangController
module: khach-hang
service: WarehouseProduct
man_hinh_ui: "Quản lý kho > Combo > Combo của khách hàng (route warehouse-product, combo-spa-khachhang.component — list combo KH đã sở hữu, Thêm/Sửa/Xóa + 4 luồng Import: mẫu 1/2/3 + Import update) · KH360 tab 'Số lần combo' (Customer/quanly-khachhang-detail/solan-combo) · dùng ở Thu ngân / Sale / Đặt lịch khi chọn buổi combo để trừ"
muc_dich: "Quản lý combo/gói liệu trình mà KHÁCH HÀNG đã sở hữu (mua) — bản instance của combo gốc gắn vào 1 khách: CRUD, import hàng loạt (3 mẫu + update), và TÁC ĐỘNG ĐỊNH LƯỢNG (cộng/trừ số buổi/số lần còn lại SoLanConLai khi khách dùng dịch vụ/sản phẩm trong combo)."
alias: ["combo khách hàng", "combo của khách", "combo đã mua", "gói khách hàng", "số lần combo", "buổi combo", "trừ buổi", "trừ buổi combo", "combo KH", "gói liệu trình của khách"]
auth: mixed
state_machine: []
entities: [ComBoSpaKhachHang, ComBoSpaKhachHangChiTiet, ComboSpaKhachHangLichSu, ComboSpaKhachHangGiaoDichLichSu]
stored_procs: []
depends: [WarehouseProduct, Customer (KhachHang/TimTheoId), CommonService, ImportComboKhachHang]
cross_module_in: [Order, Customer, Promotion, Thu ngân/Sale/Đặt lịch (FE)]
cross_module_out: [Order (snapshot combo + trừ buổi khi bán/dùng dịch vụ), KH360 (hiển thị số buổi còn lại)]
---

# ComBoSpaKhachHang — Combo/gói của khách hàng (instance đã mua, trừ buổi dần)

> Khác `ComBoSpaController` (đầu combo gốc, master data): controller này quản lý **instance combo gắn 1 khách hàng** sau khi khách mua. Mỗi instance có `MaComboKhachHang` riêng (auto sinh `CB...`), và mỗi dòng chi tiết giữ `SoLan` (số buổi ban đầu) + `SoLanConLai` (số buổi còn lại) → bị **trừ dần** mỗi lần khách dùng.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Thêm combo cho khách | "Thêm mới" → `ThemMoi` | POST `api/ComBoSpaKhachHang/ThemMoi` | `ComBoSpaKhachHang_ThemMoi` | public | màn combo KH | ModelState invalid → MESS_Model_UnValidate. Lấy combo gốc theo IdComboSpa + đơn vị (ko thấy → "Không tìm thấy combo!"), check KH tồn tại (gọi Customer `KhachHang/TimTheoId`), sinh `MaComboKhachHang` (CB+8, unique theo đơn vị), copy SP+DV combo gốc thành chi tiết với `SoLan=SoLanConLai=SoLuong/SoLan` gốc. Insert trong transaction | EF | Message thành công của `Update`/`Delete` bị copy nhầm = "Thêm mới thành công!" (Service:286) |
| 2 | Sửa combo KH (số buổi) | "Lưu" → `CapNhat` | POST `api/ComBoSpaKhachHang/CapNhat` | `ComBoSpaKhachHang_CapNhat` | public | per-row edit | Cập nhật `SoLanConLai`/`SoLan` từng dòng chi tiết (SP+DV). Chặn `SoLanConLai > SoLan` → "Số lần còn lại ... không được lớn hơn số lần ban đầu!". MarkDirty(SoLanConLai) | EF | - |
| 3 | Xóa combo KH | "Xóa" → `Xoa` | GET `api/ComBoSpaKhachHang/Xoa/{id}` | `ComBoSpaKhachHang_Xoa` | public | per-row, confirm | Soft-delete instance combo của khách (IsDelete) | EF | Message trả về cũng = "Thêm mới thành công!" (copy nhầm) |
| 4 | Chi tiết combo KH theo id | mở popup chi tiết | GET `api/ComBoSpaKhachHang/GetDetailById/{id}` | `ComBoSpaKhachHang_GetDetailById` | public | per-row | Trả `ComBoSpaKhachHangDto` (qua CommonService.GetDetailComboKhachHang) | EF | - |
| 5 | List combo theo id khách | tab combo trong hồ sơ KH | GET `api/ComBoSpaKhachHang/GetListComBoByIdKhachHang/{id}` | `ComBoSpaKhachHang_GetListComBoByIdKhachHang` (gồm DV-SALE, DV-MKT, DV-NV) | public | có idKhachHang | Trả list combo + chi tiết SP/DV (kèm SoLan/SoLanConLai) khách đang sở hữu | EF | - |
| 6 | List combo KH theo điều kiện | "Tìm kiếm" lưới combo KH | POST `api/ComBoSpaKhachHang/TimTheoDieuKien` | `ComBoSpaKhachHang_TimTheoDieuKien` | public | màn list combo KH | Lọc theo keyword + `SoLanSuDungDichVuConLai` (join chi tiết: >= / == / <= theo cờ) + phân trang; join Customer lấy tên KH | EF | - |
| 7 | List combo KH (paging, đa role) | lưới combo KH (KH360/báo cáo) | POST `api/ComBoSpaKhachHang/GetListComBoByIdKhachHangFilterDto` | `ComBoSpaKhachHang_GetListComBoByIdKhachHangFilterDto` (full role: AD/KT/SALE/MKT/NV/HT-AD...) | public | có IdKhachHang | IdKhachHang rỗng → "Id khách hàng là bắt buộc". Trả list phân trang theo filter | EF | - |
| 8 | List combo KH dạng Pt | (màn PT/chọn buổi) | POST `api/ComBoSpaKhachHang/GetListComBoByIdKhachHangPt` | `ComBoSpaKhachHang_GetListComBoByIdKhachHangPt` (gồm SALE/MKT/NV) | public | - | Biến thể list combo KH cho luồng PT | EF | - |
| 9 | Full thông tin combo KH theo id | popup chi tiết đầy đủ | POST `api/ComBoSpaKhachHang/LayThongFullThongTinComboKhachHangById` | `ComBoSpaKhachHang_LayThongFullThongTinComboKhachHangById` | public | có id | Cần id combo KH (rỗng → "Cần truyền id..."), ko thấy → "Không tìm thấy combo!". Trả full info theo CurrentUser | EF | - |
| 10 | Chi tiết 1 dòng combo KH | (Order chọn buổi để bán) | GET `api/ComBoSpaKhachHang/GetComboSpaKhDetail/{id}` | `ComBoSpaKhachHang_GetComboSpaKhDetail` | public | id = IdComBoSpaKhachHangChiTiet | Trả `ComBoSpaKhachHangChiTietDto` (1 buổi/1 dịch vụ) — **Order gọi sang** khi dựng đơn từ buổi combo | EF | - |
| 11 | **Cộng/Trừ buổi (1 hàng/lần)** | (thu ngân/đặt lịch trừ buổi) | POST `api/ComBoSpaKhachHang/TacDongDinhLuong_Single` | `ComBoSpaKhachHang_TacDongDinhLuong_Single` | public | combo còn hạn | TrangThai=1 cộng / =2 trừ `SoLanConLai` của 1 dòng (theo IdHang=IdDichVu). Trừ < 0 → "Số đơn vị đã đạt giới hạn!"; cộng > SoLan → "vượt quá số đơn vị ban đầu!" | EF | **BUG check hạn ngược**: `if (HanSuDung > DateTime.Now) → "Combo đã hết hạn"` → chặn nhầm combo CÒN hạn, cho qua combo HẾT hạn (Service:754,783) |
| 12 | **Cộng/Trừ buổi (nhiều hàng/lần)** | (trừ nhiều dịch vụ 1 lần) | POST `api/ComBoSpaKhachHang/TacDongDinhLuong_Multi` | `ComBoSpaKhachHang_TacDongDinhLuong_Multi` | public | combo còn hạn | Như #11 nhưng loop `ListHangHoa` | EF | Cùng BUG check hạn ngược (#11). `dichVu` ko null-check → IdHang sai/ko thuộc combo → NullReferenceException (Service:756,788) |
| 13 | Lấy combo KH theo list id | (gọi nội bộ/màn chọn) | POST `api/ComBoSpaKhachHang/GetListComboKhByIds` · POST `GetListComboByIds` | `[SBAuthorize()]` (chỉ cần đăng nhập, không key riêng) | public | - | Trả list combo KH theo danh sách id | EF | 2 endpoint chỉ dùng `[SBAuthorize()]` rỗng — không gắn permission key riêng |
| 14 | Import combo KH (3 mẫu + update) | "Import" → 4 luồng | POST `ImportComBoSpaKhachHang` · `ImportComBoSpaKhachHangMau2` · `ImportComBoSpaKhachHangMau3` · `ImportUpdateComBoSpaKhachHang` (IFormFile) | `ComBoSpaKhachHang_ImportComBoSpaKhachHang` / `_ImportComBoSpaKhachHangMau2` / `_Mau3` / `_ImportUpdateComBoSpaKhachHang` | public | popup import | Đẩy Excel → tạo/cập nhật combo KH hàng loạt theo từng mẫu | EF | 3 mẫu import song song + 1 import-update — sửa logic 1 mẫu nhớ đối chiếu các mẫu kia |
| 15 | Tải template + file lỗi import | nút trong popup import | GET `DownloadFileTemplateImportComBoSpaKhachHang` (+ `...Mau2`/`...Mau3`/`...UpdateComBoSpaKhachHang`) · GET `DownloadDataWrongComboKhachHang_Template_1\|2\|3/{id}` · GET `DownloadDataWrongUpdateComboKhachHang/{id}` | tương ứng `ComBoSpaKhachHang_DownloadFileTemplate...` / `_DownloadDataWrongComboKhachHang_Template_*` | public | popup import | Trả file mẫu (`ComboKhachHangMau1/2/3.xlsx`, `ComboKhachHangUpdate.xlsm`) + file dòng lỗi sau import | GemBox Excel | - |

## Endpoint nội bộ (private / service-to-service — `[PrivateSBAuthorize]`)
- [internal] **GetDetailByIdPrivate** (`GetDetailCrossServiceDto`) — service ngoài lấy chi tiết 1 combo KH theo id; id rỗng → "Không tìm thấy id combo khách hàng!".
- [internal] **GetListComBoByIdKhachHang_Private** (GET `/{idKhachHang}` mặc định page=1/size=100, hoặc POST `GetDataCrossServiceDto`) — list combo của 1 khách cho service ngoài (Customer hợp đồng KH gọi sang).
- [internal] **GetListComboKhach** (`ReqGetLstComboKh`) — list combo KH cho luồng bán hàng/đặt lịch.

## Quy tắc nghiệp vụ (cộng/trừ buổi + validate — blueprint để trace)
1. **Sinh mã combo KH**: `MaComboKhachHang = "CB" + 8 ký tự`, loop sinh lại đến khi unique theo `IdDonVi`. (Service:147-151)
2. **Khởi tạo số buổi khi mua** (Create): mỗi SP/DV trong combo gốc → `SoLan = SoLanConLai = SoLuong(SP)/SoLan(DV)` gốc. Chưa dùng buổi nào → còn = ban đầu. (Service:189-190,211-212)
3. **Sửa số buổi** (CapNhat): ràng buộc **`SoLanConLai <= SoLan`** cho từng dòng SP và DV; vi phạm → chặn. (Service:342-344,358-360)
4. **Trừ buổi** (TacDongDinhLuong, TrangThai=2): `SoLanConLai -= SoLuong`; nếu `< 0` → "Số đơn vị đã đạt giới hạn!" (chặn trừ quá số còn lại). (Service:767-769,799-801)
5. **Cộng buổi / hoàn buổi** (TrangThai=1): `SoLanConLai += SoLuong`; nếu `> SoLan` → "vượt quá số đơn vị ban đầu!" (không hoàn quá số gốc). (Service:759-761,791-793)
6. **Check hạn sử dụng (đang LỖI)**: ý đồ chặn combo hết hạn, nhưng điều kiện `HanSuDung > DateTime.Now` bị viết ngược → thực tế chặn combo còn hạn. Cần xác nhận/sửa trước khi tin tưởng nghiệp vụ trừ buổi. (Service:754,783)
7. **Lọc theo buổi còn lại** (TimTheoDieuKien): join `ComBoSpaKhachHangChiTiet` theo `SoLanConLai` so với `SoLanSuDungDichVuConLai` bằng 1 trong 3 toán tử (>= / == / <=) tùy cờ filter. (Service:589,594,599)

## Quan hệ
- **Gọi sang:** Customer `KhachHang/TimTheoId` (verify KH khi tạo); CommonService (GetDetailComboKhachHang); đọc combo gốc + `ComBoSpaSanPham`/`ComBoSpaDichVu` (WarehouseProduct nội bộ) để copy chi tiết.
- **Dữ liệu (entities):** `ComBoSpaKhachHang` (đầu instance: IdCombo gốc, IdKhachHang, MaComboKhachHang, HanSuDung, giá, IdDonHang), `ComBoSpaKhachHangChiTiet` (mỗi dòng SP/DV: `SoLan`/`SoLanConLai`, `SoPhutSuDung`/`SoPhutSuDungConLai`, KieuLoai SP|DV, IsQuaTang, quy đổi đơn vị tính). Lịch sử lưu MongoDB: `ComboSpaKhachHangLichSu`, `ComboSpaKhachHangGiaoDichLichSu`.
- **Ripple / đồng bộ:** Order gọi `GetComboSpaKhDetail` lấy buổi để dựng đơn dịch vụ/sản phẩm từ combo (DonHangService:3732,4056,14134); KH360 hiển thị số buổi còn lại (tab solan-combo). Trừ buổi làm giảm `SoLanConLai` → ảnh hưởng số buổi còn dùng được ở các màn bán hàng/đặt lịch.

## Luồng xuyên module
1. **Mua combo:** (Order/Sale) khách mua combo gốc → tạo `ComBoSpaKhachHang` (instance) + chi tiết với SoLanConLai = số buổi mua → xuất hiện trong KH360.
2. **Dùng buổi:** Thu ngân/Đặt lịch chọn buổi combo → Order gọi `GetComboSpaKhDetail` lấy dòng chi tiết → khi chốt, `SoLanConLai` bị **trừ dần** (qua `TacDongDinhLuong_Single/Multi` hoặc cập nhật chi tiết) → KH360 phản ánh số buổi còn lại giảm.

## Lỗi đã biết / lưu ý kỹ thuật
- **BUG check hạn sử dụng ngược** ở cả `TacDongDinhLuong_Single` và `_Multi`: `if (comboKhachHang.HanSuDung > DateTime.Now) return "Combo này đã hết hạn sử dụng!"` — điều kiện đảo ngược: combo còn hạn (hạn ở tương lai) bị chặn, combo hết hạn lại cho qua. `ComBoSpaKhachHangService.cs:754` và `:783`.
- **NullReferenceException tiềm ẩn**: `dichVu` (dòng chi tiết tìm theo `IdDichVu == payload.IdHang`) không null-check trước khi `+=`/`-=` → IdHang sai hoặc không thuộc combo → NRE. `ComBoSpaKhachHangService.cs:756`, `:788`.
- **Message copy nhầm**: nhánh thành công của `Update` và `Delete` trả về "Thêm mới thành công!" (lẽ ra "Cập nhật/Xóa thành công"). `ComBoSpaKhachHangService.cs:286` (Delete).
- **2 endpoint `[SBAuthorize()]` rỗng** (`GetListComboKhByIds`, `GetListComboByIds`) — chỉ yêu cầu đăng nhập, không gắn permission key riêng → khó audit phân quyền.
- **3 mẫu import + 1 import-update song song** (`ImportComBoSpaKhachHang` / `Mau2` / `Mau3` / `ImportUpdateComBoSpaKhachHang`) — mỗi mẫu có template + file lỗi riêng; sửa logic 1 mẫu phải đối chiếu các mẫu còn lại.
- **Lệch tên DTO endpoint**: FE service gọi prefix `ComboSpaKhachHang/...` còn route BE là `api/ComBoSpaKhachHang` — case khác nhau (route .NET không phân biệt hoa/thường nên vẫn chạy), nhưng dễ nhầm khi grep.
- **Phân biệt với `ComBoSpaController`**: đó là combo GỐC (master, đầu combo bán). Controller này là combo của KHÁCH (instance đã mua, có số buổi trừ dần). Đừng sửa nhầm bảng.
