---
name: feedback-camelcase-double-leading-cap
description: Field .NET có 2+ chữ HOA liền đầu (SBTeamPhanTich, URLValue) → JSON camelCase hạ CẢ dãy hoa đầu → FE key là sbTeamPhanTich (không phải sBTeamPhanTich)
metadata:
  type: feedback
---

Khi BE (.NET) trả field có **2+ chữ HOA liền ở ĐẦU** tên property, camelCase (cả System.Text.Json lẫn Newtonsoft) **hạ nguyên dãy hoa đầu** cho tới chữ hoa cuối-trước-thường — KHÔNG phải chỉ hạ 1 chữ đầu.

Ví dụ:
- `SBTeamPhanTich` → `sbTeamPhanTich` (KHÔNG phải `sBTeamPhanTich`)
- `URLValue` → `urlValue`
- `IOStream` → `ioStream`
- `ChoSBPhanHoi` → `choSBPhanHoi` (SB Ở GIỮA → giữ nguyên; chỉ dãy hoa ĐẦU mới bị hạ)

**Why:** Đoán nhầm key khi đọc JSON ở FE (`item.sBTeamPhanTich`) → luôn `undefined` → UI ra `--` dù BE đã lưu đúng (deserialize input thường case-insensitive nên SAVE vẫn chạy, chỉ READ hỏng → dễ tưởng "chưa lưu"). Tốn 1 vòng debug.

**How to apply:**
- Đặt tên property BE tránh 2 chữ hoa liền ở đầu nếu được (vd `SbTeamPhanTich` thay vì `SBTeamPhanTich`) → camelCase ra `sbTeamPhanTich` trực quan hơn.
- Nếu đã lỡ có: FE phải dùng đúng key camelCase-dãy-hoa (`sbTeamPhanTich`), kiểm tra bằng cách xem field tương tự đã chạy, hoặc log response thật — ĐỪNG đoán "chỉ hạ chữ đầu".
- Field HOA ở GIỮA (`ChoSBPhanHoi`→`choSBPhanHoi`) không bị ảnh hưởng — chỉ dãy hoa ĐẦU.

Liên quan: [[feedback-bao-su-that-doc-code-minh-truoc]].
