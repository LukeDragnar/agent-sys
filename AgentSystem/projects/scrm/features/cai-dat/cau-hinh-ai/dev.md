# Dev — Cấu hình AI

> Hai surface tách service: **Token AI** (service AIChat, FE `QuanLyAIModule`) + **Prompt AI** (service SandBoxAI, FE `SandBoxAIModule`). INDEX gộp thành 1 feature "Cấu hình AI — token + prompt". Chỉ dành cho dev.

## Màn hình & route (FE)
| Màn hình | Route | Component |
|---|---|---|
| Danh sách Token AI | `/quan-ly-ai/danh-sach` | `DanhSachTokenAIComponent` (sửa: `tokenai-add-edit/`) |
| Agent Profile (AI chat) | `/quan-ly-ai/agent-profile` | `AgentProfileComponent` |
| Quản lý Prompt (SandBox) | `/sandboxai/ai-prompt` | `AIDonViPromptListComponent` (sửa: `aidonviprompt-edit/`) |
| Quản lý URL map (SandBox) | `/sandboxai/ai-urlmap` | `AIUrlMapListComponent` |

- Route gốc: `app-routing.module.ts:479` (`quan-ly-ai` → `QuanLyAIModule`), `:419` (`sandboxai` → `SandBoxAIModule`).
- Route con: `quan-ly-ai-routing.module.ts:8` (danh-sach), `sandboxai-routing.module.ts:7` (ai-prompt), `:11` (ai-urlmap).

## Endpoint / Controller (trace registry)

**TokenAIController** (registry: `he-thong/TokenAIController.md`), service AIChat, `[TienIch(..., TI_AI)]`, route `api/TokenAI`:
| Hành động | Endpoint | Quyền (key) |
|---|---|---|
| Tìm danh sách token | POST `.../TimTheoDieuKien` | `TokenAI_TimTheoDieuKien` (HT-AD, DV-AD-TONG, HT-CSKH) |
| Xem chi tiết | GET `.../TimTheoId/{id}` | `TokenAI_TimTheoId` (HT-AD, DV-AD-TONG, HT-CSKH) |
| Thêm mới | POST `.../ThemMoi` | `TokenAI_ThemMoi` (DV-AD-TONG) |
| Cập nhật | POST `.../CapNhatThongTin` | `TokenAI_CapNhatThongTin` (DV-AD-TONG) |
| Xóa | POST `.../Xoa/{id}` | `TokenAI_Xoa` (DV-AD-TONG) |

> TokenAI: permission key **còn active** (không bị comment). CRUD scope theo `CurrentUser.DonViId`.

**AIDonViPromptController** (registry: `he-thong/AIDonViPromptController.md`), service SandBoxAI, route `api/AIDonViPrompt`:
| Hành động | Endpoint | Quyền (key) |
|---|---|---|
| Xem chi tiết | GET `.../TimTheoId/{id?}` | — (chỉ login); khác đơn vị → NotFound |
| Thêm mới | POST `.../ThemMoi` | (gợi ý `AIDonViPrompt_ThemMoi`, key ở comment → chỉ login) |
| Cập nhật | POST `.../CapNhatThongTin` | (gợi ý, chỉ login) |
| Xóa | POST `.../Xoa/{id}` | (gợi ý, chỉ login); SuperUser xóa được đơn vị khác |
| Tìm theo điều kiện | POST `.../TimTheoDieuKien` | — (chỉ login); lọc cứng IdDonVi |
| Khởi tạo mẫu | POST `.../KhoiTaoMau` | — (chỉ login); clone bản mẫu IdDonVi==Guid.Empty |

**AIUrlMapController** (registry: `he-thong/AIUrlMapController.md`), service SandBoxAI, route `api/AIUrlMap` — danh mục dùng chung, **không scope đơn vị** (màn `/sandboxai/ai-urlmap`).

## Entities
- **`TokenAI`** — khóa gọi model AI, scope đơn vị (IdDonVi). Trường FE thấy: `loaiAI`/`loaiAITen`, `token`, `isMacDinh`, `ngayTao`.
- **`AIDonViPrompt : BaseEntity`** — `IdDonVi`, `Ma`, `Ten`, `Prompt`, `MoTa` (+ audit, IsDelete). Mỗi đơn vị giữ bộ prompt riêng. Bản mẫu = bản ghi `IdDonVi == Guid.Empty`.
- **`AIUrlMap : BaseEntity`** — `Ma`, `Ten`, `Url`, `MoTa` — **không có IdDonVi** (danh mục toàn hệ thống).

## Luồng code
- TokenAI: CRUD chuẩn TimTheoDieuKien/TimTheoId/ThemMoi/CapNhatThongTin/Xoa qua `ITokenAIService`, gắn DonViId.
- AIDonViPrompt: `ThemMoi` ép `IdDonVi=CurrentUser.DonViId`; `CapNhat`/`Xoa` GetById → check đơn vị → Update/Delete (soft); `KhoiTaoMau` check đơn vị đã có bản ghi chưa → clone bản mẫu (`IdDonVi==Guid.Empty`), gán Id/DonVi/audit mới, `BulkInsertOrUpdate` trong transaction.
- Lúc AI chạy: consumer/Chat dùng TokenAI của đơn vị gọi model; tính năng SandBox AI đọc AIDonViPrompt/AIUrlMap.

## Bug-prone / lưu ý (path:line) — chỉ GHI, không sửa
- `TokenAIController.cs:61` `Xoa` chỉ theo `id`, **không re-check đơn vị** → về lý thuyết xóa được token của đơn vị khác nếu biết id.
- `AIDonViPromptController.cs:34-42` `TimTheoIdAsync` gọi `GetByIdAsync` **2 lần** (check rồi trả) → query thừa.
- `AIDonViPromptService.cs:58-65` `TimTheoDieuKienAsync` — nhánh `if (Sorts.Any())` và `else` đều `OrderBy(NgayTao)` → tham số sort FE gửi **vô tác dụng**.
- AIDonViPrompt permission key (`AIDonViPrompt_ThemMoi`…) chỉ ở **comment**; runtime chỉ yêu cầu login → mọi user đăng nhập CRUD được trong phạm vi đơn vị mình (chưa siết theo role admin).
- `AIDonViPrompt.Xoa` — nếu **SuperUser** thì xóa được bản ghi đơn vị khác (bỏ qua check đơn vị).
- `KhoiTaoMau` idempotent theo "đã có bất kỳ bản ghi nào" (không theo từng mã) → đơn vị mới chỉ cần có 1 prompt là coi như đã khởi tạo, không bổ sung mã còn thiếu.
- `AIUrlMapController.cs:32-36` `TimTheoIdAsync` **thiếu `[SBAuthorize]`** → đọc chi tiết URL map không cần đăng nhập (public hoàn toàn). Toàn controller không scope IdDonVi → mọi đơn vị thấy/sửa chung 1 danh mục.

## File source (path:line)
- BE TokenAI: `D:\PROJECT\Git\_SourceForCode\Group2\scrm\AIChat\AIChat.API\Controllers\TokenAIController.cs`.
- BE AIDonViPrompt: `D:\PROJECT\Git\_SourceForCode\Group2\scrm\SandBoxAI\SandBoxAI.API\Controllers\AIDonViPromptController.cs`.
- FE Token AI: `D:\PROJECT\Git\_SourceForCode\Group2\scrm_frontend\src\app\pages\quan-ly-ai\danhsach-tokenai\danhsach-tokenai.component.html` / `.ts` (sửa: `tokenai-add-edit/`).
- FE Prompt: `...\pages\sandboxai\aidonviprompt\aidonviprompt-list.component.html` / `.ts` (sửa: `aidonviprompt-edit/`).
- FE URL map: `...\pages\sandboxai\aiurlmap\aiurlmap-list.component.html`.
- FE routing: `...\pages\quan-ly-ai\quan-ly-ai-routing.module.ts`, `...\pages\sandboxai\sandboxai-routing.module.ts`.
- Registry node: `projects/scrm/registry/he-thong/TokenAIController.md`, `.../AIDonViPromptController.md`, `.../AIUrlMapController.md`, `.../PageAIController.md`.
