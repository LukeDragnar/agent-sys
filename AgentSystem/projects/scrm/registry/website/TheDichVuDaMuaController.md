---
controller: TheDichVuDaMuaController
module: website
service: Website
man_hinh_ui: "Storefront công khai (theme) — trang chi tiết Thẻ dịch vụ đã mua của khách (`TheDichVuDaMua/{id}`); render `~/Views/Themes/{ThemeCode}/Home/TheDichVuDaMuaDetail.cshtml`"
muc_dich: "Render chi tiết 1 thẻ dịch vụ khách hàng đã mua — lấy theo token khách (không cần IdDonVi), dùng cho khu vực tài khoản khách trên storefront."
alias: ["thẻ dịch vụ đã mua website", "the dich vu da mua storefront", "chi tiết thẻ đã mua khách hàng"]
auth: mixed                # [TokenAuthentication] + lấy token khách qua ITokenService
state_machine: []
deprecated: false
entities: [TheDichVuKhachHang, LinkWebsite]
stored_procs: []
depends: [Website (ICommonApiService, ILinkWebsiteService, ITokenService)]
cross_module_in: []
cross_module_out: []
---

# TheDichVuDaMuaController — Chi tiết thẻ dịch vụ đã mua (storefront)

> Controller MVC `Website.API/Controllers/TheDichVuDaMuaController.cs`, base `Controller`, `[TokenAuthentication]`. Storefront resolve theo domain; lấy chi tiết thẻ đã mua **theo token khách** (`ITokenService.GetToken()`) qua filter `LayTatCaPrivateWithoutIdDonViFilterDto`.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Chi tiết thẻ đã mua | xem trong "thẻ của tôi" | GET `TheDichVuDaMua/{TheDichVuDaMuaId:Guid}` | — (token khách) | mixed | khách đã đăng nhập | `GetDetaiTheDichVuKhachHanglByIdPrivate` với `IdObject=id` + `Token` khách; `NotFound` nếu không thấy; gán `model.TheDichVuDaMua`; render `Home/TheDichVuDaMuaDetail.cshtml` | EF qua CommonApiService | block `PrivateGetTheDichVuDaMua` & payload list đã comment (chỉ lấy chi tiết) |

## Quan hệ
- **Gọi sang:** `ICommonApiService.GetDetaiTheDichVuKhachHanglByIdPrivate`, `ITokenService.GetToken`, `ILinkWebsiteService.GetDataByDomain`.
- **Dữ liệu (entities):** thẻ dịch vụ khách hàng (TheDichVuKhachHang), `LinkWebsite`.
- **Ripple / đồng bộ:** không.

## Lỗi đã biết / lưu ý kỹ thuật
- Method service tên `GetDetaiTheDichVuKhachHanglByIdPrivate` — sai chính tả (`Detai`/`Hangl`); BE+FE phải khớp, không tự sửa lẻ.
- Phần list thẻ đã mua liên quan (`PrivateGetTheDichVuDaMua`) đã comment — chỉ còn lấy chi tiết đơn lẻ.
- Logger inject `ILogger<HomeController>` (copy-paste pattern).
