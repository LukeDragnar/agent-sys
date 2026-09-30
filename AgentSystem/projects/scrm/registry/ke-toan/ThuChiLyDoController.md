---
controller: ThuChiLyDoController
module: ke-toan
service: Branch
man_hinh_ui: "Cấu hình > Lý do thu chi (danh mục lý do thu/chi): danh sách + thêm/sửa/xóa. Bản ghi default (IdDonVi=Guid.Empty) của hệ thống chỉ HT-AD/superuser được sửa; đơn vị tự thêm lý do riêng."
muc_dich: "CRUD danh mục lý do thu-chi của đơn vị, cộng bản ghi default toàn hệ thống (IdDonVi=Guid.Empty). Có cache Redis theo đơn vị; chống trùng tên trong 1 loại thu chi và trùng mã lý do default."
alias: ["lý do thu chi", "danh mục lý do", "thu chi lý do", "lý do phiếu thu", "lý do phiếu chi"]
auth: mixed
state_machine: []
deprecated: false
entities: [ThuChiLyDo]
stored_procs: []
depends: [Redis cache (CK_ThuChiLyDo_ theo đơn vị)]
cross_module_in: []
cross_module_out: []
---

# ThuChiLyDoController — Danh mục lý do thu chi

> CRUD danh mục `ThuChiLyDo`. Mỗi đơn vị có lý do riêng (IdDonVi = DonViId) + dùng chung bản ghi default của hệ thống (IdDonVi = `Guid.Empty`). Superuser/HT-AD thao tác trên bản ghi default; user đơn vị chỉ sửa/xóa lý do của đơn vị mình. Cache Redis `CK_ThuChiLyDo_{idDonVi}`, clear sau khi Lưu/Xóa. `GetAllLyDoThuChi` là endpoint nội bộ.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Lưu lý do thu chi | Lưu | `POST api/ThuChiLyDo/Luu` | `ThuChiLyDo_Xoa` | public | popup thêm/sửa | Superuser/HT-AD → ghi default (IdDonVi=Empty), được set MaLyDo; user đơn vị → IdDonVi=DonViId. Validate LoaiThuChi∈[1,4], TenLyDo≤100, **chống trùng tên trong 1 loại**, **chống trùng MaLyDo** (chỉ default). Insert/update + clear cache. | **EF** `ThuChiLyDo` | Key gắn là `ThuChiLyDo_Xoa` (copy nhầm — cùng key với Xóa). `ThuChiLyDoController.cs:36`. |
| 2 | Xóa lý do thu chi | Xóa | `GET api/ThuChiLyDo/Xoa/{id}` | `ThuChiLyDo_Xoa` | public | mỗi dòng | `DeleteAsync` (soft-delete qua BaseService) theo id; clear cache đơn vị. | **EF** | Không re-check ownership đơn vị trước khi xóa (xóa theo id thuần). `ThuChiLyDoService.cs:374`. |
| 3 | Danh sách (phân trang) | Lưới list / Lọc | `POST api/ThuChiLyDo/DanhSach` | `ThuChiLyDo_DanhSach` | public | màn Lý do thu chi | Lấy (cache) lý do đơn vị + default; lọc LoaiThuChi/Keyword(TenLyDo); paging; map AllowEdit (default chỉ sửa nếu là admin) + tên loại thu chi. | **EF** (cache) | — |
| 4 | Danh sách không phân trang | (combo lọc) | `GET api/ThuChiLyDo/DanhSachKhongPhanTrang?idDonVi=&loaiThuChi=&coMacDinh=` | `ThuChiLyDo_DanhSachKhongPhanTrang` | public | dropdown chọn lý do | Non-admin ép idDonVi=DonViId; `coMacDinh` quyết định có lấy bản ghi default; lọc theo LoaiThuChi. | **EF** (cache) | — |
| 5 | Chi tiết theo Id | Mở edit | `GET api/ThuChiLyDo/ChiTiet/{id}?idDonVi=` | `ThuChiLyDo_ChiTiet` | public | click dòng | Lấy từ cache theo id; map AllowEdit/AllowUpdateMaLyDo theo quyền admin & bản ghi default. | **EF** (cache) | Param `idDonVi` truyền vào bị ghi đè bằng userLogin.DonViId (admin→Empty). `ThuChiLyDoService.cs:232`. |

## Endpoint nội bộ (private / service-to-service)
- **[internal] `GET api/ThuChiLyDo/GetAllLyDoThuChi/{idDonVi}`** (`[PrivateSBAuthorize]`) — trả toàn bộ lý do thu-chi (đơn vị + default) cho service khác dùng (`ThuChiLyDoService.cs:396`). Tương tự `CommonController/GetListLyDoKeToan` nhưng theo đơn vị.

## Quan hệ
- **Gọi sang:** không (chỉ EF + Redis cache).
- **Dữ liệu (entities):** `ThuChiLyDo` (IdDonVi đơn vị hoặc Guid.Empty cho default; MaLyDo + MaKeToan để map sang kế toán).
- **Ripple / đồng bộ:** clear cache `CK_ThuChiLyDo_{idDonVi}` sau Lưu/Xóa. Lý do được phiếu thu-chi tham chiếu (ThuChiController) + map MaKeToan khi đồng bộ Accounting.

## Lỗi đã biết / lưu ý kỹ thuật
- `ThuChiLyDoController.cs:36` — endpoint `Luu` gắn key `ThuChiLyDo_Xoa` (copy nhầm key của Xóa) → phân quyền thêm/sửa = key xóa.
- `ThuChiLyDoService.cs:374` — `Xoa` không re-check IdDonVi của bản ghi trước khi `DeleteAsync` (xóa theo id, dựa vào lọc danh sách phía trên).
- `ThuChiLyDoService.cs:232` — `ChiTiet` ghi đè param `idDonVi` bằng userLogin (param FE truyền không có tác dụng).
