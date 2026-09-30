# Skill "Quản lý thông tin project" — Sổ hạ tầng SCRM

> 🎯 **Mục đích:** 1 chỗ DUY NHẤT để tra & giữ thông tin hạ tầng project — hết mò thủ công Excel/appsettings/Portainer.
> Trả lời nhanh: *service X ở server/DB/port nào · con nào chạy Y · version deploy bao nhiêu · Redis/Rabbit/ES ở đâu*.

## Kiến trúc (giống qlcv: khung commit · data local)
| Thành phần | Chỗ | Commit? |
|---|---|---|
| Khung skill (file này) + `infra.sh` | `skills/thong-tin-project/` | ✅ commit (team dùng chung) |
| **Command điểm vào** | `.claude/commands/quan-ly-thong-tin-project.md` | ✅ commit |
| **DỮ LIỆU sổ hạ tầng** | `memory/local/infra-registry.md` | 🔒 **LOCAL, gitignore — KHÔNG push** (nhạy cảm: IP/DB nội bộ) |

> Vì sao data để local: chứa IP/DB nội bộ. Nếu team muốn chia sẻ chung → thiennq quyết chuyển sang `memory/` (committed). Mặc định giữ local cho an toàn.

## Khi nào chạy
User hỏi: *"X ở server nào / DB gì / port mấy"*, *"con nào chạy service Y"*, *"version deploy Z"*, *"IP Mongo/Redis/Rabbit"*, *"thông tin hạ tầng/project"* — hoặc `/quan-ly-thong-tin-project`.

## 3 tác vụ

### 1. TRA CỨU (mặc định — read-only, làm ngay)
- `bash skills/thong-tin-project/infra.sh <từ khóa>` → grep sổ. Không tham số → liệt kê mục.
- Hoặc đọc thẳng `memory/local/infra-registry.md` (13 mục: SQL · Mongo · Redis · RabbitMQ · ES · APM · internal-URL · Portainer/deploy · domain · git · version · tools · creds-location).
- Trả lời gọn: service → host:port + DB + version. VD "Invoice": SQL `10.0.63.91` · Mongo `10.0.63.33` · Portainer app-server `.71`.

### 2. BỔ SUNG / SỬA (WRITE — CONFIRM trước khi ghi)
Khi trao đổi nhặt được info mới (IP/port/DB/version/domain/creds-location) hoặc phát hiện sai:
1. **Cân nhắc:** đúng mục nào? có trùng/mâu thuẫn dòng cũ không? prod hay staging (đúng subnet)?
2. **Trình đề xuất** (dòng sẽ thêm/sửa) → **CONFIRM với thiennq**.
3. OK → ghi vào `infra-registry.md`, kèm ngày nếu là cập nhật.
- 🔴 **KHÔNG bao giờ ghi password thô** — chỉ ghi "creds ở <file/chỗ nào>".

### 3. VERIFY / QUÉT LẠI (khi nghi ngờ hoặc muốn refresh)
- **Version deploy:** đọc trên **build path** `_SourceForBuild/scrm_build` (@release_product) + `build_scrm_frontend`, KHÔNG folder dev. (bài học: báo "đã đẩy" phải verify code+version thật.)
- **Topology (SQL/Mongo/Redis/Rabbit/ES/Portainer):** quét `scrm_build/Libs/SB.Core/appsettings.base.{Production,Staging}.json` + per-service appsettings + `BuildCodeSCRM/.../Data/Portainer*.xml`. Quét toàn bộ → **spawn 1 agent read-only** (redact mọi password; chỉ lấy host/port/DB/service).

## Convention BẮT BUỘC
- 🔒 **Data LOCAL** (memory/local/, gitignore) — không push.
- 🔴 **KHÔNG password thô** trong sổ — chỉ trỏ chỗ giữ (appsettings redacted / Excel / password manager).
- ✅ **Verify từ nguồn thật** (config/build path) — đừng tin suông. Nghi/lỗi thời → verify rồi cập nhật, ghi ngày.
- 🌐 **Subnet:** `10.0.63.x`=Production · `10.0.161.x`=Staging · `10.0.61.x`=APM.
- ⚠️ Có mục "CẦN XÁC NHẬN / MÂU THUẪN" ở cuối sổ — các điểm chờ thiennq chốt (vd Redis public-IP legacy, WarehouseProduct host).

## Nguồn dữ liệu gốc (để verify/rescan)
- Config: `_SourceForBuild/scrm_build/Libs/SB.Core/appsettings.base.{Production,Staging}.json` + per-service.
- Portainer/deploy: `scrm_claude/BuildCodeSCRM/FastDeploy/.../Data/Portainer{Production,Staging,Sprint,Dev}.xml` + `SearchPath.xml`.
- Excel tay của thiennq (sheet PortainerProduct/Staging/Sprint/Dev + DbProduct + IPPublic) — có thêm Portainer UI creds + cross-check.
- Deploy version: build path (mục 3 VERIFY).
