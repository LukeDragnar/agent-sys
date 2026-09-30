# Pushsale — Code Patterns & Conventions

> Convention thực tế rút từ codebase (VB.NET / DNN WebForms). Tuân theo style hiện có, **đừng** áp pattern .NET Core / EF / DI vào phần legacy.

## 0. Namespace & import

- Root namespace: **`BASE`**. DAL: `BASE.DAL.{Domain}` (vd `BASE.DAL.APP`, `BASE.DAL.HT`, `BASE.DAL.LD`). Helper: `BASE.HELPER`. Model: `BASE.Models`.
- Code-behind UI: `Namespace BASE` + `Partial Public Class {TenControl}` `Inherits PortalModuleBase`.
- Import hay gặp: `Imports DotNetNuke.Entities.Modules`, `Imports BASE.HELPER`, `Imports BASE.DAL.{Domain}`, `Imports Microsoft.ApplicationBlocks.Data`.

## 1. Control UI = `.ascx` + code-behind `.ascx.vb`

Mọi màn hình là một DNN User Control kế thừa `PortalModuleBase`. Có sẵn (từ DNN): `ModuleId`, `TabId`, `PortalId`, `UserId`, `UserInfo`, `ModuleConfiguration`, `Settings`.

```vb
Namespace BASE
    Partial Public Class DanhSachContact
        Inherits PortalModuleBase

        Protected Sub Page_Load(sender As Object, e As EventArgs) Handles Me.Load
            Try
                ' check quyền trước
                If Not (UserInfo.IsInRole("Sale") OrElse UserInfo.IsSuperUser) Then
                    Response.Redirect("/", True) : Return
                End If
                If Not Page.IsPostBack Then
                    InitData()   ' đổ dropdown/filter, set default
                    ShowData()   ' query + bind
                End If
            Catch ex As Exception
                WebHelper.ShowExceptionForDeveloper(_Message, ex)
            End Try
        End Sub
    End Class
End Namespace
```

**Quy ước method:** `InitData()` (khởi tạo filter/dropdown, thường theo `id_don_vi`), `ShowData()` (query + bind), event handler `btnX_Click`. Logic nặng đặt trong các Sub/Function `Private`/`Public` của class, gọi từ event.

## 2. Truy cập dữ liệu — Stored Procedure qua DAL

**KHÔNG có ORM, KHÔNG viết SQL inline.** Mọi truy vấn = gọi **Stored Procedure** qua `SqlHelper` (Microsoft.ApplicationBlocks.Data). Mỗi domain DAL expose wrapper dùng sẵn `ConnectionString` của domain đó:

```vb
' trong BASE.DAL.APP.APPDAL — ví dụ wrapper chuẩn
Public Shared Function ExecuteDataset(procname As String, ParamArray params() As Object) As DataSet
    Return SqlHelper.ExecuteDataset(ConnectionString, procname, params)
End Function
```

Cách gọi từ code-behind hoặc DAL khác:

```vb
Dim dt = SqlHelper.ExecuteDataset(LDDAL.ConnectionString, "LD_Contact_BangTongHopBanHang",
                                  id_don_vi, tu_ngay, den_ngay).Tables(0)
rptData.DataSource = dt   ' bind thẳng DataTable
rptData.DataBind()
```

- 4 entry: `ExecuteDataset` (DataSet), `ExecuteReader` (SqlDataReader), `ExecuteNonQuery` (số dòng), `ExecuteScalar` (1 giá trị).
- **Params truyền theo đúng thứ tự** SP định nghĩa (positional `ParamArray`), không phải named.
- Tên SP: `{Module}_{Đối tượng}_{Hành động}` — vd `LD_Contact_BangTongHopBanHang`, `APP_UserProfile_Insert`, `HT_Logs_Log`.
- **SP không nằm trong repo.** Khi task cần đổi logic SP → báo rõ cần sửa ở DB, không có file để sửa trong source. File `.sql` ở SVN root chỉ là script vá data.

### Model ↔ SP qua ObjectHelper
Insert/Update entity dùng convention map property → tham số SP:
```vb
APPDAL.Object_Execute_Insert(objInfo, "APP_UserProfile_Insert")   ' ObjectHelper map field -> @param
APPDAL.Object_Execute_Update(objInfo, "APP_UserProfile_Update")
```
Hoặc class CRUD tự build `List(Of SqlParameter)` rồi gọi SP (xem `lib/base.dal/APP/APP_UserProfile/APP_UserProfileCRUD.vb`).

### Cấu trúc 1 domain DAL
```
lib/base.dal/{Domain}/
  {Domain}DAL.vb          ' entry: ConnectionString + Execute* wrappers + static methods nghiệp vụ
  {Domain}Config.vb       ' đọc connection string từ config
  {Domain}Constants.vb    ' hằng số (tên SP, key cache, ...)
  {Entity}/
    {Entity}Info.vb       ' model (properties)
    {Entity}CRUD.vb       ' Get/GetAll/Insert/Update/Delete + cache
```

## 3. Multi-tenant — luôn theo `id_don_vi`

- Mọi truy vấn nghiệp vụ phải lọc theo **`id_don_vi`** (ID đơn vị/tenant). Lấy từ `APP_UserProfileInfo` của user hiện tại (`up.IdDonVi`), **không** tin client.
- Cache key **bắt buộc** chứa `id_don_vi` để tránh rò rỉ dữ liệu giữa tenant:
```vb
Dim cache_key = CacheHelper.GetCacheKey(CACHE_KEY_..., id_don_vi)
```
- ⚠️ Bug nguy hiểm nhất ở dự án này = quên `id_don_vi` trong query/cache → tenant A thấy data tenant B.

## 4. Cache pattern (double-checked locking)

```vb
Public Shared Function APP_UserProfile_GetAll(id_don_vi As Integer, Optional use_cache As Boolean = True) As List(Of APP_UserProfileInfo)
    Dim cache_key = CacheHelper.GetCacheKey(CACHE_KEY_APP_UserProfile_GetAll, id_don_vi)
    Dim tmp = CacheHelper.GetCache(cache_key)
    If tmp Is Nothing OrElse Not use_cache Then
        SyncLock cache_key
            tmp = CacheHelper.GetCache(cache_key)
            If tmp Is Nothing Then
                tmp = ' ... load từ DB
                CacheHelper.SetCache(cache_key, tmp)
            End If
        End SyncLock
    End If
    Return tmp
End Function
```
Khi sửa data phải **invalidate cache** tương ứng (`CacheHelper.Remove...`) nếu không UI hiển thị cũ.

## 5. Phân quyền — `UserInfo.IsInRole`

Không dùng declarative permission của DNN cho nghiệp vụ; check thủ công trong `Page_Load`:
```vb
Dim is_admin = UserInfo.IsInRole("Sale") OrElse UserInfo.IsInRole("Marketing") _
            OrElse UserInfo.IsInRole("Admin") OrElse UserInfo.IsInRole("CEO") OrElse UserInfo.IsSuperUser
If Not is_admin Then Response.Redirect("/", True) : Return
```
Role chính: `Sale`, `Marketing`, `Admin`, `CEO`, `UnitAdmin`, `Accounting`, `Warehouse`, `ChiaSo`, `TaoDonVi`. Lấy role theo tên qua `CommonHelper.GetRole_ByNames(...)`.

## 6. Giữ trạng thái filter — `Session("S_...")`

Filter (dropdown đơn vị, sale, khoảng ngày...) lưu vào Session với prefix `S_`:
```vb
Public Sub SaveSession()
    Session("S_ddlDonVi") = ddlDonVi.SelectedValue
    Session("S_ddlSales") = ddlSales.SelectedValue
End Sub
```
Đọc lại trong `InitData()` để restore lựa chọn cũ. (Vì WebForms postback toàn trang.)

## 7. Xử lý lỗi & log

- Bọc `Page_Load`/handler bằng `Try ... Catch ex As Exception` → `WebHelper.ShowExceptionForDeveloper(_Message, ex)` (hiện lỗi cho dev, ẩn với user thường).
- Log nghiệp vụ/hệ thống: `HTDAL.HT_Logs_Log(category, type, message, page, detail)`.
- Log hoạt động user: `APPDAL.APP_UserActivities_Log(action, listPD, userId, username, displayName, idDonVi, ...)`.
- **Performance:** đo `start = DateTime.Now`; query > 3000ms thì `HT_Logs_Log("TIME: ...", "TRACKING", ...)`. `WebHelper.TimeTracking(controlName, start, username, False)` ở `PreRender`.

## 8. Naming

| Loại | Quy ước | Ví dụ |
|---|---|---|
| Class control | PascalCase, tiếng Việt không dấu | `BangTongHopBanHang`, `CapNhatContact2` |
| Field private/ViewState backing | `_PascalCase` (HiddenField) | `_IsAdmin`, `_TuNgay` |
| Biến local | snake_case hoặc camelCase (lẫn lộn) | `id_don_vi`, `is_admin`, `cache_key` |
| DAL | `{Domain}DAL` | `APPDAL`, `LDDAL`, `HTDAL`, `RPDAL` |
| Model / CRUD | `{Entity}Info` / `{Entity}CRUD` | `APP_UserProfileInfo` |
| Stored proc | `{Module}_{Entity}_{Action}` | `LD_Contact_BangTongHopBanHang` |
| Session filter | `S_{control}` | `S_ddlDonVi` |
| Cache key | hằng số `CACHE_KEY_...` + `id_don_vi` | |

> Tiếng Việt không dấu xuyên suốt (class, biến, comment). Giữ nguyên phong cách này khi thêm code.

## 9. Microservice mới (`Services/`) — KHÁC hẳn

Phần `Services/` là C# .NET Core: dùng DI, EF Core / Dapper, gRPC, MongoDB, JWT, middleware (`Pushsale.CommonV3`). **Đừng** trộn pattern này vào web legacy và ngược lại. Khi task thuộc `Services/` mới áp convention .NET Core hiện đại.

## 10. Những điều TUYỆT ĐỐI tránh

1. ❌ Viết SQL inline / nối chuỗi SQL trong code-behind → SQL injection + lệch convention. Dùng SP + params.
2. ❌ Quên `id_don_vi` trong query/cache → rò rỉ dữ liệu cross-tenant.
3. ❌ Bind/cache data mà không invalidate khi update.
4. ❌ Để lộ exception/SQL cho user thường (dùng `ShowExceptionForDeveloper`).
5. ❌ Copy connection string / mật khẩu từ web.config ra ngoài.
6. ❌ Đổi logic kỳ vọng nằm trong SP mà tưởng sửa code VB là xong (SP ở DB, không ở repo).
