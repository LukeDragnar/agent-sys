# HBBTL Trigger Flow — Logistics-Gated Design

> Design spec + work plan cho việc trigger HBBTL từ đơn hoàn Shopee dựa trên logistics state.
> Cross-repo: SCRM (Order Logistic + Accounting) ↔ ecommerce_connect.
> Tham chiếu:
> - Shopee spec: [SHOPEE_RETURN_STANDARD_FLOW.md](SHOPEE_RETURN_STANDARD_FLOW.md)
> - Internal flow ECOMM: [RETURNS_FLOW.md](RETURNS_FLOW.md)
> - AgentSystem version (đầy đủ): `AgentSystem/projects/ecommerce_connect/hbbtl_trigger_flow.md`

---

## Context

- **Sprint 1-4 đã DONE** (2026-05-15): nút "Tạo HBBTL" trên row đơn hoàn ecommerce + popup pre-fill + double-create guard + bilateral chain mapping (`CrmOrderItemId`, `IdDonHangHoanSanPham`).
- **Sprint 5 (đang plan)**: Logistics-gated trigger + auto-create config + DELIVERY_FAILED/LOST flow.

---

## Design decisions (confirmed user 2026-05-29)

### 1. Model 2-phase
```
DonHangHoan          = "đơn hoàn đang xử lý" (Draft layer, edit/delete OK)
   ↓ trigger khi đủ điều kiện logistics + config
PhieuBanTraLai (HBBTL) = "hoàn thành" (Final, không undo, kéo theo điều chỉnh hóa đơn TODO)
```

→ KHÔNG cần thêm Draft state trong `PhieuBanTraLai`. Bản thân `DonHangHoan` đã chứa "đang xử lý".

### 2. Logistics-gated trigger (state matrix)

| Logistics state (`reverse_logistic_status`) | HBBTL state | UI badge |
|---|---|---|
| PENDING_ARRANGE / READY / REQUEST_CREATED | KHÔNG tạo | "Chờ buyer gửi" |
| PICKUP_DONE (đang về kho) | KHÔNG tạo HBBTL — chỉ chờ | "Hàng đang về" |
| **DELIVERY_DONE** (đã về kho) | **Cho phép tạo HBBTL Final** | "Đã về kho — sẵn sàng tạo HBBTL" |
| DELIVERY_FAILED | KHÔNG tạo HBBTL — phải khiếu nại | "Giao fail — cần khiếu nại" |
| LOST | KHÔNG tạo HBBTL — phải khiếu nại | "Hàng mất — cần khiếu nại" |

Với `validation_type = warehouse_validation` (FBS): dùng `post_return_logistics_status` (segment 2) thay cho `reverse_logistic_status`.

### 3. Config Auto vs Manual

- Setting cấp **UnitConnect** (per shop) — `AutoCreateHBBTL` (bool, default `false`).
- **Auto = true**: webhook fire `LOGISTICS_DELIVERY_DONE` → SCRM internal call `HBBTL_Save` tự động.
- **Auto = false**: user bấm nút "Tạo HBBTL" trên row (popup Sprint 4 đã có) → tự nhập form → save.

### 4. Constraint quan trọng — KHÔNG đụng logic HBBTL

- KHÔNG sửa `PhieuBanTraLaiService.HBBTL_Save` (Accounting SCRM).
- KHÔNG sửa Sprint 1-4 code đã làm.
- CHỈ "call sang" endpoint `HBBTL_Save` existing — tận dụng as-is.

### 5. Điều chỉnh hóa đơn — **DEFER (TODO)**

- HBBTL final → kéo theo credit note / invoice adjustment.
- Luồng chi tiết defer cho task riêng. Hiện chưa rõ module xử lý.

---

## Field changes

### SCRM: `DonHangHoan` entity (thêm 5 fields)

| Field | Kiểu | Mục đích |
|---|---|---|
| `LogisticsStatusCode` | `VARCHAR(50)?` | Snapshot `reverse_logistic_status` từ webhook |
| `LogisticsStatusUpdatedAt` | `DATETIME?` | Timestamp update logistics |
| `IsHangVeKho` | `BIT` (default 0) | Computed: code == DELIVERY_DONE |
| `TrangThaiHoan` | `VARCHAR(30)?` | Enum: `DANG_XU_LY` / `DA_NHAN` / `GIAO_FAIL` / `MAT_HANG` |
| `IdPhieuBanTraLai` | `UNIQUEIDENTIFIER?` | Link 1:1 HBBTL đã tạo (tracking ngược) |

### ECOMM: `UnitConnect` entity (thêm 1 field)

| Field | Kiểu | Default |
|---|---|---|
| `AutoCreateHBBTL` | `BIT` | `0` |

### DTO: `CrmOrderReturnModel.ReturnDataCrm` (thêm field forward)

| Field | Kiểu | Note |
|---|---|---|
| `LogisticsStatusCode` | `string?` | Forward `reverse_logistic_status` sang SCRM |

---

## Flow chi tiết

### Flow A — Auto-create HBBTL

```
Shopee webhook 29 (push) → ECOMM receive (PushNotificationService)
  ↓ update_field == "logistics_status" && new_value == "LOGISTICS_DELIVERY_DONE"
  ↓
ECOMM OrderReturnShopeeService:
  - Check UnitConnect.AutoCreateHBBTL
  - Call SCRM endpoint mới: POST /api/DonHangHoan/UpdateLogisticsStatus
    Body: { idDonHangHoan, logisticsStatusCode, timestamp, autoCreate=true }
  ↓
SCRM DonHangHoanService.UpdateLogisticsStatus:
  - Update DonHangHoan.LogisticsStatusCode + IsHangVeKho + TrangThaiHoan = DA_NHAN
  - if autoCreate == true:
    - Build default args (lookup từ TaiKhoanNgamDinh / Kho mặc định của DonVi)
    - Internal call HBBTL_PrefillFromDonHangHoan + HBBTL_Save
    - Update DonHangHoan.IdPhieuBanTraLai
  ↓
HBBTL Final created → trigger điều chỉnh hóa đơn (TODO)
```

⚠️ **Pain point Auto**: HBBTL_Save expects user input (idTaiKhoanChi, idKho, idPhuongThucChi, hinhThucHoanTien, luaChonHoaDon, isKiemNhapKho). Cần default config — lookup từ đâu? Đề xuất:
- `idTaiKhoanChi` ← `TaiKhoanNgamDinh.HBBTL` của DonVi (có sẵn)
- `idKho` ← `Kho mặc định` của DonVi/UnitConnect
- `idPhuongThucChi` ← cấu hình mặc định
- `isKiemNhapKho` ← false (đơn ecommerce thường skip)
- `hinhThucHoanTien` ← mặc định "TienMat" / "ShopeeWallet"
- `luaChonHoaDon` ← TODO (e-invoice)

→ **Sub-task design**: Add config fields cho auto-default args (có thể ở UnitConnect hoặc DonVi setup riêng).

### Flow B — Manual create HBBTL (giữ nguyên Sprint 4)

```
Shopee webhook 29 → ECOMM receive
  ↓
ECOMM call SCRM UpdateLogisticsStatus với autoCreate=false
  ↓
SCRM update DonHangHoan logistics fields only
  ↓
User vào màn "Đơn hoàn" → row hiện badge "Đã về kho"
  ↓
User bấm nút "Tạo HBBTL" (Sprint 4 đã có) → popup pre-fill → user save → HBBTL_Save
```

### Flow C — DELIVERY_FAILED / LOST

```
Webhook 29 fire logistics_status = LOGISTICS_DELIVERY_FAILED hoặc LOGISTICS_LOST
  ↓
ECOMM forward logistics code sang SCRM (UpdateLogisticsStatus)
  ↓
SCRM update DonHangHoan:
  - LogisticsStatusCode = "LOGISTICS_DELIVERY_FAILED" / "LOGISTICS_LOST"
  - TrangThaiHoan = "GIAO_FAIL" / "MAT_HANG"
  - IsHangVeKho = false (giữ nguyên)
  ↓
SCRM FE hiển thị:
  - Badge đỏ "Hàng không về kho — Cần khiếu nại" / "Hàng mất — Cần khiếu nại"
  - Button "Khiếu nại" (DEFER logic — chỉ log + TODO)
```

⚠️ Future: button "Khiếu nại" → SCRM call ECOMM → ECOMM call Shopee `v2.returns.dispute` (constraint: chỉ accept status ∈ {REQUESTED, PROCESSING}).

---

## Phase rollout

### Phase 1 — Backbone (Logistics sync) — `KHỞI ĐẦU`
- [ ] SCRM `DonHangHoan` entity — add 5 fields
- [ ] SQL migration sprint 5 (1 ALTER `DonHangHoan`)
- [ ] SCRM `DonHangHoanService` — method `UpdateLogisticsStatus(idDonHangHoan, code, time, autoCreate)` + Controller endpoint
- [ ] ECOMM `CrmOrderReturnModel.ReturnDataCrm` — add field `LogisticsStatusCode`
- [ ] ECOMM `OrderReturnShopeeService` — khi webhook trigger `logistics_status` change → POST endpoint mới SCRM
- [ ] Test E2E: webhook → DonHangHoan có logistics fields cập nhật

### Phase 2 — Auto-create HBBTL config
- [ ] ECOMM `UnitConnect` — add `AutoCreateHBBTL` (bool)
- [ ] SQL migration: ALTER `UnitConnect`
- [ ] SCRM `UpdateLogisticsStatus`: if config auto + DELIVERY_DONE → internal call `HBBTL_PrefillFromDonHangHoan` + `HBBTL_Save`
- [ ] **Sub-task design**: default args cho HBBTL_Save (TaiKhoanNgamDinh / Kho default / phương thức chi)
- [ ] Test E2E: webhook DELIVERY_DONE + AutoCreateHBBTL=true → HBBTL tự tạo

### Phase 3 — UI
- [ ] SCRM FE `don-hang-hoan.component.html` — badge "Hàng đang về" / "Đã về kho" / "Giao fail"
- [ ] Filter nút "Tạo HBBTL" — chỉ enable khi `IsHangVeKho = true`
- [ ] Button "Khiếu nại" — chỉ show khi `TrangThaiHoan ∈ {GIAO_FAIL, MAT_HANG}` — DEFER logic call Shopee (chỉ log)
- [ ] Cập nhật cột table list đơn hoàn

### Phase 4 — Defer
- [ ] Khiếu nại → call Shopee `v2.returns.dispute` (constraint REQUESTED/PROCESSING)
- [ ] Điều chỉnh hóa đơn khi HBBTL final
- [ ] Cleanup duplicate OrderDataExt job (race fallback)

### Phase 5+ — SCRM Operations Center (ERP-style Seller Center)

> Scope MỚI (user 2026-05-29): SCRM cho seller làm gần như mọi thao tác Seller Center trực tiếp từ ERP.
> Chi tiết xem [SELLER_HANDLING_GUIDE.md](SELLER_HANDLING_GUIDE.md) §8.
>
> **🔑 Constraint critical**: UI **dùng CHUNG cho TẤT CẢ sàn (Shopee/TikTok/Lazada) + đơn non-sàn**. KHÔNG cá nhân hóa. Partner-specific code ở ECOMM adapter layer.

**Phase 5 — Backend proxy** (5-7d effort):
- [ ] SCRM endpoints 6 action: UploadProof, Confirm, Offer, AcceptOffer, Dispute, CancelDispute
- [ ] ECOMM Controller forward → existing service methods
- [ ] Authorization theo UnitConnect / DonVi

**Phase 6 — Frontend** (7-10d effort):
- [ ] 3 tab redesign (Trả hàng/Hoàn tiền, Đơn Hủy, Đơn Giao hàng không thành công)
- [ ] Sidebar 5 Hành động quan trọng
- [ ] Form modal cho mỗi action
- [ ] Reverse tracking timeline component
- [ ] Badge deadline countdown

**Phase 7 — Notifications**:
- [ ] Job cron daily check DeadlineKhieuNai sắp hết hạn
- [ ] Webhook → realtime UI update (signalr)

**Phase 8 — Audit + Reporting**:
- [ ] Log audit trail action seller bấm từ SCRM
- [ ] Dashboard metrics

---

## Open questions

1. Auto-create default args lưu đâu? Cần design phụ.
2. Khi `validation_type = warehouse_validation` (FBS), logistics field nào dùng? `post_return_logistics_status`? Phase 1 chỉ dùng `reverse_logistic_status` (Normal RR) — FBS support sau.
3. Khi nhiều đơn hoàn cùng `IdDonHangLogistic` (1 đơn bán → N đơn hoàn), auto-create cho cái nào? → Cho đơn hoàn cụ thể đang fire webhook.

---

## Code touch points trong ECOMM repo

| File | Sửa gì |
|---|---|
| `EcommerceConnect.DataContext/Entity/UnitConnect.cs` | + `AutoCreateHBBTL` field |
| `EcommerceConnect.DataContext/Model/Crm/CrmOrderReturnModel.cs` | + `LogisticsStatusCode` trong `ReturnDataCrm` |
| `EcommerceConnect.Service/Generated/MySystem/OrderReturnShopeeService.cs` | Khi `logistics_status` change → POST SCRM endpoint mới |
| (Optional) `EcommerceConnect.Service/Generated/MySystem/PushNotificationService.cs` | Có thể sync ngay tại receive (Phase 1) thay vì timer job |

---

## Constraint reminder

- ✅ KHÔNG đụng logic `PhieuBanTraLaiService.HBBTL_Save` (Accounting SCRM)
- ✅ KHÔNG sửa Sprint 1-4 code (chỉ extend bằng field mới + flow trigger)
- ✅ Bilateral chain mapping đã có (`CrmOrderItemId`, `IdDonHangHoanSanPham`) — tận dụng

---

## 2026-06 — Đã implement (combo decompose + chiết khấu sàn + chống clobber)

### 1. Combo decompose (mirror PBH) — `Ecommerce/DonHangHoanService.BuildSanPham`
Đơn hoàn ECOMM gửi **cấp combo** (`Loai=2`, `CrmProductId==CrmSkuId`). Đơn bán gốc lưu khác id-space:
`DonHangLogisticSanPham.IdCombo` = `DonHangLogisticCombo.Id` (**Guid nhóm per-đơn**) ≠ `DonHangLogisticCombo.IdComboSpa` (**master combo id** = CrmProductId). Match combo bằng `IdCombo==CrmProductId` → **vĩnh viễn sai** (cột đơn gốc ra "—").
→ **Fix**: match `DonHangLogisticCombo.IdComboSpa == CrmProductId` → lấy child lines → **nổ thành N dòng** `DonHangHoanSanPham` (`Loai=1`, `IdSanPham`=child, FK mới `IdDonHangLogisticCombo`=combo header, `IdDonHangLogisticSanPham`=child line). Lý do mirror: luồng PBH (`DonHangKeToanService`) đẩy **SP con** kèm `TenCombo`, KHÔNG đẩy dòng combo (block combo-as-line comment out). Tiền/SL: **net đơn gốc × tỉ lệ SL hoàn** (không phân bổ lại refund). Chi tiết: memory `reference_don_hoan_combo_decompose`.

### 2. Chiết khấu sàn (TMDT trợ giá) — field `TienChietKhauSan`
Lệch `net CRM − tiền sàn hoàn` = phần sàn trợ giá. **HBBTL đảo theo net CRM, CK sàn KHÔNG vào HBBTL** (sự kiện riêng). Thêm `DonHangHoanSanPham.TienChietKhauSan` (per-SP) + `DonHangHoan.TienChietKhauSan` (header Σ). Auto-seed khi đơn về = `lineGoc.TMDTSanChietKhau × ratio`; user sửa per-SP ở detail → `POST DonHangHoan/UpdateChietKhauSan`. Chưa đẩy HBBTL (groundwork điều chỉnh + xuất phiếu sau). Chi tiết: memory `reference_don_hoan_chiet_khau_san`.

### 3. Chống clobber khi re-sync (E1 + merge giữ edit tay)
Update path rebuild items = **delete+insert** (Id mới) — **chỉ khi `IdPhieuBanTraLai == null`** (E1: đã có HBBTL → đóng băng items, tránh mồ côi ref). Trước persist, merge từ `oldItems` theo **khóa identity SP** (`IdSanPham|IdDonHangLogisticCombo` / `IdCombo`, KHÔNG theo Id row): giữ `TienChietKhauSan` user sửa + khôi phục mapping tay (`IdDonHangLogisticSanPham`) + snapshot khi auto-match mới null mà cũ đã map. Chi tiết: memory `feedback_don_hoan_resync_preserve_manual`.

### Field/Cột mới (ALTER)
- `DonHangHoanSanPham`: `IdDonHangLogisticCombo UNIQUEIDENTIFIER NULL`, `TienChietKhauSan FLOAT NULL`
- `DonHangHoan`: `TienChietKhauSan FLOAT NULL`

### Webhook reverse tracking (Gap E — mã vận đơn hoàn)
Webhook job `ProcessSendOrderReturnToCrm` (ECOMM) giờ gọi `GetReverseTrackingInfo` khi logistics đổi sang mốc có tracking (`IsReverseTrackingAvailable`, REQUEST_CREATED trở đi) → điền `OrderReturnCode3Pl` → SCRM map `LogisticsMaVanDon`. Chi tiết: [shopee_reverse_logistics_tracking.md](shopee_reverse_logistics_tracking.md) §6 Gap E + §8.
