# SCRM — QA Spec Checklist

Dùng khi test nghiệp vụ / review logic trước khi deploy.
Gọi bằng: "QA spec đi" → Claude đọc file này và kiểm tra logic của tính năng mới.

---

## Chung — Áp dụng cho mọi module

### Multi-tenant

- [ ] Mọi query phải filter `IdDonVi = userLogin.DonViId` (không lấy data của tenant khác)
- [ ] Mọi insert phải set `IdDonVi` và `IdChiNhanh` từ `userLogin`
- [ ] API không nhận `IdDonVi` từ request body — luôn lấy từ `UserLogin` (tránh giả mạo tenant)

### Soft delete

- [ ] Xóa dùng `DeleteAsync` (soft delete — set `IsDeleted = true`) — không xóa vật lý trừ khi có yêu cầu rõ ràng
- [ ] Không thêm `IsDeleted == false` vào query — đã có global filter

### Response & Error handling

- [ ] Service trả `IMethodResult<T>` — không throw exception ra ngoài (wrap trong `ResultWithError`)
- [ ] Controller trả `ResponseResult(rs)` — không trả trực tiếp object
- [ ] Frontend: luôn check `res.success` trước khi dùng `res.data`

### Authorization

- [ ] Mọi controller action có `[SBAuthorize]` hoặc `[PrivateSBAuthorize]`
- [ ] Internal API (service-to-service) dùng `[PrivateSBAuthorize]`

### Transaction

- [ ] Các thao tác ghi nhiều bảng trong 1 luồng → dùng transaction với `CreateExecutionStrategy`
- [ ] Rollback khi có exception, không để data inconsistent

### UI theo góc nhìn / vai trò (chat, phân quyền hiển thị, "của tôi" vs "của người khác")

- [ ] Hiển thị phụ thuộc **người đang xem** (canh trái/phải bong bóng chat, nút "chỉ tôi sửa", nhãn "bạn/tôi", màu, badge) → **KHÔNG hard-code theo loại người GỬI/tạo**. Phải suy theo cờ vai trò của **viewer** (vd `isSandboxTeam`), so với thuộc tính bản ghi.
- [ ] **Test với TỪNG vai** (SB xem / đơn vị-khách xem / admin / user thường) — cùng 1 data, đổi người xem thì vị trí & quyền phải đổi đúng. Bug canh chat SB=phải lọt lưới vì chỉ test vai SB (2026-07). Xem [lessons/chat-canh-theo-goc-nhin-nguoi-xem.md](lessons/chat-canh-theo-goc-nhin-nguoi-xem.md).

### Trạng thái QUYẾT ĐỊNH (duyệt / phê duyệt / xác nhận / huỷ) — đi theo BỘ, không đi lẻ

Áp cho mọi luồng có người "ra quyết định" trên bản ghi (duyệt yêu cầu, phê duyệt phiếu, xác nhận, khoá sổ…).

- [ ] **Giá trị + người + thời gian (+ lý do) phải đổi CÙNG LÚC.** Đổi trạng thái mà giữ mốc thời gian cũ → màn hình đọc ra *"Đã duyệt — B — 10:00"* trong khi B bấm lúc 15:00 (A từ chối lúc 10:00) = **hiển thị sai sự thật**.
- [ ] **Lùi/huỷ quyết định phải XOÁ HẲN cả bộ** (người + thời gian + lý do), không chỉ đổi mỗi trạng thái. Giữ lại sẽ ra *"Chờ duyệt — nguyenvana — 16:47"*: chưa ai quyết mà vẫn khoe người quyết. Vết "ai lùi lúc nào" nằm ở **lịch sử**, không phải ở các cột này.
- [ ] **Lịch sử phải ghi ĐỦ trường đổi, không chỉ trường trạng thái** — thiếu "lý do từ chối" thì log chỉ có *"Chờ duyệt → Từ chối"*, người đọc lại phải đi hỏi. Ghi cả **chiều XOÁ** (từ chối → duyệt / lùi) để log khớp dữ liệu đang lưu.
- [ ] **Idempotent:** bấm lại đúng trạng thái đang có → không ghi, không đụng mốc thời gian, **không bắn noti lần 2**.
- [ ] **Đổi ngược trạng thái phải có noti riêng**, đừng im lặng: khách vừa nhận *"đã được duyệt"* mà trạng thái đổi ngược không báo thì họ vẫn tin cái cũ.
- [ ] **Một nghĩa ⇒ một cách biểu diễn.** Đừng vừa để `NULL` vừa có mã riêng cùng nghĩa ("không cần duyệt") — sinh ra luật gộp phải nhớ ở **mọi** chỗ đọc (lọc / hiển thị / gác quyền). Chọn 1, chặn giá trị kia ở tầng validate input.
- [ ] **Quyền quyết định gác ở SERVICE**, không tin FE ẩn nút; cờ trả về cho FE (`coTheDuyet`…) chỉ để ẩn/hiện, gọi thẳng API vẫn phải bị chặn. Kèm guard multi-tenant **trước** khi xét vai trò.

> Nguồn: 591-3 duyệt ticket (2026-07-30).

---

## Module: NghiepVu / ChiTieu (thêm 2025-03)

### Luồng chính: Tăng chi tiêu khách hàng

- [ ] Khi đơn đạt trạng thái `DaGiaoHang(31)`, `DaThanhToan(32)`, `GiaoHangMotPhan(35)` → tạo Pending record
- [ ] Pending record có `ThoiGianXuLy = now + PhutDelay` (mặc định 3 ngày = 4320 phút)
- [ ] Sau khi delay: gọi `TangChiTieuAsync` → ghi `ChiTieuLog` với `Huong=1`
- [ ] `GiaoHangMotPhan`: số tiền = `GiaoHangMotPhanCOD + DonHangDatCoc`, các trạng thái khác = `DonHangTienCuoi`
- [ ] Anti-double: nếu `lastLog.Huong == 1` → không tăng lại

### Luồng chính: Giảm chi tiêu khách hàng

- [ ] Khi đơn đạt trạng thái `DaHoan(41)` → tạo Pending record
- [ ] Sau khi delay: gọi `GiamChiTieuAsync` với số tiền = `lastLog.SoTien` (bằng lần tăng trước)
- [ ] Chỉ giảm nếu đã từng tăng (`lastLog.Huong == 1`) và chưa giảm (`lastLog.Huong != -1`)
- [ ] `IdThamChieu` = `IdTransaction` của lần tăng → Customer service trace được

### Anti-double / Edge cases

- [ ] Đơn không có khách hàng (`KhachHangId` null) → bỏ qua, không tạo Pending
- [ ] Trạng thái không nằm trong danh sách tăng/giảm → không tạo Pending
- [ ] Đã có Pending cho cùng (đơn + nghiệp vụ + trigger + trạng thái) → không tạo duplicate
- [ ] `SoTien <= 0` → không xử lý
- [ ] Retry tối đa 3 lần, mỗi lần delay 5 phút → sau đó chuyển History với `TrangThaiXuLy=2` (lỗi)
- [ ] Xử lý thành công: Pending bị xóa, History được tạo với `TrangThaiXuLy=1`

### Cấu hình (DonHangLogisticNghiepVuCauHinh)

- [ ] Tenant chưa cấu hình → dùng default: tăng `31;32;35`, giảm `41`, delay 4320 phút
- [ ] Sau khi save cấu hình → cache bị clear, lần sau lấy từ DB mới
- [ ] Màn hình config hiển thị đúng giá trị hiện tại từ API `GetNghiepVuCauHinh`
- [ ] Save thành công → thông báo success

### Job / Consumer

- [ ] `JobXuLyChiTieuKhachHang` chạy mỗi 5 phút, quét tối đa 100 Pending đến hạn
- [ ] `XuLyNghiepVuConsumer` nhận message từ RabbitMQ, xử lý ngay nếu đến hạn, bỏ qua nếu chưa đến
- [ ] Khi cả consumer lẫn job cùng pick cùng Pending → chỉ 1 cái thành công (Pending bị xóa sau khi xử lý)

---

## Module: WarehouseProduct — Sửa phiếu đã hoàn thành (thêm 2026-07)

### Chặn tồn âm khi sửa phiếu (lô date / IMEI)

- [ ] **SP thường** (không lô/IMEI): KhoChiTiet CHO tồn âm theo spec — KHÔNG chặn. Chỉ chặn tồn âm cho **SP lô date / IMEI**.
- [ ] **Phiếu XUẤT hoàn thành**: tăng SL / thêm dòng vượt tồn khả dụng của lô → phải BÁO LỖI ("Số lượng xuất vượt quá tồn kho của lô..."). Giảm SL / hủy dòng (trả về kho) → cho lưu.
- [ ] Validate chiều XUẤT khác chiều NHẬP: xuất tăng SL, FE phát entry `ThemMoi` phần chênh → validate phải cover `ThemMoi` (đừng tái dùng `KiemTraLoDateBiViPham` của nhập — nó bỏ ThemMoi + ngược dấu).
- [ ] Công thức validate phải MIRROR `ApDungLoDateThayDoiAsync`: `delta = dauTacDong*(SoLuongCu−SoLuongMoi)*tiLe`, cùng cách chọn lô key.
- [ ] **Cộng dồn theo composite key lô** trước khi so tồn: 2 dòng cùng SP+lô, từng dòng lẻ < tồn nhưng TỔNG > tồn → vẫn chặn.
- [ ] Biên "vừa đủ" (SL xuất = tồn) → CHO lưu (so `< -eps`, không phải `< 0`).
- [ ] IMEI: guard `KiemTraImeiThemPhieuXuatKhongHopLe` chặn thêm IMEI không còn trong kho.
- [ ] Quy đổi đơn vị: entry lô date phải gửi `TiLeQuyDoi` khớp `TyLeQuyDoiMacDinh` của line (so tồn ở đơn vị GỐC).
- [ ] Phiếu CHUYỂN KHO sửa-hoàn-thành hiện TẮT (dead code) — chưa validate; bật lại flow mới thêm.

> Chi tiết root-cause/pattern: `lessons/bug_sua_phieu_xuat_hoan_thanh_validate_ton_lodate.md`.

---

## Template cho module mới

Khi thêm nghiệp vụ mới vào NghiepVu framework, kiểm tra:

- [ ] Handler mới implement `INghiepVuHandler` và đăng ký DI
- [ ] Seed default config vào `DonHangLogisticNghiepVuCauHinh` cho tất cả tenant (migration SQL)
- [ ] Màn hình config hiển thị đúng
- [ ] Test tăng → giảm → tăng lại (anti-double hoạt động đúng)
- [ ] Test retry khi handler throw exception
