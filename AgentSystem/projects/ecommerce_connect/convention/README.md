# ecommerce_connect — Convention (quy tắc viết code riêng mảng, chi tiết)

> **Convention** = quy tắc viết code đúng, áp project-wide (proactive, "luôn/không bao giờ"). Khác `lessons/` (bug-sự-cố 1 lần) và recipe.
>
> **2 tầng convention:** `../patterns.md` = core/tổng quan ↔ folder này = **chi tiết per-chủ-đề** (load-on-demand, ví dụ đúng/sai đầy đủ). `../qa-code.md` = checklist review mirror lại.

## Danh sách

- [internal-webhook-no-auth.md](internal-webhook-no-auth.md) — webhook ECOMM↔SCRM cùng cluster: pattern `NhanDuLieuXxx` đơn giản, KHÔNG HMAC/JWT/raw-body/MassTransit-realtime
- [ecomm-vs-crm-boundary.md](ecomm-vs-crm-boundary.md) — ranh giới: generic business logic → CRM (dùng chung mọi nguồn); partner adapter → ECOMM
- [defer-persist-until-external-success.md](defer-persist-until-external-success.md) — luồng thủ công: không Insert local upfront, chỉ persist sau khi call ngoài thành công (tránh row mồ côi `Status=null`)
- [deleteasync-persists-dirty.md](deleteasync-persists-dirty.md) — `MarkDirty → DeleteAsync` = 1 hit (soft-delete + persist dirty); KHÔNG cần UpdateAsync riêng; QA đừng flag false-positive

> Core convention (EntityBase, MassTransit, persist-parent-trước-SP, retry re-lookup FK, filter response, SetInfoBase logging) → `../patterns.md`.
