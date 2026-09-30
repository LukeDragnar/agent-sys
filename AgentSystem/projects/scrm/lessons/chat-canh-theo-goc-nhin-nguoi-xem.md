# Lesson — Canh hiển thị theo GÓC NHÌN người xem, không theo loại người gửi

> **Sự cố (2026-07):** Chat trong ticket canh bong bóng **cứng theo loại người gửi** (`nguoiGuiLoai === 'SB'` → phải, còn lại → trái). SB team xem thì đúng, nhưng **đơn vị khách xem lại thấy tin của CHÍNH MÌNH nằm bên trái** — sai hoàn toàn. QA lần đầu (review + trace) **không bắt được** vì chỉ mô phỏng từ 1 vai (SB).

## Triệu chứng
Đứng vai đơn vị thường (khách), mở tab "Trao đổi": tin mình gửi hiện **bên trái** (lẽ ra phải bên phải như mọi app chat).

## Nguyên nhân gốc
Logic canh trái/phải bám vào **thuộc tính của BẢN GHI** (người gửi là SB hay KHACH) thay vì **quan hệ giữa bản ghi và NGƯỜI ĐANG XEM**. Với chat 2 phe, "của tôi" = tin do phe của người-đang-đăng-nhập gửi — phải suy từ cờ vai trò của viewer.

```typescript
// SAI — cứng theo loại người gửi
chatLoaiClass(loai: string) {
  return loai?.toUpperCase() === 'SB' ? 'is-sb' : 'is-khach';  // SB luôn phải
}

// ĐÚNG — theo góc nhìn viewer (isSandboxTeam = người đang xem có phải SB không)
chatLoaiClass(loai: string) {
  const laTinSB = (loai || '').trim().toUpperCase() === 'SB';
  const laCuaToi = this.isSandboxTeam ? laTinSB : !laTinSB;  // SB xem → tin SB là của mình; khách xem → tin KHACH là của mình
  return laCuaToi ? 'is-me' : 'is-them';
}
```
File: `pages/ticket-ho-tro/ticket-detail-drawer/ticket-detail-drawer.component.ts` (`chatLoaiClass`) + `.scss` (`.is-me`/`.is-them`).

## Vì sao QA sót
Trace chỉ chạy data **từ vai SB** → bong bóng rơi đúng → tưởng pass. Feature 2 vai mà **chỉ test 1 vai** = mù nửa còn lại.

## Rule rút ra (đã đưa vào checklist)
- **UI phụ thuộc "ai đang xem"** (canh chat, nút "chỉ tôi sửa", nhãn bạn/tôi, màu, badge, quyền hiện) → **KHÔNG hard-code theo loại người gửi/tạo**. Suy theo cờ vai trò của **viewer**.
- **Trace BẮT BUỘC chạy từ TỪNG vai** (người gửi ↔ người xem, SB ↔ khách, admin ↔ user): cùng 1 data, đổi viewer thì vị trí/quyền/nhãn phải đổi đúng.
- Đã ghi vào: `agents/trace-agent.md` (checklist case edge "Nhiều vai / góc nhìn") + `projects/scrm/qa-spec.md` (mục "UI theo góc nhìn / vai trò").

## Ghi chú thiết kế (không phải bug)
Model canh theo **PHE** (SB/khách), không theo từng userId → 2 người cùng đơn vị khách thấy tin của nhau bên phải. Chấp nhận được vì đây là chat 2 phe (SB ↔ đơn vị), tên người gửi vẫn hiện ở đầu bong bóng để phân biệt. Muốn "mỗi cá nhân 1 phía" phải đổi spec + BE trả thêm `NguoiTaoUserId`.
