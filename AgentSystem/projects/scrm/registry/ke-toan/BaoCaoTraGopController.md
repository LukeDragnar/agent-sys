---
controller: BaoCaoTraGopController
module: ke-toan
service: Accounting
man_hinh_ui: "Kế toán > Báo cáo trả góp: (1) Báo cáo công nợ phải thu trả góp (tổng hợp theo đối tác trả góp, lọc Từ/Đến ngày, paging); (2) Chi tiết công nợ phải thu trả góp theo 1 đối tác trả góp (paging)."
muc_dich: "2 báo cáo công nợ phải thu của đơn hàng trả góp: tổng hợp + chi tiết theo đối tác trả góp. Đọc thuần qua stored proc, scope theo đơn vị + chi nhánh của userLogin."
alias: ["báo cáo trả góp", "bao cao tra gop", "công nợ phải thu trả góp", "công nợ trả góp", "báo cáo công nợ trả góp", "đơn hàng trả góp"]
auth: public
state_machine: []
deprecated: false
entities: []
stored_procs: [PRC_SCRM_Accounting_DonHangTraGop_BaoCaoCongNoPhaiThu_Search, PRC_SCRM_Accounting_DonHangTraGop_BaoCaoCongNoPhaiThuChiTiet_Search]
depends: [SqlSugar (GetDataSetAll stored proc), ObjectHelper.FillCollection (map DataTable → DTO)]
cross_module_in: []
cross_module_out: []
---

# BaoCaoTraGopController — Báo cáo công nợ phải thu trả góp (tổng hợp + chi tiết)

> Controller view/report THUẦN ĐỌC, gọn: 2 endpoint, mỗi cái 1 **stored proc**, scope cứng theo `userLogin.DonViId` + `userLogin.IdChiNhanh` (KHÔNG nhận chi nhánh từ request → user chỉ thấy báo cáo chi nhánh đang đăng nhập). Map `Tables[0]` → DTO bằng `ObjectHelper.FillCollection`, `total` = `Tables[1][0][0]`. Khác BaoCaoKeToan: KHÔNG gate `CheckQuyenXemReportOrder`, KHÔNG log query chậm, KHÔNG enrich tên. `[SBAuthorize()]` trống (chỉ cần login). Controller KHÔNG có `[TienIchAttribute]`.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Báo cáo công nợ phải thu trả góp | Lưới báo cáo (tổng hợp theo đối tác trả góp) | `POST api/BaoCaoTraGop/GetBaoCaoCongNoPhaiThu` | `SBAuthorize()` (any login) | public | màn báo cáo trả góp | Build 6 param (IdDonVi, IdChiNhanh = từ userLogin, TuNgay, DenNgay, paging). Chạy SP, `FillCollection<ResponseBaoCaoCongNoPhaiThuDto>(Tables[0])`; `total` = `(int)Tables[1][0][0]`. | **SP** `PRC_SCRM_Accounting_DonHangTraGop_BaoCaoCongNoPhaiThu_Search` (table0=data, table1=total) | IdChiNhanh ép theo userLogin (request KHÔNG truyền) → không xem được chi nhánh khác kể cả admin tổng. Total cast cứng `(int)` (DBNull/rỗng → throw). |
| 2 | Chi tiết theo đối tác trả góp | (drill-down 1 đối tác) | `POST api/BaoCaoTraGop/GetBaoCaoCongNoPhaiThuChiTiet` | `SBAuthorize()` (any login) | public | click 1 đối tác trả góp | Build 5 param (IdDonVi, IdChiNhanh từ userLogin, `IdDoiTacTraGop`, paging). Chạy SP, `FillCollection<ResponseBaoCaoCongNoPhaiThuChiTietDto>(Tables[0])`; `total` = `(int)Tables[1][0][0]`. | **SP** `PRC_SCRM_Accounting_DonHangTraGop_BaoCaoCongNoPhaiThuChiTiet_Search` | Như #1: IdChiNhanh ép theo userLogin; total cast cứng `(int)`. |

## Quan hệ
- **Gọi sang:** không (chỉ DB qua SqlSugar stored proc).
- **Dữ liệu (entities):** không entity nội bộ — toàn bộ qua 2 stored proc; map ra `ResponseBaoCaoCongNoPhaiThuDto` / `ResponseBaoCaoCongNoPhaiThuChiTietDto`.
- **Ripple / đồng bộ:** không (read-only).

## Lỗi đã biết / lưu ý kỹ thuật
- BE `Accounting/Accounting.API/Controllers/BaoCaoTraGopController.cs`
  - L26/L34: `[SBAuthorize()]` trống → chỉ login, không permission key. Controller không có `[TienIchAttribute]`.
- BE `Accounting/Accounting.Service/Generated/BaoCaoTraGopService.cs`
  - L34/L51: scope `@IdChiNhanh = userLogin.IdChiNhanh` (không lấy từ request) → báo cáo bị khóa cứng theo chi nhánh đang login; admin tổng không xem chéo chi nhánh được.
  - L43/L59: `var TotalCount = (int)ds.Tables[1].Rows[0][0];` — cast trực tiếp, nếu SP trả NULL/khác kiểu → InvalidCast/Null throw (không có try/catch, không default).
  - L30-31: 2 dòng comment gán cứng `model.TuNgay/DenNgay` (code test cũ) — đã comment, vô hại.
  - Method `async` nhưng không `await` gì (SP call sync) → cảnh báo CS1998, vô hại.
- DTO: `Models.Core.DTO.Accounting.Client.BaoCaoCongNoTraGop` (`ReqBaoCaoCongNoPhaiThu`, `ReqBaoCaoCongNoPhaiThuChiTiet`, `ResponseBaoCaoCongNoPhaiThuDto`, `ResponseBaoCaoCongNoPhaiThuChiTietDto`).
