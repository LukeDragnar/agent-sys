---
name: scrm-interface-inline-service
description: SCRM BE — interface phải đặt cùng file với service impl, không tách file IXxxService.cs riêng
metadata:
  type: feedback
---

Khi viết service BE cho SCRM, **interface và impl đặt chung 1 file** — không tách file `IXxxService.cs` riêng. Cả 2 cùng namespace, cùng file `.cs`, interface đặt phía trên class.

**Why:** Đây là convention team SCRM. User đã yêu cầu refactor lại 3 service `IGiaVonRecalculator`, `IGiaVonTheoKyApplyService`, `IGiaVonTheoKyJobService` (từ file riêng) gộp vào file impl. Reference file đã có pattern này: `KhoChiTietService.cs`, `CauHinhService.cs`, `PhieuNhapService.cs` — tất cả interface `Ixxx` đều khai báo cùng file với class impl.

**How to apply:** 
- Tạo service mới: 1 file `XxxService.cs` chứa cả `public partial interface IXxxService` + `public partial class XxxService : IXxxService`.
- Nếu Strategy pattern nhiều impl (vd `IGiaVonRecalculator` → `AvgEligibleImport...` + V2 `HistoricalReplay...`): để interface trong file impl V1 mặc định, impl khác là file riêng cùng namespace.
- Không tạo file `IXxxService.cs` riêng — sẽ bị refactor lại.
