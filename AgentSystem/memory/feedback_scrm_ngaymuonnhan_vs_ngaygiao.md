---
name: feedback-scrm-ngaymuonnhan-vs-ngaygiao
description: SCRM "ngày muốn nhận" vs "ngày giao thực tế" — Contact.LgtGiaoHangNgayGiaoHang mang nghĩa MUỐN NHẬN (tên nói dối); order tách 2 cột; sync qua 2 consumer host riêng; verify nghiệp vụ qua web trước khi assume dialog
metadata:
  type: feedback
---

**Task 1046** (tách "ngày muốn nhận hàng" khỏi "ngày giao thực tế"). Mô hình field CHUẨN sau fix:

| | Ngày khách MUỐN nhận | Ngày GIAO thực tế |
|---|---|---|
| **Đơn `DonHangLogistic`** | `NgayMuonNhanHang` | `GiaoHangNgayGiaoHang` (nuôi hạn hóa đơn `NgayTieuChuanXuatHoaDon`) |
| **`Contact`** | `LgtGiaoHangNgayGiaoHang` ⚠️ **tên nói dối** — bên Contact field này = ngày MUỐN (tái dùng, không thêm cột/reindex ES). Có comment đầy đủ ở entity. | (contact KHÔNG giữ ngày giao) |

**3 dialog "Cập nhật ngày muốn nhận hàng"** (sale từ danh sách contact · kho · kế toán tác nghiệp) — CẢ 3 đều sửa **NGÀY MUỐN** (KHÔNG phải ngày giao). **Không có form nào sửa ngày giao** — ngày giao chỉ do luồng đổi trạng thái/webhook tự set. Endpoint order-side `UpdateNgayMuonNhanHang` (KhoTacNghiepService) ghi `NgayMuonNhanHang`; dialog sale ghi `Contact.LgtGiaoHangNgayGiaoHang`.

**Sync 2 chiều chạy ở HOST CONSUMER RIÊNG** (không phải trong *.API web):
- Contact→Order: `OrderLogisticConsumer.API` (ContactUpdateDonHangLogisticConsumer)
- Order→Contact: `ContactConsumer.API` (DonHangLogisticUpdateContactConsumer)
→ Test local mà không chạy 2 host này ⇒ message bị **consumer STAGING (code cũ) nhặt** (broker RabbitMQ dùng chung) ⇒ map kiểu cũ, tưởng bug code. Broker chung còn dính **competing-consumer** (staging tranh message) ⇒ test sync local KHÔNG tin cậy → nên deploy staging test.

**Why:** field bị overload từ lịch sử (Toan 6/2024 tạo cột NgayMuonNhanHang bỏ trống → staging 11/2024 nhét "muốn" vào GiaoHangNgayGiaoHang → NganLQ 3/2025 luồng trạng thái ghi đè). "1 field" mà ripple: đơn↔contact↔2 consumer↔ES↔phiếu bán↔3 dialog↔filter.

**How to apply:**
- Đụng ngày giao/muốn ở SCRM: nhớ bảng trên. Sửa "muốn" KHÔNG được chạm `order.GiaoHangNgayGiaoHang` (đè ngày giao → sai hạn hóa đơn).
- **Verify semantics qua WEB/nghiệp vụ TRƯỚC khi assume** một dialog/field làm gì — tên field/endpoint/label ở SCRM hay nói dối (overload). Đừng assume "kho/kế toán quan tâm ngày giao" hay "3 dialog cùng nghĩa" mà chưa soi UI + code cũ (`git show release_product:<file>`).
- Filter "đơn giao sai hẹn" = `NgayMuonNhanHang != GiaoHangNgayGiaoHang` (so theo NGÀY, `.Value.Date`), cờ `DonHangLogisticFilterDto.IsGiaoSaiHen` (true=sai/false=đúng/null=không lọc). List **đơn sale** (`DonHangService.TimTheoDieuKienAsync`) chạy **DB/EF**.
- Liên quan [[feedback-cherrypick-feature-branch-config-gate]], [[feedback-tach-field-quet-het-noi-hien-thi]].

**Bổ sung (phiên grid-column, 7/2026) — cột "Ngày muốn nhận" trên list thu-kho/kế-toán bị SÓT:**
- List **thu-kho + kế-toán tác nghiệp** (`KhoTacNghiepService`/`KeToanTacNghiepService.TimTheoDieuKienAsync`) chạy **Elasticsearch** (KHÁC list đơn sale = DB/EF). Nguồn ES `DonHangLogisticES` map từ entity qua **AutoMapper** (`CreateMap<DonHangLogistic, DonHangLogisticES>`) → field cùng tên tự map, chỉ cần **reindex** cho đơn cũ.
- Cột grid nhãn "Ngày muốn nhận" **bind nhầm `giaoHangNgayGiaoHang`** (label đúng/field sai — pre-existing, commit dev khác). Sau khi 1046 tách field, phải **rebind sang `ngayMuonNhanHang`** ở TẤT CẢ: FE `GridColumn.field`+`dataType`+HTML `*ngSwitchCase` (HTML switch theo **dataType** — đổi field phải đổi kèm switchCase) + value **vm1 + vm2 grouped** + `<td title>` + dòng update-local sau lưu form; BE trả field trong response qua **ES `.Select`** (KeToan 1 site, Kho 3 site: 2 ES + 1 SQL-join) **+ stored proc** `PRC_..._ThuKhoTacNghiep_GetListByIds` (nhánh config `Allows_Return_..._From_SQL=true`, Dapper map theo TÊN cột → SP phải SELECT thêm cột; SP **dùng chung** cho cả 2 màn, không có trong repo).
- `data.ngayMuonNhanHang` để ở **top-level DTO** `ThuKhoTacNghiepFilterResponse`, KHÔNG ở object lồng `DonHangLogisticInfo` → HTML đọc `data.ngayMuonNhanHang` (top-level), không phải `data.donHangLogisticInfo.*`.
