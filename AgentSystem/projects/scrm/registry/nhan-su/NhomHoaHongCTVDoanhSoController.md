---
controller: NhomHoaHongCTVDoanhSoController
module: nhan-su
service: CommissionFee
man_hinh_ui: "Hoa hồng CTV > Nhóm hoa hồng > tab Hoa hồng theo doanh số (bảng các mức bậc thang)"
muc_dich: "Cấu hình hoa hồng theo bậc doanh số cho 1 nhóm hoa hồng CTV: tối đa 10 mức, mỗi mức 1 khoảng doanh số [Từ, Đến) liên tục, gắn %HoaHongCTV và %HoaHongLienKet."
alias: [NhomHoaHongCTVDoanhSo, "hoa hồng doanh số", "bậc doanh số", "tier doanh số"]
auth: public
state_machine: []
deprecated: false
entities: [CauHinhHoaHongDoanhSoCTV, NhomHoaHongCTV]
stored_procs: []
depends: [NhomHoaHongCTV]
cross_module_in: []
cross_module_out: [CommissionFeeUpdateOrder (dùng % để tính hoa hồng), CongTacVien (GetStats_DoanhSo)]
---

# NhomHoaHongCTVDoanhSo — Cấu hình hoa hồng theo bậc doanh số

## Hành động
| # | Hành động | Nhãn UI/nút | Endpoint | Quyền (key) | Auth | Hành vi đúng (expected) | Nguồn | Known-issue |
|---|---|---|---|---|---|---|---|---|
| 1 | Lưu cấu hình bậc doanh số | Lưu | POST `HoaHongDoanhSoThemMoi` | `NhomHoaHongCTVDoanhSo_HoaHongDoanhSoThemMoi` (AD-TONG/CN/MCN) | public | Validate toàn bộ danh sách mức → **clear toàn bộ cấu hình cũ của nhóm** → insert lại danh sách mới (kiểu replace-all). | EF | Replace-all: lưu = xóa hết rồi ghi lại |
| 2 | Tìm cấu hình theo nhóm | (load tab) | POST `TimKiemTheoDieuKien` | `NhomHoaHongCTVDoanhSo_TimKiemTheoDieuKien` | public | Trả các mức của nhóm, sắp theo `MucHoaHong`, tên = "Hoa hồng mức {MucHoaHong}". Non-admin chỉ xem nhóm cùng chi nhánh. | EF | — |

## Quy tắc nghiệp vụ (validate bậc doanh số — blueprint trace)
Danh sách `DanhSachHoaHongDoanhSo` được sort theo `MucHoaHong`, duyệt với `stt` chạy 1..n:
1. Bắt buộc có `IdNhomHoaHongCTV` + nhóm tồn tại; non-admin → nhóm phải cùng `IdChiNhanh`.
2. `MucHoaHong` không null, **≤ 10** (tối đa 10 bậc).
3. **Mức rỗng**: nếu `DoanhSoTu == 0 && DoanhSoDen == 0` → coi như mức trống, ép `HoaHongCTV=0, HoaHongLienKet=0`, **bỏ qua** các check còn lại của mức (continue).
4. `MucHoaHong` phải bằng `stt` (đúng thứ tự liên tục 1,2,3...) — sai → lỗi "không hợp lệ mức {stt}".
5. `DoanhSoTu`, `DoanhSoDen` không null, **≥ 0**; `DoanhSoDen` **phải > `DoanhSoTu`**.
6. `HoaHongCTV` không null, trong **[0,100]**; `HoaHongLienKet` (nếu có) trong **[0,100]**.
7. **Liên tục bậc thang**: từ mức 2 trở đi, `DoanhSoTu` của mức N phải **bằng** `DoanhSoDen` của mức N-1 (không hở, không chồng). Mức N-1 phải tồn tại trước khi thêm mức N.
8. `IsSuDung` luôn bị ép `= true` khi lưu.

> Ví dụ hợp lệ: M1 [0; 10tr) 3%, M2 [10tr; 50tr) 5%, M3 [50tr; 100tr) 7%. Lỗi nếu M2.Từ=11tr (≠ M1.Đến=10tr) → "Doanh số từ của mức 2 phải bằng doanh số đến của 1".

## Quan hệ
- **Gọi sang:** chỉ EF (validate + clear + insert).
- **Dữ liệu (entities):** `CauHinhHoaHongDoanhSoCTV` (mỗi dòng = 1 bậc), thuộc `NhomHoaHongCTV`.
- **Ripple/đồng bộ:** % theo bậc được `CommissionFeeUpdateOrder` / báo cáo `GetStats_DoanhSo_*` của CongTacVien dùng để tính hoa hồng theo tổng doanh số CTV.

## Lỗi đã biết / lưu ý kỹ thuật
- `NhomHoaHongCTVDoanhSoService.cs:73` — `HoaHongDoanhSoThemMoi` là **replace-all** (clear toàn bộ rồi insert): nếu insert lỗi sau khi đã clear → mất cấu hình cũ (clear + insert KHÔNG bọc trong 1 transaction).
- `NhomHoaHongCTVDoanhSoService.cs:185` — mức có `DoanhSoTu=0 && DoanhSoDen=0` bị skip toàn bộ validate liên tục → có thể tạo "lỗ" giữa các bậc nếu xen mức rỗng giữa chừng.
- Validate viết tay 10 nhánh `stt==1..10` (lặp lại) → khó mở rộng quá 10 mức; rule giới hạn cứng 10 bậc.
