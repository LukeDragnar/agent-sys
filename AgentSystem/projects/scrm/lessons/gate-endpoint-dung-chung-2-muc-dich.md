# Lesson: Endpoint dùng chung 2 mục đích → gate theo 1 mục đích làm hỏng mục kia

**Sự cố (2026-07-20, feature "lọc sale phụ trách" màn Ticket hỗ trợ):** `UserService.GetSaleSbTeam` (Core) trả rỗng `[]` cho user SB team → dropdown sale trống dù họ đúng là SandBox team.

## Nguyên nhân (chốt)
Endpoint `GetSaleSbTeam` phục vụ **2 nơi**:
1. **Form gán sale vào đơn vị** (quản trị) — chỉ **admin** (SuperUser/HT-AD/HT-KT/HT-CSKH).
2. **Bộ lọc sale trên màn Ticket** — dùng cho **cả SB team** (không nhất thiết admin).

Gate ban đầu chỉ theo **role admin**:
```csharp
if (!laAdmin) return ...empty;   // ← SB team non-admin rơi vào đây → []
```
Trong khi bộ lọc ticket (WorkFlow) lại gate theo **`laSandbox` = user thuộc đơn vị SB team** (`DonViId == SANDBOX_TEAM_DON_VI_ID`). → **2 chỗ lệch gate**: SB team thấy ô lọc nhưng data rỗng.

**Triệu chứng nhận diện:** UI hiện control lọc nhưng data rỗng, **KHÔNG 401** (auth pass, chỉ business-gate chặn). curl trả `{ data: [] }` cả khi chưa gõ keyword.

## Cách xử lý đúng
Gate phải cover **HỢP** các caller hợp lệ = **admin HOẶC thành viên đơn vị đang phục vụ**:
```csharp
// parse idDonViSB LÊN TRƯỚC gate (để check thành viên)
var laSbTeam = userLogin.DonViId == idDonViSB.Value;
if (!laAdmin && !laSbTeam) return ...empty;   // outsider vẫn bị chặn → an toàn
```

## Quy tắc rút ra
- **Endpoint tái dùng nhiều context** → khi feature A mượn endpoint của feature B, **đối chiếu gate với gate của feature đang tiêu thụ** (ở đây bộ lọc ticket = `laSandbox`), đừng để lệch.
- Đổi gate kiểu **nới quyền** (admin → admin OR SB team) vẫn phải giữ chặn outsider → verify "khách domain khác vào có lọt không".

## Bẫy đi kèm (perf, chưa xử)
Filter ticket dùng **`OR` trong LINQ Where** (`SalePhuTrachUserId == sale || IdDonVi IN ...`) → **vi phạm** `../convention/no-or-in-linq.md` (SQL Server không tận dụng index với OR). Đã đánh dấu `// TODO [PERF-SALE-DONVI]`, chủ ý defer ("nặng tính sau"). Khi tối ưu: tách 2 query UNION hoặc pre-resolve danh sách id.

Liên quan: `../registry/he-thong/` (UserController/DonViController), `../convention/no-or-in-linq.md`, `rules/git.md` (mang hotfix product→feature).
