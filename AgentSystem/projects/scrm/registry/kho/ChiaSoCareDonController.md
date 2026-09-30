---
controller: ChiaSoCareDonController
module: kho
service: OrderLogistic
man_hinh_ui: "Đơn hàng logistic > màn Chia số care đơn (tác nghiệp kho): cây Phòng ban → Nhóm NV → User; grid đơn chưa phân bổ group theo trạng thái giao hàng + 2 cấp phân bổ (về Nhóm care đơn, rồi về User care đơn); nút 'Phân bổ ngay' và 'Phân bổ hàng loạt' ở mỗi cấp; popup lịch sử chia số per-đơn"
muc_dich: "Chia/phân bổ thủ công đơn hàng logistic cho nhóm care đơn rồi cho từng user care đơn (tác nghiệp kho), theo 2 cấp: cấp nhóm (IdNhomCareDon) và cấp user (UserIdCareDon); kèm xem số lượng đơn chưa phân bổ group theo trạng thái giao hàng và lịch sử chia số."
alias: ["chia số care đơn", "chia đơn care", "phân bổ đơn care", "chia đơn cho kho", "phân bổ đơn về nhóm/user", "tác nghiệp kho chia số"]
auth: mixed
state_machine: []
deprecated: false
entities: [DonHangLogistic, QueueDonHangChiaCareDon, LogChiaSoCareDon]
stored_procs: [SCRM_CauHinhChiaCareDon_GetDSTrangThaiGiaoHangKemSoLuong]
depends:
  - "IChiaSoCareDonService (OrderLogistic.Service/Generated/ChiaSoCareDonThuCongService.cs + ChiaSoCareDonThuCongService20250620.cs)"
  - "ICauHinhService (GetAsync — đọc cấu hình kho tổng / IsSuDungChiaSoCareDon)"
  - "IOtherServices ticket lock (DoProcessTicket / ReleaseLockedTicket — chống chia trùng concurrent)"
  - "IEsDocumentService (ESIndexDonHangLogistic — reindex sau khi chia)"
  - "ILogLichSuTacNghiepService (ghi log tác nghiệp ChiaCareDon)"
cross_module_in:
  - "FE màn Chia số care đơn — toàn bộ endpoint phục vụ UI tác nghiệp kho; phụ thuộc cấu hình ở CauHinhChiaSoCareDonController + CauHinhController (OrderLogistic)"
cross_module_out:
  - "Elasticsearch (ESIndexDonHangLogistic — mỗi đơn được chia lại được reindex để search theo nhóm/user care đơn)"
  - "Log tác nghiệp kho (LichSuTacNghiepLog action ChiaCareDon — ripple sang lịch sử tác nghiệp DonHangLogistic)"
---

# ChiaSoCareDonController — Chia số / phân bổ đơn care (tác nghiệp kho)

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Cây phòng ban quản lý kho | sidebar trái | GET `api/ChiaSoCareDon/GetDSPhongBan?idChiNhanh=&isTrucThuoc=` | `ChiaSoCareDonController_GetDSPhongBan` | public | màn chia số | List phòng ban theo chi nhánh (kèm phòng trực thuộc nếu isTrucThuoc) | EF | - |
| 2 | Nhóm NV trong phòng | expand node phòng | POST `api/ChiaSoCareDon/GetDSNhomNVByPhongBan` | `ChiaSoCareDonController_GetDSNhomNVByPhongBan` | public | chọn phòng | List nhóm nhân viên care đơn của phòng | EF | - |
| 3 | Đơn chưa phân bổ (toàn bộ) | grid trung tâm | POST `api/ChiaSoCareDon/GetDSAllDonHangLGTChuaPhanBo` | `ChiaSoCareDonController_GetDSAllDonHangLGTChuaPhanBo` | public | màn chia số | List đơn DonHangLogistic chưa phân bổ về nhóm (phân trang + filter) | EF | - |
| 4 | Đếm đơn chưa phân bổ theo trạng thái giao hàng (cấp NHÓM) | badge cột trạng thái | POST `api/ChiaSoCareDon/ChiaNhomCareDon_GetDonHangChuaPhanBoGroupByTrangThaiGiaoHang` | `ChiaSoCareDonController_GetDonHangChuaPhanBoGroupByTrangThaiGiaoHang` | public | màn chia số | Group số đơn chưa phân bổ theo `GiaoHangTrangThaiMa` qua SP | **SP** `SCRM_CauHinhChiaCareDon_GetDSTrangThaiGiaoHangKemSoLuong` | - |
| 5 | Phân bổ NGAY về nhóm (per-đơn) | "Phân bổ ngay" (cấp nhóm) | POST `api/ChiaSoCareDon/ChiaNhomCareDon_PhanBoNgay` | `ChiaSoCareDonController_ChiaNhomCareDon_PhanBoNgay` | public | chọn đơn + nhóm | Gán `IdNhomCareDon` cho list đơn chỉ định; reset user care đơn về null; có ticket lock + log + ES | EF (ticket) | Nếu trong transaction lỗi → `RollbackAsync` nhưng KHÔNG set cờ lỗi/đổi message (ChiaSoCareDonThuCongService20250620.cs:939) → vẫn báo thành công |
| 6 | Phân bổ HÀNG LOẠT về nhóm (chia đều) | "Phân bổ hàng loạt" (cấp nhóm) | POST `api/ChiaSoCareDon/ChiaNhomCareDon_PhanBoVeNhomUser` | `ChiaSoCareDonController_ChiaNhomCareDon_PhanBoVeNhomUser` | public | nhập SL/nhóm | Round-robin chia đều theo `SoDonHangDuocNhan` từng nhóm, ưu tiên nhóm chia lâu nhất + SL nhiều nhất; ticket lock; log + ES; trả số đơn còn lại chưa phân bổ | EF (ticket) | Xem Quy tắc nghiệp vụ |
| 7 | Danh sách nhóm NV kho (để nhập SL) | grid đối tượng phân bổ | POST `api/ChiaSoCareDon/GetDSNhomNVKho` | `ChiaSoCareDonController_GetDSNhomNVKho` | public | cấp nhóm | List nhóm NV kho + số đơn đã nhận, cột nhập SoDonHangDuocNhan | EF | - |
| 8 | Đơn chưa phân bổ trong 1 nhóm | drill-down nhóm | POST `api/ChiaSoCareDon/GetDSDonHangLGTChuaPhanBoTheoNhom` | `ChiaSoCareDonController_GetDSDonHangLGTChuaPhanBoTheoNhom` | public | đã chia về nhóm | List đơn trong nhóm mà chưa phân bổ tiếp xuống user | EF | - |
| 9 | NV kho theo nhóm (cấp USER) | grid user trong nhóm | POST `api/ChiaSoCareDon/GetDSNhanVienKhoByNhom` | `ChiaSoCareDonController_GetDSNhanVienKhoByNhom` | public | chọn nhóm | List user care đơn của nhóm + SL đã nhận | EF | - |
| 10 | Đếm đơn chưa phân bổ theo trạng thái (cấp USER) | badge | POST `api/ChiaSoCareDon/ChiaUserCareDon_GetDonHangChuaPhanBoGroupByTrangThaiGiaoHang` | `ChiaSoCareDonController_ChiaUserCareDon_GetDonHangChuaPhanBoGroupByTrangThaiGiaoHang` | public | cấp user | Group số đơn (đã chia nhóm, chưa chia user) theo trạng thái giao hàng | EF/SP | - |
| 11 | Phân bổ NGAY về user (per-đơn) | "Phân bổ ngay" (cấp user) | POST `api/ChiaSoCareDon/ChiaUserCareDon_PhanBoNgay` | `ChiaSoCareDonController_ChiaUserCareDon_PhanBoNgay` | public | đơn đã có nhóm | Gán `UserIdCareDon` + CareDonUsername/DisplayName cho đơn; ticket lock; log + ES | EF (ticket) | - |
| 12 | Phân bổ HÀNG LOẠT về user (chia đều) | "Phân bổ hàng loạt" (cấp user) | POST `api/ChiaSoCareDon/ChiaUserCareDon_PhanBoVeUser` | `ChiaSoCareDonController_ChiaUserCareDon_PhanBoVeUser` | public | nhập SL/user | Round-robin chia đều đơn trong nhóm cho từng user theo SoDonHangDuocNhan; ticket lock; log + ES | EF (ticket) | - |
| 13 | Lịch sử chia số theo đơn | popup "Lịch sử chia số" | GET `api/ChiaSoCareDon/GetLogChiaSoByIdDonHang/{idDonHang}` | `ChiaSoCareDonController_GetLogChiaSoByIdDonHang` | public | per-đơn | Trả log chia số (nhóm/user/thời điểm) của 1 đơn | EF | - |
| 14 | Dropdown nhóm care đơn | filter combobox | GET `api/ChiaSoCareDon/GetDsNhomCareDon/{idChiNhanh}` | `[SBAuthorize]` (mở) | public | - | List nhóm care đơn theo chi nhánh | EF | Không gắn permission key riêng |
| 15 | Dropdown user care đơn | filter combobox | POST `api/ChiaSoCareDon/GetDsUserCareDon` + `GetDsAllUserNameCareDon` | `[SBAuthorize]` (mở) | public | - | List user care đơn (lọc / lấy tên all) | EF | Không gắn permission key riêng |

## Quy tắc nghiệp vụ (phân bổ hàng loạt — chia đều round-robin)
Logic ở `ChiaNhomCareDon_PhanBoVeNhomUserAsync20250620` (ChiaSoCareDonThuCongService20250620.cs:292), tương tự cho cấp user `ChiaUserCareDon_PhanBoVeUserAsync`:
1. **Ticket lock trước**: `DoProcessTicket(...PhanBoHangLoatTicketKey)`. Nếu `Data==false` → delay 1s, trả "Chưa đến lượt thực hiện." code -1 (chống 2 user chia cùng lúc → trùng đơn).
2. **Check quyền role kho**: `CheckPermissionViewrReportRoleKho(phòng ban, nhóm NV, chi nhánh)` — fail → trả lỗi, không chia.
3. **Tổng SL phân bổ** = Σ `SoDonHangDuocNhan` các đối tượng. `<= 0` → lỗi "Bạn chưa nhập số liệu phân bổ".
4. **Nguồn đơn**: lấy đơn chưa phân bổ group theo trạng thái, lọc đúng `TrangThaiGiaoHangChiaData` → nạp vào `Queue<DonHangLogistic>`.
5. **Vòng chia đều**: mỗi đơn → sort đối tượng còn quota (`SoDonHangDuocNhan>0`) theo (NgayChiaDonHangGanNhat asc → SoDonHangDuocNhan desc) → gán đơn cho đối tượng đầu, giảm quota, cập nhật NgayChiaDonHangGanNhat=Now (để vòng sau ưu tiên đối tượng khác → chia đều). Đơn đã thuộc đúng đối tượng → chỉ giảm quota, không update.
6. **Guard mỗi đơn**: bỏ qua nếu `IdDonVi != currentUser.DonViId` hoặc `IdChiNhanh != req.IdChiNhanh`.
7. **Gán nhóm** set `IdNhomCareDon`, reset `UserIdCareDon=null` + CareDonUsername/DisplayName="" (chia nhóm khác phải xoá user cũ). **Gán user** set `UserIdCareDon` + tên hiển thị.
8. **Persist trong transaction** + `LichSuTacNghiepLog(old,new, action ChiaCareDon)` per đơn; sau commit reindex ES per đơn. Trả số đơn **còn lại** chưa phân bổ (`dataChuaPhanBoQueue.Count`).

## Quan hệ
- **Gọi sang:** `ICauHinhService.GetAsync(DonViId)` đọc cấu hình kho tổng (`IsSuDungKhoTong`, `IsSuDungChiaSoCareDonKhoTong`, `IsSuDungChiaSoCareDon`, `IdChiNhanhTong`) để quyết phạm vi đơn (chi nhánh tổng được view all đơn toàn đơn vị); SP `SCRM_CauHinhChiaCareDon_GetDSTrangThaiGiaoHangKemSoLuong` đếm đơn theo trạng thái giao hàng.
- **Dữ liệu (entities):** `DonHangLogistic` (fields care đơn: `IdNhomCareDon`, `NgayNhomNhanCareDon`, `UserIdCareDon`, `CareDonUsername`, `CareDonDisplayName`, `CareDonNgayNhan`); `QueueDonHangChiaCareDon` (bảng queue cho job auto-chia — service insert khi cần job chạy nền, ChiaSoCareDonThuCongService.cs:1065); log chia số per-đơn.
- **Ripple / đồng bộ:** mỗi đơn chia lại → reindex Elasticsearch (`ESIndexDonHangLogistic`) + log tác nghiệp kho (`ChiaCareDon`). KHÔNG đụng tồn kho/kế toán.

## Luồng xuyên module (chia số 2 cấp)
```
[cấu hình] CauHinhChiaSoCareDon (Kiểu1 nhóm / Kiểu2 user-liên-kết) + CauHinh kho tổng bật IsSuDungChiaSoCareDon
  → cấp NHÓM: PhanBoNgay (per-đơn) / PhanBoVeNhomUser (chia đều) → set IdNhomCareDon, reset user
  → cấp USER: PhanBoNgay (per-đơn) / PhanBoVeUser (chia đều) → set UserIdCareDon
  → mỗi bước: ticket lock → transaction update DonHangLogistic → LogTacNghiep(ChiaCareDon) → reindex ES
  → user care đơn thấy đơn của mình ở màn tác nghiệp kho
```
Triage "chia trùng / nhảy số": ticket lock (DoProcessTicket) — nếu trả "Chưa đến lượt" là đang có người khác chia. "Chia về nhóm nhưng user vẫn còn cũ": kiểm tra reset `UserIdCareDon=null` (chỉ reset khi đổi nhóm).

## Lỗi đã biết / lưu ý kỹ thuật
- Service là **partial 2 file**: `ChiaSoCareDonThuCongService.cs` (cấp user + queue job) và `ChiaSoCareDonThuCongService20250620.cs` (bản mới cấp nhóm `...20250620`). Endpoint controller trỏ phần lớn vào bản `20250620`.
- `ChiaNhomCareDon_PhanBoNgayAsync20250620` (ChiaSoCareDonThuCongService20250620.cs:939): catch `Exception` chỉ `RollbackAsync()`, KHÔNG set cờ lỗi → method vẫn trả thành công dù rollback (swallow lỗi DB).
- Một số endpoint dropdown (GetDsNhomCareDon/GetDsUserCareDon/GetDsAllUserNameCareDon) dùng `[SBAuthorize]` mở — không phân quyền key.
- Cấu hình kho tổng quyết phạm vi đơn: chỉ chi nhánh tổng (`IdChiNhanhTong`) khi bật đủ 3 cờ kho tổng mới view all đơn toàn đơn vị; còn lại lọc theo `IdChiNhanh` của user (ChiaSoCareDonThuCongService20250620.cs:853).
