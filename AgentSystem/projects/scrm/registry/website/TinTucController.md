---
controller: TinTucController
module: website
service: Website
man_hinh_ui: "Quản trị website — màn Quản lý Tin tức: list + Thêm/Sửa/Xóa bài tin, lấy tin theo nhóm/đơn vị; tin hiển thị trên storefront"
muc_dich: "CRUD + truy vấn tin tức website (admin): tìm theo điều kiện, theo nhóm, theo đơn vị, theo id; thêm/sửa/xóa; cộng endpoint Private cấp tin cho storefront (NewsController/TinTucChiTiet)."
alias: ["quản lý tin tức website", "tin tức admin", "bài viết website", "news admin website", "lấy tin theo nhóm"]
auth: mixed                # action UI [SBAuthorize("key", roles)]; 2 endpoint [PrivateSBAuthorize] cho storefront service
state_machine: []
deprecated: false
entities: [TinTuc, DMNhomTinTuc]
stored_procs: []
depends: [Website (ITinTucService)]
cross_module_in: [Website storefront (NewsController/TinTucChiTietController gọi LayTatCa_Private/LayTatCaTheoNhom_Private)]
cross_module_out: []
---

# TinTucController — Quản lý tin tức website (admin + Private storefront)

> Controller `Website.API/Controllers/Api/TinTucController.cs`, route `api/TinTuc`, base `BaseCoreController`, `[ApiController]`, `partial`. `[TienIch("TinTucController", "Quản lý tin tức", TI_Website)]`. Action UI scope `DV-AD-TONG` (write) / nhiều role DV (read). Có 2 endpoint `[PrivateSBAuthorize]` service-to-service cấp tin cho storefront.

## Hành động (UI / công khai)
| # | Hành động | Nhãn UI/nút | Endpoint (HTTP path) | Quyền (key) | Auth | Điều kiện hiện/state | Hành vi đúng (expected) | Nguồn (EF/SP) | Known-issue |
|---|---|---|---|---|---|---|---|---|---|
| 1 | Tìm theo điều kiện (grid) | nút Tìm kiếm | POST `api/TinTuc/TimTheoDieuKien` | `TinTuc_TimTheoDieuKien` | mixed | màn list | Trả list tin theo filter (roles trừ DV-NV) | EF | DV-NV bị loại theo yêu cầu |
| 2 | Lấy theo nhóm tin tức | filter nhóm | POST `api/TinTuc/LayTheoNhomTinTuc` | `TinTuc_LayTheoNhomTinTuc` | mixed | màn list | Trả tin theo list id nhóm | EF | - |
| 3 | Thêm mới tin | nút Thêm | POST `api/TinTuc/ThemMoi` | `TinTuc_ThemMoi` | mixed | popup Thêm | `ThemMoi(body, user)` | EF | scope DV-AD-TONG |
| 4 | Tìm theo id | mở popup Sửa | GET `api/TinTuc/TimTheoId/{id:Guid}` | `TinTuc_TimTheoId` | mixed | per-row | `TimTheoId(id, user)` | EF | - |
| 5 | Cập nhật tin | nút Sửa | POST `api/TinTuc/CapNhatThongTin` | `TinTuc_CapNhatThongTin` | mixed | popup Sửa | `CapNhatThongTin(model, user)` | EF | scope DV-AD-TONG |
| 6 | Xóa tin | nút Xóa | POST `api/TinTuc/Xoa/{id:Guid}` | `TinTuc_Xoa` | mixed | per-row | `XoaTheoId(id, user)` | EF | scope DV-AD-TONG |
| 7 | Tìm tin theo id (biến thể) | xem chi tiết | GET `api/TinTuc/TimTinTucTheoId/{id:Guid}` | `TinTuc_TimTinTucTheoId` | mixed | per-row | `TimTinTucTheoId(id, user)` | EF | trùng mục đích #4, DTO khác |
| 8 | Lấy list theo ids | bulk | POST `api/TinTuc/GetListTinTucByIds` | `TinTuc_GetListTinTucByIds` | mixed | — | `GetListTinTucByIds(ids, user)` | EF | - |
| 9 | Lấy DS theo đơn vị | dropdown/list | GET `api/TinTuc/GetDsTinTucTheoDonVi` | `TinTuc_GetDsTinTucTheoDonVi` | mixed | — | `GetDsTinTucTheoDonVi(user)` | EF | - |

## Endpoint nội bộ (private / service-to-service — `[PrivateSBAuthorize]`)
- [internal] Website storefront gọi `POST PrivateGetTinTucByIds` (`PrivateGetItemFilterDto`) — lấy tin theo ids cho render.
- [internal] Website storefront gọi `POST LayTatCa_Private` (`LayTatCaPrivateFilterDto`) — `NewsController` (chi tiết/danh sách/tìm) dùng.
  (Lưu ý: `TinTucChiTietController` dùng `LayTatCaTheoNhom_Private` — endpoint này có thể ở file partial khác của TinTucController, cần xác nhận.)

## Quan hệ
- **Gọi sang:** `ITinTucService` (TimTheoDieuKienAsync, LayTheoNhomTinTuc, ThemMoi, TimTheoId, CapNhatThongTin, XoaTheoId, TimTinTucTheoId, GetListTinTucByIds, GetDsTinTucTheoDonVi, PrivateGetTinTucByIds, LayTatCa_Private).
- **Dữ liệu (entities):** `TinTuc`, `DMNhomTinTuc` (nhóm tin).
- **Ripple / đồng bộ:** tin tức admin tạo ở đây → storefront (`NewsController`, `TinTucChiTietController`) render qua endpoint Private.

## Lỗi đã biết / lưu ý kỹ thuật
- Một số endpoint bị comment (`TimTheoIdBasic`, `GetTinTucByNhomSP`) — không còn dùng.
- `partial class` — có thể còn endpoint Private khác ở file partial (vd `LayTatCaTheoNhom_Private` mà TinTucChiTiet gọi). Cần xác nhận file partial bổ sung.
- `TimTheoId` vs `TimTinTucTheoId` mục đích gần trùng, DTO trả khác — đừng nhầm khi sửa.
