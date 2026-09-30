# Recipe — Migration sang Token tĩnh KHÔNG hết hạn (SuperShip superkey)

> Áp dụng khi đối tác cấp **token tĩnh không hết hạn** thay cho Access-Token có hạn (đỡ phải thay token định kỳ → hết 401 hết hạn). SuperShip: `POST /v1/partner/auth/superkey`. Tái dùng được cho partner khác có cơ chế tương tự.

## Bối cảnh
- Access-Token (JWT) **hết hạn** định kỳ → đăng đơn/tính phí 401 liên tục, phải thay tay.
- Đối tác cung cấp **token tĩnh** (vd 40 ký tự, không hết hạn): gọi 1 lần đổi từ Access-Token → lưu → dùng mãi (gửi qua header `Token:` thay vì `Authorization: Bearer`).

## Cách làm (đã áp cho SuperShip)
1. **Exchange khi KẾT NỐI** (không phải lúc Lưu): trong `ConnectAccountPartner`, gọi superkey bằng Access-Token → lấy token tĩnh.
   - **Best-effort**: bọc `try/catch`, lấy không được **không chặn** kết nối (fallback Access-Token cũ).
   - **Persist nhờ flow ngoài**: chỉ set field + `MarkDirty`, KHÔNG tự `Update` — `CrmUnitConnectService.ConnectShippingUnit` tự `Insert`(Id null)/`Update` sau khi test pass. (Giống NjvService.)
2. **Lưu vào Prop field** `PropName2="SuperToken"` / `PropValue2=<token>` → **không cần migration DB**. (NJV đã lưu refresh-token ở `PropValue2` — cùng cơ chế, có tiền lệ prod.)
3. **Mọi API ưu tiên token tĩnh**: helper `BuildAuthHeader` → có token tĩnh thì gửi `{ "Token": <token> }`, không thì `{ "Authorization": "Bearer "+AccessToken }` (zero-regression cho khách chưa migrate).
4. **Chỉ GEN khi CHƯA có token tĩnh** (`GetStoredSuperToken` rỗng):
   - Đã có token tĩnh + Access-Token KHÔNG đổi (kể cả AT đã hết hạn) → **KHÔNG gọi lại superkey** → không phụ thuộc AT, không lỡ làm mất token tĩnh.
   - Access-Token ĐỔI → `MergeAccountPartner` clear token tĩnh trước → vào Connect thấy rỗng → gen mới cho account mới.
5. **Clear token tĩnh khi đổi Access-Token** (trong `MergeAccountPartner`): `target.PartnerToken` (cũ) CÓ giá trị và khác token mới → set null `PropName2/PropValue2` (kẻo đăng đơn nhầm account). Cũ rỗng (lần đầu) → không clear.

## Invariant SỐNG CÒN (đừng refactor làm vỡ)
- **Thứ tự `Merge` → `Connect`**: trong `ConnectShippingUnit`, `MergeUnitConnect` (clear token tĩnh nếu AT đổi) phải chạy **TRƯỚC** `ConnectAccountPartner` (gen mới). Đảo thứ tự → khi đổi account sẽ dùng token tĩnh cũ → **đăng nhầm account**.
- **Lưu chỉ giữ/xóa, KHÔNG gen**: nút "Lưu" (`SaveUnitConnect`) chỉ lưu Access-Token + clear token tĩnh nếu AT đổi; gen token tĩnh CHỈ ở nút "Kết nối".

## Lưu ý
- `UnitConnect.PropName*/PropValue*` là **per-partner-per-row**: mỗi partner dùng cho mục đích riêng (VTP=voucher, GHN=pick_shift, NJV=RefreshToken...). `MergeAccountPartner` mỗi partner chỉ đụng field của mình → không đụng nhau.
- `CrmUnitConnectService.MapUnitConnect` (ghi đè PropValue* từ request) là **dead code** (không caller) → không lo nó wipe token tĩnh. Flow thật `Get()` entity từ DB nên Prop field được preserve.
- Token tĩnh bị đối tác **revoke** (hiếm) → reconnect không tự lấy lại (vì thấy còn) → khách phải đổi/nhập lại AT để re-gen.
