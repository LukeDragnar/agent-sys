# Unit Test — nguyên tắc & khuôn CHUNG (mọi project .NET)

> Chưa biết UT? Đọc `rules/unit-test-gioi-thieu.md` (khái niệm) TRƯỚC.
> Dùng chung cho mọi agent/project. Chi tiết riêng từng project (repository pattern, DTO, nơi để test) → `projects/<proj>/skills/unit-test.md`.
> Thứ tự đọc: **giới thiệu → file này (nguyên tắc) → skill project (đặc thù)**.

---

## 0. TỔNG KẾT — 6 điều PHẢI nhớ (đọc cái này là nắm cốt lõi)

1. **🔴 LUẬT VÀNG:** kết quả mong đợi (Then) lấy từ **Tiêu chí nghiệm thu** (Cho–Khi–Thì), **KHÔNG chép từ code**. Chép code = đóng dấu cho bug.
2. **Test chạy CODE THẬT của service** — chỉ repository/cache là giả (Moq) để khỏi đụng DB/Redis. Sửa service sai → test đỏ (đó là lý do nó bắt bug).
3. **Cấu trúc AAA:** Given (dựng cảnh + mock) → When (**gọi ĐÚNG 1 hàm service**) → Then (kiểm theo tiêu chí). 1 test = kiểm 1 tình huống → chỉ 1 lời gọi service.
4. **3 loại "sai":** ① test đỏ vì **code sai** → sửa CODE (tốt, bắt bug); ② test đỏ vì **test sai** (expected/mock sai) → sửa TEST; ③ **test XANH mà expected lấy sai nguồn** → 💀 nguy hiểm nhất (giả an toàn). Phân biệt ①/② bằng cách đối chiếu expected ↔ tiêu chí.
5. **SỬA hay THÊM** (khi sửa/thêm code): **đọc test hiện có TRƯỚC** → đổi behavior thì **SỬA** test cũ, thêm method/nhánh mới thì **THÊM** test. KHÔNG mặc định thêm mới. Đổi behavior → cập nhật test cùng commit.
6. **Comment TIẾNG VIỆT:** mỗi test mở đầu bằng tiêu chí **Cho–Khi–Thì** + chú thích Given/When/Then → team đọc hiểu không cần biết Moq.

> Chi tiết từng điều ở các mục dưới. Đặc thù SCRM (IRepository, TableWithNolock…) → `projects/scrm/skills/unit-test.md`.

---

## 1. Unit test là gì (1 phút)
- Kiểm **"code ra quyết định đúng chưa"** — chạy trong RAM, **KHÔNG đụng DB/Redis**.
- Phụ thuộc ra-hệ-thống-ngoài (repository/cache/http) được thay bằng **mock** → nhanh (mili-giây), chạy lại mãi.
- Giá trị: ai sửa vỡ logic → test **đỏ ngay**, biết trước khi lên production.
- ≠ integration test (loại đó mới dùng DB thật, ít hơn, để sau).

## 2. 🔴 LUẬT VÀNG (quan trọng nhất)
> **Kết quả mong đợi (Then/Assert) lấy từ TIÊU CHÍ NGHIỆM THU, KHÔNG chép từ code.**

- Chép kết quả code vào Assert = "code tự xác nhận chính nó" → **đóng dấu hợp lệ cho bug**.
- Đúng: nhìn **Tiêu chí nghiệm thu** (Given/When/Then) → viết expected theo đó.
- Chỗ nào code khác tiêu chí → **báo nghi bug**, đừng sửa test cho khớp code.
- Không có tiêu chí sẵn (feature mới) → **hỏi người biết nghiệp vụ** hoặc dán nhãn `// CHARACTERIZATION: chốt hành vi hiện tại, CHƯA verify đúng`.

## 3. Given / When / Then = Arrange / Act / Assert
- **Given** (cho trước) = **Arrange** — dựng bối cảnh + mock.
- **When** (khi) = **Act** — gọi đúng 1 method cần test.
- **Then** (thì) = **Assert** — so kết quả với tiêu chí.
- 1 Scenario trong tiêu chí nghiệm thu → 1 test.

## 4. Test cái gì / KHÔNG test
- ✅ Logic ra quyết định: validate, if/else, mapping, tính toán, gọi phụ thuộc đúng/sai cách, cờ (soft/hard).
- ❌ Getter/setter, DTO thuần, code khung.
- ⏭️ Method chạy trong transaction DB / câu SQL thật → **integration test** (để sau, không unit).

## 5. Đặt tên: `<Method>_<TìnhHuống>_<KỳVọng>`
- Ví dụ: `Xoa_KhiKhongTimThay_ThiTraVeNotFound`, `Copy_KhiNguonRong_ThiChanKhongXoaDich`.
- Tên = 1 câu tiêu chí → đọc tên là biết test kiểm gì.

## 5b. Comment TIẾNG VIỆT (bắt buộc — để team đọc hiểu)
- **Mỗi test mở đầu bằng khối tiêu chí Cho–Khi–Thì (tiếng Việt):**
  ```csharp
  // ─── Tiêu chí: Xoá khi KHÔNG tìm thấy ───
  //   Cho : không có danh mục nào với id đó
  //   Khi : admin xoá theo id
  //   Thì : báo "không tồn tại" và TUYỆT ĐỐI không xoá gì
  [Fact]
  public async Task Xoa_KhiKhongTimThay_ThiTraVeNotFound() { ... }
  ```
- **Chú thích tiếng Việt cho các dòng Given/When/Then**: dòng mock giải thích "dựng cảnh gì", dòng Assert giải thích "kiểm điều gì theo tiêu chí".
- Mục tiêu: người đọc **không cần biết Moq** vẫn hiểu test đang kiểm nghiệp vụ gì.

## 6. Khuôn xUnit + Moq (generic — mock 1 interface bất kỳ)

```csharp
public class XxxServiceTests
{
    [Fact]
    public async Task Method_KhiTinhHuong_ThiKyVong()
    {
        // Given (Arrange)
        var dep = new Mock<ISomeDependency>();
        dep.Setup(d => d.LayGiDo(It.IsAny<Guid>())).ReturnsAsync(giaTriGia);
        var service = new XxxService(dep.Object);

        // When (Act) — gọi 1 method THẬT
        var result = await service.Method(input);

        // Then (Assert) — expected TỪ TIÊU CHÍ, không từ code
        Assert.True(result.Success);
        dep.Verify(d => d.GhiGiDo(It.IsAny<T>()), Times.Once);
    }
}
```

`.csproj` (net khớp project gốc):
```xml
<PropertyGroup>
  <TargetFramework>net6.0</TargetFramework>
  <Nullable>enable</Nullable>
  <IsTestProject>true</IsTestProject>
</PropertyGroup>
<ItemGroup>
  <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
  <PackageReference Include="xunit" Version="2.6.6" />
  <PackageReference Include="xunit.runner.visualstudio" Version="2.5.6" />
  <PackageReference Include="Moq" Version="4.20.70" />
</ItemGroup>
<ItemGroup>
  <ProjectReference Include="...\Xxx.Service.csproj" />
</ItemGroup>
```

## 7. Mock cheat-sheet (Moq)
```csharp
dep.Setup(d => d.Get(It.IsAny<object>())).ReturnsAsync(entity);              // stub trả data
dep.Setup(d => d.Delete(It.IsAny<T>(), It.IsAny<bool>())).Returns(Task.CompletedTask); // method trả Task
dep.Setup(d => d.Delete(It.IsAny<T>(), It.IsAny<bool>())).ThrowsAsync(new Exception("boom")); // test catch
dep.Verify(d => d.Delete(entity, false), Times.Once);   // đúng 1 lần + cờ chặt
dep.Verify(d => d.Delete(It.IsAny<T>(), It.IsAny<bool>()), Times.Never); // không gọi
```
> Method trả `Task` (void async) mà không `.Returns(Task.CompletedTask)` → `await null` → NRE. Nhớ set.

## 8. Chạy test
```bash
cd <thư mục test project>
dotnet test
```
Nhìn **dòng cuối**: `Passed! - Failed: 0, Passed: N` = ngon; `Failed!` → in tên test rớt + lý do.
**VS Code:** C# Dev Kit → mở folder có `.sln` → icon **🧪 Testing** → bấm ▶. Repo lớn → tạo solution nhỏ chỉ add project test.

## 9. Checklist trước khi commit
```
☐ Expected lấy từ tiêu chí nghiệm thu — KHÔNG chép code
☐ Given/When/Then rõ; tên = <Method>_<TìnhHuống>_<KỳVọng>
☐ Chỉ mock cổng ra ngoài; ruột service chạy thật
☐ Verify chặt cờ quan trọng (soft/hard, Times.Once/Never)
☐ Phủ: happy + not-found + validation + exception (catch)
☐ dotnet test XANH, 0 warning
☐ Chỗ code ≠ tiêu chí → báo nghi bug (không sửa test cho khớp code)
```

## Quy trình chuẩn 1 task (UT nằm ở đâu trong luồng)
```
Tiêu chí nghiệm thu (Cho–Khi–Thì)  →  Code  →  Review  →  Trace  →  [GỢI Ý] viết/cập nhật UT  →  Confirm
   (từ yêu cầu, KHÔNG từ code)                                        ↑ CODE XONG là tới bước này
```
- **Code xong → tự gợi ý UT** (đã wire vào pipeline `CLAUDE.md` — Claude chủ động DẪN *"viết/cập nhật UT nhé?"* sau Trace).
- Bước UT:
  1. **Đọc test cũ TRƯỚC** → quyết **SỬA** (đổi behavior) hay **THÊM** (method/nhánh mới) — không mặc định thêm mới (mục 5c).
  2. Viết/sửa UT **bám Tiêu chí nghiệm thu** (mục 6), **KHÔNG chép code**.
  3. `dotnet test` → xanh. Đỏ → xem **code sai hay tiêu chí sai** (mục 0-4: 3 loại sai).
  4. Code ≠ tiêu chí → **báo nghi bug**, không ôm theo code.
- **Gợi ý, chưa hard-gate** — đang giai đoạn áp dụng; code đụng logic thì nên làm.

> **FE (Angular)** dùng Jasmine + Karma (`ng test`) — nguyên tắc mục 1–5 giữ nguyên, cú pháp khác (spy thay Moq). Chi tiết FE khi cần bổ sung riêng.
