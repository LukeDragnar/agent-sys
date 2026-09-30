---
name: scrm_frontend Docker Registry
description: Private Docker registry hub.pushsale.vn dùng cho scrm_frontend — base images phải pull từ đây vì staging không có internet ra Docker Hub
type: project
---

Build machine (staging) không có internet trực tiếp ra Docker Hub → TLS handshake timeout khi pull image.

**Private registry:** `hub.pushsale.vn`
- `hub.pushsale.vn/scrm/fullbooking.client:staging-1.0` — staging
- `hub.pushsale.vn/scrm-sprint/fullbooking.client:sprint-1.0` — sprint
- `hub.pushsale.vn/scrm-prod/fullbooking.client:prod-4.3117` — prod

**Fix:** Base image trong Dockerfile phải dùng private registry:
```dockerfile
FROM hub.pushsale.vn/base/nginx:stable-alpine
```

Trước khi dùng lần đầu, push nginx lên registry từ máy có internet:
```bash
docker pull nginx:stable-alpine
docker tag nginx:stable-alpine hub.pushsale.vn/base/nginx:stable-alpine
docker push hub.pushsale.vn/base/nginx:stable-alpine
```

**Why:** Staging server nằm trong mạng nội bộ, chỉ access được private registry, không ra được Docker Hub.

**How to apply:** Bất kỳ Dockerfile nào trong project scrm đều phải dùng base image từ `hub.pushsale.vn/base/` thay vì Docker Hub.
