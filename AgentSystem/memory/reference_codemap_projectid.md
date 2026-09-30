---
name: CodeMap Project IDs
description: Trạng thái scan CodeMap cho tất cả project (✅ scanned / ❌ chưa scan) + rule scan local vs remote
type: reference
---

| Project | Status | ID | Last scan | Ghi chú |
|---------|--------|-----|-----------|---------|
| scrm (BE) | ✅ | `ac6f2d6d-67ef-49f0-82c7-e273cf2a4bd6` | 2026-04-08 | code sống trên server, scan qua backend remote được |
| scrm_frontend | ❌ | — | — | gợi ý scan khi cần query FE, phải scan qua local |
| ecommerce_connect | ✅ | `f175182c-594d-4bc5-807c-aeb0921c5405` | 2026-04-09 | code trên D:\ local, scan qua local backend |
| delivery_connect | ✅ | `422f258e-e4b2-4965-b2f1-09419bceefa5` | 2026-04-09 | code trên D:\ local, scan qua local backend |
| ps_delivery | ❌ | — | — | PsWebhook + DeliveryReceive, scan qua local |
| delivery_receive | ❌ | — | — | scan qua local |
| delivery_status | ❌ | — | — | scan qua local |

> **Note**: Remote + local backend hiện chung DB (query `bash codemap.sh projects` ở cả 2 trả kết quả giống nhau). Phân biệt backend chỉ cần khi **scan** — xem rule dưới.

## Scan ở đâu: local hay remote?

**Nguyên tắc**: Backend scan phải đọc được path source code → xác định scan location bằng vị trí code:

- **Code trên server remote** (vd scrm, đã có clone trên 10.0.161.21) → scan qua remote backend `http://10.0.161.21:50888`
- **Code trên máy local D:\** (mọi project còn lại vì chỉ có clone local) → scan qua local backend `http://localhost:5137` (phải start backend trước nếu chưa chạy)

Trước khi chạy `bash codemap.sh scan <path>` hoặc `update <path>`:
1. Hỏi hoặc tự xác định: path source đang ở đâu?
2. Set `CODEMAP_API` đúng backend:
   - Remote: mặc định (không cần set)
   - Local: `CODEMAP_API=http://localhost:5137 bash codemap.sh scan <path>`
3. Nếu nhầm — scan path local D:\ qua remote backend → backend remote không đọc được, scan fail hoặc rỗng.

API base remote: `http://10.0.161.21:50888/api/codemap/{projectId}/`
API base local:  `http://localhost:5137/api/codemap/{projectId}/`

SCRM chạy trên remote server. ecommerce_connect + delivery_connect scan trên **local backend** (`http://localhost:5137` — chính là AgentService trong repo này) vì code ở ổ `D:\` local, server remote không đọc được.

**Lưu ý khi scan path Windows:** phải dùng forward slash `D:/...` thay vì `D:\...` (tránh lỗi JSON escape).

**How to apply:**
- Query SCRM: `bash codemap.sh search <keyword>` (dùng remote mặc định).
- Query ecommerce_connect / delivery_connect: `CODEMAP_API=http://localhost:5137 CODEMAP_PROJECT_ID=<id> bash codemap.sh search <keyword>` — local backend phải đang chạy (`dotnet run --project backend/AgentService`).
