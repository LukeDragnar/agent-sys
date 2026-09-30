# SCRM — Lessons (CHỈ bug-sự-cố thật)

> **Lessons** = bài học rút ra từ **sự cố đã xảy ra** (reactive, "lần đó bị X vì Y"). KHÁC convention (quy tắc "luôn/không bao giờ", proactive → `../convention/` + `../patterns.md`) và recipe (`../skills/`).

## Trạng thái (2026-06-20)

Các file convention bị gán nhãn nhầm "lesson" → đã **tách sang `../convention/`** (folder convention chi tiết, ngang hàng folder này) + tóm tắt ở `../patterns.md`/`../qa-code.md`. **File cũ trong folder này giữ tạm**, xóa sau khi test/duyệt (TODO session).

| File cũ (lessons/, chờ xóa) | Đã có ở |
|---|---|
| `feedback_scrm_canonical_partner_field_pair.md` | `../convention/canonical-partner-field-pair.md` |
| `feedback_accounting_form_domain_agnostic.md` | `../convention/form-domain-agnostic.md` |
| `feedback_ngzorro_disable_attribute.md` | `../convention/ngzorro-disable-attribute.md` |
| `convention_scrm_backend.md` | đã có trong `../patterns.md` (BE core). *Phần SetInfoBase là pattern ECOMM (nhãn sai) → `../../ecommerce_connect/patterns.md`* |

## Lesson thật

| File | Case |
|---|---|
| `bug_sua_phieu_xuat_hoan_thanh_validate_ton_lodate.md` | Sửa phiếu XUẤT hoàn thành thiếu validate tồn lô date → tăng SL vượt tồn vẫn lưu. Validate xuất ngược dấu nhập + FE phát `ThemMoi` + phải cộng dồn theo lô + mirror `ApDungLoDateThayDoiAsync` (session 2026-07-01). |
| `cherry-pick-staging-i18n-la-stub.md` | Cherry-pick FE sang `release_staging`: i18n JSON là STUB (staging load dịch từ lang DB), đừng bê bản product 10k dòng đè. Giữ `--ours`, seed key vào DB. Kèm bẫy deps churn (`npm i --legacy-peer-deps`) + launchSettings debug không commit (session 2026-07-13, B161 Lazada). |
| `sua-orderlogistic-quen-order-song-song.md` | Sửa logic xuất HĐ (CCCD + B2 nguồn TT người mua) chỉ ở OrderLogistic, QUÊN module Order song song (đơn bán lẻ/offline) → suýt ship thiếu 1 luồng đơn. 2 `DonHangInvoiceService.cs` riêng cùng bộ hàm → sửa 1 phải rà cái kia (session 2026-07-16). |
| `gate-endpoint-dung-chung-2-muc-dich.md` | `GetSaleSbTeam` gate admin-only nhưng phục vụ cả bộ lọc ticket của SB team → SB non-admin nhận `[]` (UI hiện ô lọc mà data rỗng, không 401). Fix: gate = admin OR thành viên đơn vị SB team (khớp `laSandbox`). Kèm ghi chú OR-in-LINQ vi phạm perf convention (defer) (session 2026-07-20). |
| `noti-noi-bo-2-duong-2-db.md` | Noti nội bộ có **2 đường** (HTTP `api/Notifications/Create` → `SCRM_Notification` · MassTransit `CongViecNotiInfo` → `SCRM_WorkFlowNoti`, chuông FE đọc đường này). 🔴 SB.Core `INotificationService.SendNotification` build URL **thiếu `/api` → 404 im lặng** — đừng dùng, publish `CongViecNotiInfo` (đặt `Action`+`DataThem`, reuse consumer công việc). FE phân tab chuông theo `data["Action"]`. Gotcha: tạo ticket không bắn noti (chỉ chat/đổi trạng thái, trừ actor); NgayTao Mongo lưu UTC (session 2026-07-22, 591-9). |
| `cod-giao-1-phan-webhook-ghi-de-0.md` | COD giao-1-phần (`DonHangLogistic.GiaoHangMotPhanCOD`) bị webhook DVVC (AUTO) đè 0 → chi tiêu/công nợ khách thiếu. 2 tầng: delivery_status `VtpService` (nhánh `EndsWith("1P1")`) không bơm `CodSignPart` (số ở `orderDb.ReturnCod`); CRM `ReceiveStatusOrder` ghi đè vô điều kiện null→0. Fix pattern **"0=no-value → skip"** (tiền: `ConvertToDouble>0`, mã: `!="0"`), CRM agnostic không branch đối tác; vá luôn nhóm `DonHangHoan*`. Bẫy cherry-pick: staging đọc `ReturnCod` ≠ product đọc `CodSignPart` (lệch LOGIC). Kèm query debug LogData/DataReceive + gotcha "Không thỏa mãn điều kiện" = skip 104+100 vô hại (session 2026-07-24). |
| `grid-column-field-trung-save-config-die.md` | FE `GridColumn` khai **2 cột cùng `field`** (`giaoHangNgayGiaoHang` trong `don-hang-sale.component.ts`) → **lưu cấu hình cột chết** (save config key theo `field` → collision). Lọt review+trace vì: review soi field MỚI không soi cột anh em bị nhân đôi; cả 2 GridColumn hợp lệ đứng riêng (diff không thấy trùng tầm mảng); chỉ vỡ runtime "save config" mà trace không cover. Rút: đụng mảng cột/list-có-key → grep TOÀN mảng tìm key trùng + trace case "Lưu cấu hình cột" (session 2026-07-24, 1046). |
| `hoadon-combo-vat-double.md` | HĐ combo "X tặng Y" gấp đôi tổng: `task-suaCombo` gán `SoTienVat` cho dòng SP con combo → Σ vào header `DonHangTongVatSp` → `DonHangSoTienVatTongSauPhanBo` phồng → invoice `:2010` (Net−CK+VAT) double. Anchor đúng = `DonHangTienThuKhach`/`DonHangTienSanPham`; detector `(Net−CK+VatHeader)≠ThuKhach` + guard `ThuKhach≥Net−CK`. Vá `task-hotFixDonOn` (07-24, deploy 4.4146). Bài học: **vá KHÔNG hồi tố** (đơn tồn/đã chốt vẫn hỏng → phải recompute/fix data); **header tự lành bẫy detector** → tìm đủ đơn đã-xuất-sai phải soi nguồn HĐ phát hành thật (session 2026-07-24). |

| `sp-quy-doi-gui-dvvc-sai-don-vi.md` | SP bán theo đơn vị quy đổi: `GenerateProduct` gửi ĐVVC bằng `SoLuong` (đơn vị NHỎ) trong khi `DonGia` là giá đơn-vị-BÁN → SL và giá trị đơn đều gấp `TiLeQuyDoi` lần (VTP hiện 19.160.000 thay vì 1.9tr). Cùng 1 dòng code sinh 2 mã việc: B265 "sai số lượng" + B262 "sai giá trị". Bẫy: controller hãng chỉ là stub — logic ở `OrderService`; 2 nhánh dựng Products (tính phí lấy Mongo / đăng đơn lấy DB) phải sửa cả hai. Fix `746cab8387`, deploy 4.4140 (session 2026-07-28). |

| `ghim-footer-nz-drawer-co-tabs.md` | Ghim nút Lưu đáy `nz-drawer` **có `nz-tabs`**: cả `position: sticky` (phải ĐOÁN padding `.ant-drawer-body` → dải trắng cắt ngang form) lẫn `[nzFooter]` (cắt mất phần cuối tab, `min-height:0` không cứu) đều hỏng → trả nút về trong luồng nội dung. Giữ `overflow: visible` cho `.ant-tabs*` + `[nzAnimated]="false"` vì antd clip form dài trong tab. Rút: đừng đoán số đo nội bộ thư viện; agent không build được thì thử 1 lần không xong phải DỪNG, đừng vá mò (session 2026-07-30, 591-3). |

Đơn-hoàn (return TMĐT) detail đã dời `memory/local`. Folder này để dành cho bug-sự-cố thật sau này (mỗi cái 1 dòng, gắn case cụ thể).

> Liên quan: đơn hoàn cũng đụng `../../ecommerce_connect/`.
