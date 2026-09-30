---
controller: SendDataPartnerController
module: ke-toan
service: Accounting
man_hinh_ui: "Kế toán > các màn danh sách phiếu (phiếu nhập/xuất/chuyển kho/thu chi/bán hàng/mua hàng/bù trừ/chứng từ khác/trả lại) — nút 'Đẩy sang đối tác' / 'Gửi MISA' sau khi tích chọn các phiếu chưa đồng bộ."
muc_dich: "Đẩy THỦ CÔNG (manual) các phiếu kế toán đã chọn sang đối tác kế toán ngoài (MISA) qua API: gom phiếu + chi tiết theo loại, gọi HandleDataSendPartner từng phiếu, cập nhật trạng thái đồng bộ (WaitingStatusPartner/ErrorCallApi) + thông điệp lỗi lên phiếu gốc."
alias: ["đẩy phiếu sang misa", "gửi đối tác kế toán", "đồng bộ thủ công misa", "send data partner", "đẩy chứng từ kế toán", "gửi phiếu sang đối tác"]
auth: internal
state_machine: [ChuaDongBo, WaitingStatusPartner, Success, ErrorCallApi]
deprecated: false
entities: [ThuChi, PhieuNhap, PhieuXuat, PhieuChuyenKho, PhieuMuaHang, PhieuBanHang, BuTruCongNo, ChungTuKhac, PhieuTraLaiHangMua, PhieuBanTraLai]
stored_procs: []
depends: [PartnerService/MISA (IPartnerService, IMisaService, IMisaRequestConverter), CauHinhKetNoiKeToan, JobTaoPhieuPartner]
cross_module_in: []
cross_module_out: [MISA (đẩy chứng từ kế toán qua API ngoài)]
---

# SendDataPartnerController — Đẩy thủ công phiếu kế toán sang đối tác (MISA)

> 1 endpoint duy nhất: `SendDataManualToPartner`. Dùng khi phiếu chưa được đẩy tự động (vd đơn vị chưa cấu hình tự động, hoặc đẩy lại phiếu lỗi). Nhận `RequestSendDataManualToPartner{ TypePhieu, ListIds }`, gom phiếu + chi tiết theo `TypePhieu` (switch 12 loại phiếu), lọc bỏ phiếu đã `Success`/`WaitingStatusPartner`, gọi `HandleDataSendPartner` từng phiếu (chuyển đổi sang request MISA + gọi API), rồi cập nhật `StatusSendPartner` + `ErrorMessagePartner` lên phiếu gốc. Trả `{TotalSuccess, TotalFailed}`. Đây là tích hợp NGOÀI (MISA), không phải cross-module nội bộ SCRM.

## Hành động (UI / công khai)

| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Đẩy phiếu sang đối tác | "Đẩy sang đối tác" / "Gửi MISA" (sau khi tích chọn) | `POST api/SendDataPartner/SendDataManualToPartner` | `[SBAuthorize()]` (any login) | internal | có chọn ≥1 phiếu chưa đồng bộ | Gom phiếu theo `TypePhieu` (ThuChi/Nhập/Xuất/Chuyển kho/Mua hàng/Bán hàng/Bù trừ/Chứng từ khác/Trả lại hàng mua/Trả lại hàng bán); chỉ lấy phiếu `StatusSendPartner ∉ {Success, WaitingStatusPartner}` (ThuChi/Nhập còn loại `PartnerSyncByPhieuGoc=true`). Mỗi phiếu → `HandleDataSendPartner` (convert + gọi MISA). Set `WaitingStatusPartner` nếu OK, `ErrorCallApi`+message nếu lỗi. Trả `{TotalSuccess, TotalFailed}`. | **EF** từng repo theo loại + HTTP MISA | (a) `PhieuChuyenKho`: dòng gán `IdChiNhanh` bị comment (L1289) → request thiếu chi nhánh. (b) Nhiều loại set `IdTransaction = phieu.Id` thay vì `phieu.IdTransaction` (PhieuMuaHang L1316, BanHang L1344, ChungTuKhac L1397, TraLai...) → IdTransaction không khớp transaction gốc; cần xác nhận MISA có phụ thuộc field này. |

## Quy tắc nghiệp vụ (gating / tier / ngưỡng)
- **Lọc phiếu đủ điều kiện gửi:** chỉ phiếu thuộc `IdDonVi` user + `Id ∈ ListIds` + `StatusSendPartner ≠ Success` + `≠ WaitingStatusPartner`. Riêng ThuChi & PhieuNhap còn loại `PartnerSyncByPhieuGoc == true` (phiếu ăn theo phiếu gốc → không tự gửi).
- **Mapping SubTypePhieu (chỉ ThuChi):** theo `LoaiQuy` → `TienMat` (QuyTienMat) hoặc `TienGui` (NganHang/ViDienTu/Momo/Zalopay/Vnpay/QuetThe). Phiếu kho/mua/bán → `SubTypePhieu = ""`.
- **Cập nhật trạng thái sau gửi:** OK → `WaitingStatusPartner` (chờ MISA phản hồi async qua webhook/job khác); lỗi → `ErrorCallApi` + lưu `ErrorMessagePartner`.
- **Đối tác cứng `"MISA"`:** mọi nhánh đặt `PartnerName = "MISA"` (hard-code) — hiện chỉ hỗ trợ MISA.

## Quan hệ
- **Gọi sang:** `HandleDataSendPartner` → `PartnerService`/`MisaService`/`MisaRequestConverter` (convert + gọi API MISA); đọc `CauHinhKetNoiKeToan` để lấy thông tin kết nối.
- **Dữ liệu (entities):** đọc + cập nhật `StatusSendPartner`/`ErrorMessagePartner` trên 10+ loại phiếu (ThuChi, PhieuNhap, PhieuXuat, PhieuChuyenKho, PhieuMuaHang, PhieuBanHang, BuTruCongNo, ChungTuKhac, PhieuTraLaiHangMua, PhieuBanTraLai).
- **Ripple / đồng bộ:** đẩy chứng từ ra **MISA** (hệ thống kế toán ngoài). Trạng thái cuối (`Success`) thường được cập nhật bởi luồng phản hồi async khác, không trong request này (request chỉ đặt `WaitingStatusPartner`).

## Luồng xuyên module (đẩy thủ công sang MISA)
```
User tích chọn phiếu (vd PhieuNhapKho) trên màn KT → bấm "Đẩy sang đối tác"
  → POST SendDataManualToPartner { TypePhieu=PhieuNhapKho, ListIds }
  → switch theo TypePhieu: load phiếu + chi tiết, lọc bỏ phiếu đã Success/Waiting
  → foreach phiếu: HandleDataSendPartner
        → convert sang request MISA (MisaRequestConverter) → gọi API MISA
        → OK: StatusSendPartner=WaitingStatusPartner ; lỗi: ErrorCallApi + ErrorMessagePartner
  → trả { TotalSuccess, TotalFailed }
  (MISA phản hồi thành công thực sự → cập nhật Success ở luồng async khác)
```

## Lỗi đã biết / lưu ý kỹ thuật
- BE `Accounting/Accounting.API/Controllers/SendDataPartnerController.cs` — 1 endpoint `[SBAuthorize()]` rỗng.
- BE `Accounting/Accounting.Service/Generated/DongBoPhieuCrmService.cs:1163-1699` (`SendDataManualToPartner`):
  - L1289 — `PhieuChuyenKho`: dòng `IdChiNhanh = phieu.IdChi...` bị comment → DataSendPartnerRequest thiếu IdChiNhanh cho chuyển kho.
  - L1316 / L1344 / L1397 / L1423 / L1449 — `PhieuMuaHang`, `PhieuBanHang`, `ChungTuKhac`, `PhieuTraLaiHangMua`, `PhieuTraLaiHangBan` set `IdTransaction = phieu.Id` (không phải `phieu.IdTransaction`) — khác các loại còn lại; *cần xác nhận* có chủ ý không (rủi ro lệch IdTransaction khi đối soát).
  - Toàn bộ bọc trong 1 try/catch lớn → 1 phiếu ném exception ngoài vòng cập nhật có thể làm rớt cả batch; trong vòng thì mỗi phiếu cập nhật riêng.
- Hard-code `PartnerName = "MISA"` ở mọi nhánh → muốn hỗ trợ đối tác khác phải sửa nhiều chỗ.
- Phân biệt với luồng đồng bộ TỰ ĐỘNG: việc đẩy tự động (khi nhận phiếu từ CRM) hiện đang bị **comment** trong `XuLyThuChi`/`ProcessSavePhieuCrmDb` (DongBoPhieuCrmService.cs:315-326, 497-556) → thực tế phiếu chỉ ở trạng thái `Received`, phải đẩy THỦ CÔNG qua controller này. Đây là điểm mấu chốt khi triage "phiếu không tự lên MISA".
