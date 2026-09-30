---
name: feedback_scrm_canonical_partner_field_pair
description: Mỗi trạng thái/phân loại lấy từ sàn lưu trong CRM phải đi CẶP 2 field — CRM canonical + đối tác raw (suffix DoiTac), giống pattern logistics
metadata:
  type: feedback
---

Khi CRM (SCRM) lưu 1 trạng thái/phân loại có nguồn từ sàn (Shopee/TikTok/Lazada), LUÔN tạo **2 field đồng hành**:
1. **CRM canonical** — giá trị chuẩn dùng chung mọi sàn (ECOMM map về). VD: int enum.
2. **Đối tác raw** — giá trị gốc partner gửi, suffix `DoiTac`, để view phụ + audit + phòng partner có value chưa map.

**Why:** Giống pattern logistics đã có: `LogisticsTrangThaiMa` (CRM) + `LogisticsTrangThaiMaDoiTac` (raw); return status: `TrangThai` (canonical) + `TrangThaiHoanMaDoiTac`/`TenDoiTac` (raw). User: "luôn nhớ phải có 2 loại đồng hành giống logistics nhé". Mất raw → không trace được/không xử lý được value sàn mới.

**How to apply:** Áp dụng cho mọi field mới kiểu này. VD đơn hoàn:
- `ThoiDiemYeuCauHoan` (canonical int) + `ThoiDiemYeuCauHoanDoiTac` (raw, vd Shopee request_type)
- `NoiKiemHoan` (canonical int) + `NoiKiemHoanDoiTac` (raw, vd "seller_validation"/"warehouse_validation")
ECOMM map raw→canonical TRƯỚC khi gửi (boundary [[feedback_ecomm_vs_crm_logic_boundary]]), gửi sang SCRM CẢ raw lẫn canonical. SCRM chỉ lưu. Liên quan [[feedback_confirm_each_change_before_apply]].
