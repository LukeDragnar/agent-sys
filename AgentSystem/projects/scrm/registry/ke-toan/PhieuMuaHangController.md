---
controller: PhieuMuaHangController
module: ke-toan
service: Accounting
man_hinh_ui: "Kế toán > Chứng từ mua hàng > màn 'Phiếu mua hàng'. Lưới list + bộ lọc (loại ngày, khoảng ngày, kho, NCC, trạng thái, keyword). Nút: Thêm mới (form phiếu + dòng sản phẩm), Sửa, Xoá, Hoàn thành, Huỷ, Xuất Excel, Import Excel (popup). Nguồn tạo khác: từ Đơn đặt hàng (DonDatHang/TaoPhieuMuaHang)."
muc_dich: "Quản lý chứng từ phiếu mua hàng (mua vào kho / mua cũ đổi mới / mua từ khách / mua hàng dịch vụ nội bộ). Là chứng từ gốc kéo theo phiếu nhập kho, phiếu chi, công nợ NCC và bút toán kế toán + đồng bộ MISA."
alias: ["phiếu mua hàng", "phieu mua hang", "PMH", "mua hàng vào kho", "mua hàng từ khách", "mua hàng cũ đổi mới", "nhập mua", "chứng từ mua hàng"]
auth: mixed
state_machine: [ChuaXuLy, DangXuLy, DaHoanThanh, HuyBo, Nhap]
deprecated: false
entities: [PhieuMuaHang, PhieuMuaHangSanPham, DataReceiveTaoPhieu]
stored_procs: []
depends: [CommonService (ThemMoiFromPhieuMuaHang/phiếu nhập, TaoPhieuThuChi/phiếu chi, TaoPhieuGhiNoChonhaCungCap/công nợ NCC, SinhMaPhieu, GenPhieuMuaHangSanPhamDto), WarehouseService (HoanThanhPhieuNhapTheoPhieuMuaHang, HuyPhieuNhapTheoPhieuMuaHang, gen PMH sản phẩm), ButToanService (ButToan_PhieuMuaHang_Save), MisaDanhMucSync, DonDatHangService (CapNhatSoLuongDaNhan, CapNhatDaTaoPhieuMuaHang), CauHinhKetNoiKeToan]
cross_module_in: [DonDatHangController/TaoPhieuMuaHang (tạo PMH từ đơn đặt hàng), WarehouseService callback CapNhatTrangThaiPhieuAndThuHo (kho báo phiếu nhập hoàn thành), Branch/ThuChi (cập nhật số tiền đã chi qua callback)]
cross_module_out: [WarehouseProduct/PhieuNhap (sinh + hoàn thành + huỷ), Branch/ThuChi-PhieuChi (sinh + huỷ phiếu chi), CongNo NCC (ghi nợ 331), ButToan/Accounting (bút toán phiếu mua), MISA (DataReceiveTaoPhieu + đồng bộ danh mục), PhieuBanHang (mua cũ đổi mới sinh phiếu bán)]
---

# PhieuMuaHangController — Phiếu mua hàng (chứng từ mua, kéo theo nhập kho + chi + công nợ + bút toán)

> Chứng từ trung tâm của luồng mua. 4 loại (`MaLoaiMuaHang`): 1=Mua vào kho, 2=Mua cũ đổi mới, 3=Mua từ khách hàng, 4=Mua hàng/dịch vụ nội bộ (KHÔNG nhập kho). State (`TinhTrangPhieuMuaHangEnum`): ChuaXuLy=1, DangXuLy=2, DaHoanThanh=3, HuyBo=4, **Nhap=5** (lưu ý: enum sắp xếp 1..4 rồi Nhap=5). Khi tạo phiếu KHÁC trạng thái Nháp → trong cùng transaction sinh **phiếu nhập kho** (trừ loại 4) + **phiếu chi** (nếu tick) + **công nợ NCC** (ghi nợ 331); sau commit sinh **bút toán** + cập nhật đơn đặt hàng. Hoàn thành đi qua kho rồi kho callback ngược về. Class auth là **mixed**: vài endpoint có `[SBAuthorize(key,...)]`, vài endpoint `[SBAuthorize()]` trống, vài cái `[PrivateSBAuthorize]` (internal), 1 cái `[AllowAnonymous]`.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm kiếm / load lưới | Lưới + bộ lọc | `POST api/PhieuMuaHang/TimTheoDieuKien` | `PhieuMuaHang_TimTheoDieuKien` | public | màn phiếu mua hàng | EF query `PhieuMuaHang` theo IdDonVi. Super/CSKH xem tất cả; admin (+ chọn CN) lọc theo CN; user thường ép `IdChiNhanh=user.IdChiNhanh`. Lọc ngày theo `IdLoaiNgay` (NgayTao mặc định / NgayDonHang / NgayHachToan / NgayChungTu / NgayGiaoHang), keyword, kho, NCC, trạng thái; paging. | **EF** `PhieuMuaHang` (.TableWithNolock) | — |
| 2 | Thêm mới phiếu | Form "Thêm phiếu mua hàng" / Lưu | `POST api/PhieuMuaHang/ThemMoi` | `PhieuMuaHang_ThemMoi` | public | nút Thêm mới | Validate input (NCC/kho/ngày/thanh toán/dòng SP). Admin tổng chọn CN khác → **clone userLogin** (tránh poison cache HybridCache, L509-522) rồi override IdChiNhanh. Sinh MaPhieu nếu trống, chặn trùng mã. Gen PMH sản phẩm (gọi warehouse), check lô-date đồng nhất. Trong transaction: insert PMH + SP; nếu state≠Nhap → `TinhToanTaoCacPhieu` (phiếu nhập + phiếu chi + công nợ NCC). Sau commit (state≠Nhap): sinh bút toán + nếu có IdDonDatHang → CapNhatSoLuongDaNhan + CapNhatDaTaoPhieuMuaHang. | **EF** transaction (`PhieuMuaHang`,`PhieuMuaHangSanPham`) | — |
| 3 | Xem chi tiết | mở phiếu | `GET api/PhieuMuaHang/TimTheoId/{id}` | `PhieuMuaHang_TimTheoId` | public | click dòng | Load phiếu + dòng SP theo Id (check IdDonVi). | **EF** | — |
| 4 | Cập nhật thông tin | Sửa phiếu / Lưu | `POST api/PhieuMuaHang/CapNhatThongTin` | `PhieuMuaHang_CapNhatThongTin` | public | phiếu chưa hoàn thành | `CapNhatThongTin` — cập nhật phiếu + đồng bộ SP; sinh lại bút toán. | **EF** | — |
| 5 | Xoá phiếu | nút Xoá / confirm | `DELETE api/PhieuMuaHang/XoaTheoId/{id}` | **`[SBAuthorize()]` trống** | public | mỗi dòng | `XoaTheoId`. Comment trên method ghi key dự kiến `PhieuMuaHang_XoaTheoId` nhưng **attribute thực để trống** → chỉ cần đăng nhập. | **EF** | Quyền rỗng — xem Lỗi đã biết. |
| 6 | Hoàn thành phiếu | nút "Hoàn thành" | `GET api/PhieuMuaHang/ChuyenTrangThaiHoanThanh?id=` | **`[SBAuthorize()]` trống** | public | state ∈ {DangXuLy, ChuaXuLy}; KHÔNG cho từ Nhap, KHÔNG cho khi đã DaHoanThanh | Gọi kho `HoanThanhPhieuNhapTheoPhieuMuaHang`. Nếu kho trả Error (vd IMEI chưa quét đủ) → không hoàn thành. Nếu Completed/AlreadyCompleted → kho đã callback set trạng thái + bút toán + MISA, chỉ đảm bảo PMH=DaHoanThanh (KHÔNG chạy body để tránh double bút toán). Nếu NoPhieuNhap (vd dịch vụ nội bộ) → hoàn thành trực tiếp + đồng bộ MISA danh mục + DataReceiveTaoPhieu + sinh bút toán. | **EF** + cross-service kho | Phiếu Nhap (nháp) bị chặn, phải DangXuLy trước. |
| 7 | Huỷ phiếu | nút "Huỷ" | `POST api/PhieuMuaHang/Huy` | **`[SBAuthorize()]` trống** | public | state ∉ {DaHoanThanh, HuyBo} | `HuyPhieuMuaHang` — cascade huỷ: (1) parse `IdsThuChi` (chuỗi "id;id"). (2) PRE-CHECK phiếu chi read-only (checkOnly) — nếu PC đã xử lý xong → Blocked, dừng trước khi mutate. (3) Huỷ phiếu nhập liên kết (kho) trước; PN đã hoàn thành → chặn. (4) Huỷ phiếu chi (Branch, atomic). (5) Set PMH=HuyBo. Thứ tự PN→PC để self-heal idempotent khi retry. | **EF** + cross-service kho + Branch | — |
| 8 | Xuất Excel | nút "Xuất Excel" | `POST api/PhieuMuaHang/ExportExcelPhieuMuaHang` | `[SBAuthorize()]` trống | public | có dữ liệu | `ExportExcelPhieuMuaHangAsync` → file `PhieuMuaHang.xlsx`. NotFound nếu rỗng. | EF + GemBox | — |
| 9 | Phân bổ chiết khấu | (trong form, tính client-assist) | `POST api/PhieuMuaHang/PhanBoChietKhauTheoTongHoaDon` | — | **anonymous** (`[AllowAnonymous]`) | nhập chiết khấu tổng | `PhanBoChietKhauTheoTongHoaDon` — chia chiết khấu tổng hoá đơn về từng dòng: TuNhap% (tổng phải =100%, dòng cuối bù sai số), theo đơn giá (SoLuong×DonGia), theo số lượng. Trả lại list đã tính. | in-memory (không DB) | **`[AllowAnonymous]` cho hàm tính tiền** — không cần đăng nhập (L133). |
| 10 | Lấy cấu hình thông báo mua bán | (màn cấu hình) | `GET api/PhieuMuaHang/GetCauHinhThongBaoMuaHang` | `SBAuthorize()` trống | public | màn "Cấu hình mua bán" | Đọc `CauHinh.JsonSetting` → `GuiThongBaoTaoPhieuMuaHang` (bật/tắt) + `RolesNhanThongBaoTaoPhieuMuaHang` (role nhận). | **EF** `CauHinh` | Mới (2026-06/07). |
| 11 | Lưu cấu hình thông báo mua bán | nút Lưu cấu hình | `POST api/PhieuMuaHang/SaveCauHinhThongBaoMuaHang` | `SBAuthorize()` trống | public | màn "Cấu hình mua bán" | Ghi bật/tắt + danh sách role nhận thông báo tạo phiếu mua hàng vào `CauHinh.JsonSetting`. | **EF** `CauHinh` | — |

## Endpoint nội bộ (private / service-to-service)
- [internal] `POST CapNhatTrangThaiPhieuAndThuHo` (`[PrivateSBAuthorize]`) — **callback từ kho/Branch**: cộng `TongSoTienDaChi`, set cờ `IsPhieuNhapHoanThanh`/`IsThanhToanThuChiThanhCong`. Khi phiếu nhập hoàn thành → set PMH=DaHoanThanh theo loại (loại 1/2 mua vào kho/cũ đổi mới: chỉ cần phiếu nhập xong; loại 3 mua từ khách: chỉ cần phiếu nhập xong; loại 4 nội bộ: set hoàn thành luôn). Rollback loại 2 khi `IsPhieuNhapHoanThanh=false` → về DangXuLy. Sinh bút toán + đồng bộ MISA + DataReceiveTaoPhieu khi DaHoanThanh.
- [internal] `POST TạoPhieuBanHangFromPhieuMuaHang` (`[SBAuthorize()]` trống) — loại "mua cũ đổi mới": dựng `rqTaoPhieuBanHang` từ dòng SP rồi gọi `CommonService.PhieuBanHangUpate` tạo phiếu bán; gắn `IdPhieuBanHang` ngược lại PMH.
- [internal] `GET IsNccDaCoPhieuMuaHang/{idNcc}` (`[PrivateSBAuthorize]`) — check NCC đã từng có phiếu mua chưa (theo DonVi+ChiNhanh).
- [internal] `POST CapNhatPhieuTheoDonHang` (`[PrivateSBAuthorize]`) — cập nhật phiếu theo đơn hàng.

## Endpoint Import (partial `PhieuMuaHangController.Import.cs`, tất cả `[SBAuthorize()]` trống)
- `GET DownloadTemplatePhieuMuaHang` → file mẫu `TemplatePhieuMuaHang.xlsx`.
- `POST ImportTemplatePhieuMuaHang` (IFormFile) → đọc file, validate, lưu bảng tmp.
- `POST GetDanhSachPhieuMuaHangImport` → list dòng tmp đã import (theo statusImport).
- `POST DeleteTmpPhieuMuaHangImport` → xoá dòng tmp.
- `POST InsertTmpPhieuMuaHangToPhieuMuaHang` → chuyển dòng tmp hợp lệ thành phiếu mua hàng thật.

## Delta mới (2026-06/07 — nhánh phan-bo-chi-phi + thong-bao-kho + mau-in-phieu-thu)
- **Thông báo kho khi tạo/chuyển trạng thái phiếu** (`6ea5dbc4e9`, `68b5dcf33e`): `ThemMoi` (state≠Nhap) → `GuiThongBaoTaoPhieuMuaHang` publish `GuiThongBaoInfo` (Code=`TAO_PHIEU_MUA_HANG`, `ListRolesReceive`=role từ CauHinh "Cấu hình mua bán", `[TONG_TIEN]`=TongGiaNhap). Consumer build topic theo role × chi nhánh phiếu. **Publish lỗi KHÔNG chặn tạo phiếu** (Console.WriteLine nuốt lỗi). `CapNhatThongTin`: chỉ bắn khi **chuyển Nháp→trạng thái xử lý** (`trangThaiCu==Nhap`), tránh báo lại mỗi lần sửa phiếu đã xử lý. Constructor thêm `IPublishEndpoint` + `IChiPhiMuaHangService` (`6b6c7d5361`).
- **Chi phí mua hàng (landed cost)** (`1100d02111`): PMH nhận phân bổ từ `ChungTuMuaDichVu` → cột mới `TongChiPhiMuaHang` + `PhuongThucPhanBoChiPhi` (header), `PhieuMuaHangSanPham.ChiPhiMuaHang` + `PhanTramChiPhi` (dòng). Luồng Lưu gọi `IChiPhiMuaHangService.PhanBoChiPhi(boQuaRuleE=true)` + `RecomputeAllocationForPhieu`. Chi tiết: `registry/ke-toan/ChungTuMuaDichVuController.md`. Phiếu nhập sinh từ PMH set diễn giải "Nhập kho từ Phiếu mua hàng {mã}" (`7c695e2c0c`).
- **In "Phiếu thu cũ đổi mới"** (`87c0621334`, `6852c5d790`): `TimTheoId` resolve địa chỉ đối tác (best-effort, bọc try-catch nuốt lỗi) — chỉ fill `rs.PhieuMuaHang.DiaChi` **KHI RỖNG**, ưu tiên KhachHangDaChon → NhaCungCapDaChon (vì DTO trung gian không copy DiaChi → bản in trắng). Thêm `TenDonViTinh` vào `PhieuMuaHangSanPhamDto` populate trong TimTheoId.

## Quan hệ
- **Gọi sang:** WarehouseService (gen PMH sản phẩm, hoàn thành/huỷ phiếu nhập), CommonService (sinh phiếu nhập `ThemMoiFromPhieuMuaHang`, phiếu chi `TaoPhieuThuChi`, công nợ NCC `TaoPhieuGhiNoChonhaCungCap`), ButToanService, MisaDanhMucSync, DonDatHangService.
- **Dữ liệu (entities):** `PhieuMuaHang`, `PhieuMuaHangSanPham`, `DataReceiveTaoPhieu` (hàng đợi đẩy MISA).
- **Ripple / đồng bộ:** phiếu nhập kho, phiếu chi, công nợ NCC (331), bút toán, đồng bộ danh mục MISA (đối tượng/kho/sản phẩm). Không dùng RabbitMQ trực tiếp — đẩy MISA qua bảng `DataReceiveTaoPhieu` + job.

## Luồng xuyên module
- **Tạo phiếu (state≠Nhap)** `ThemMoi` → trong transaction `TinhToanTaoCacPhieu`: (1) nếu loại≠4 → `CommonService.ThemMoiFromPhieuMuaHang` tạo **phiếu nhập kho**, gán `IdPhieuNhap`; (2) nếu `IsTaoPhieuChi=true` → `TaoPhieuThuChi` tạo **phiếu chi**, lưu `IdsThuChi`; (3) nếu có NCC → `TaoPhieuGhiNoChonhaCungCap` ghi **công nợ NCC** (No, TK 331). Sau commit → `ButToan_PhieuMuaHang_Save` + cập nhật **đơn đặt hàng** (số lượng đã nhận + cờ đã tạo PMH).
- **Hoàn thành** `ChuyenTrangThaiHoanThanh` → kho `HoanThanhPhieuNhapTheoPhieuMuaHang` (đi đúng luồng cập nhật phiếu nhập: tồn kho, giá vốn, IMEI, lô-date, ES). Kho hoàn thành → callback `CapNhatTrangThaiPhieuAndThuHo` set PMH=DaHoanThanh + sinh bút toán + đẩy MISA.
- **Huỷ** `Huy` → cascade huỷ phiếu nhập (kho) + phiếu chi (Branch) + set PMH=HuyBo (có pre-check tránh trạng thái lệch).
- **Mua cũ đổi mới** (loại 2) → có thể sinh **phiếu bán hàng** qua `TạoPhieuBanHangFromPhieuMuaHang`.

## Lỗi đã biết / lưu ý kỹ thuật
- BE `Accounting/Accounting.API/Controllers/PhieuMuaHangController.cs`
  - L64 `XoaTheoId`, L108 `ChuyenTrangThaiHoanThanh`, L120 `Huy`, L79 `TạoPhieuBanHangFromPhieuMuaHang`, L146 `ExportExcelPhieuMuaHang`: dùng `[SBAuthorize()]` **trống** (chỉ cần đăng nhập) — comment phía trên ghi key dự kiến nhưng không áp; thao tác xoá/huỷ/hoàn thành đáng lẽ cần key riêng.
  - L133 `PhanBoChietKhauTheoTongHoaDon` = `[AllowAnonymous]` → gọi không cần đăng nhập (hàm tính tiền thuần, không DB nên rủi ro thấp nhưng vẫn lộ).
- BE `Accounting/Accounting.Service/Generated/PhieuMuaHangService.cs`
  - Enum `TinhTrangPhieuMuaHangEnum` (`Libs/Models.Core/Enum.cs:1055`): **Nhap=5** xếp sau HuyBo=4 (không phải state đầu) → cẩn thận khi suy luận thứ tự trạng thái.
  - `ChuyenTrangThaiHoanThanh` L1843-1857: nhánh Completed/AlreadyCompleted KHÔNG chạy body sinh bút toán (đúng — tránh double), nhưng phụ thuộc callback kho đã set; nếu callback miss thì PMH chỉ được set DaHoanThanh mà thiếu bút toán.
  - `CapNhatTrangThaiPhieuAndThuHo` L1216-1239: nhiều điều kiện check `IsThanhToanThuChiThanhCong`/`TongSoTienDaChi==TongGiaNhap` đã bị comment out → hiện chỉ cần `IsPhieuNhapHoanThanh=true` là set hoàn thành (logic thanh toán đã nới lỏng).
