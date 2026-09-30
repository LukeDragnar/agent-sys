#!/usr/bin/env python
# -*- coding: utf-8 -*-
"""
sb_work.py — Worker DUY NHẤT cho skill "Quản lý công việc" (bản dùng chung cho team).
Danh tính người dùng KHÔNG hardcode: tự suy từ token (claim `id` = DEVELOPER id).
Creds/token/cache của MỖI người ở `memory/local/qlcv/` (local, gitignore) — KHÔNG commit.
Xem SKILL.md + API.md + SETUP.md cùng folder.

Auth: login -> JWT -> gửi làm COOKIE `protoken` (KHÔNG phải Bearer). Token token.txt (~7 ngày).

Lệnh:
  mytasks [--all]            # việc của tôi (lọc server-side ext_DEVELOPER; bỏ 'Đã Update' trừ khi --all)
  detail <Mã>                # chi tiết 1 việc (mô tả, ưu tiên, estimate, deadline, người liên quan)
  timeline [--all]           # ngày dự kiến + estimate (như Gantt /work/custom-view)
  suggest <from-iso> <to-iso> [hours]   # GỢI Ý sắp lịch (TuDongSapXepThoiGian, KHÔNG ghi DB)
  apply <Mã> <field> <value> # GHI 1 trường lịch (ngayBatDauDuKien/ngayKetThucDuKien/estimate) — sau khi user chốt
  setxuly <Mã> <giá trị>     # GHI Trạng thái xử lý (ext) — nghiệp vụ, sau khi user chốt
  create <parentId> <tiêu đề> <est> <bd> <kt> [status] [PL]  # TẠO việc con (bake đủ 4 field để lên timeline)
  assign <Mã> <userName|uid> [dev|test|ba|theodoi] [--yes]   # GÁN người vào ĐÚNG cột theo vai; KHÔNG --yes = chỉ PREVIEW
  users [keyword]            # danh mục user (id -> tên)
  tabs                       # danh sách tab
  raw <Controller/Method> '<json>'   # gọi API bất kỳ
Mật khẩu: env SB_PASS, hoặc creds.json (local). userName: creds.json(user) hoặc env SB_USER.
"""
import os, sys, json, io, base64, urllib.request, urllib.error

BASE   = "https://api.sandbox.com.vn"
ORIGIN = "https://sandbox.sandbox.com.vn"
HERE   = os.path.dirname(os.path.abspath(__file__))   # dir CODE (commit): đọc creds.json.template
# DATA dir (runtime/secret): mặc định repo_root/memory/local/qlcv (gitignore), override bằng env.
# repo_root = 2 cấp trên skills/qlcv/ (skills/ -> repo_root).
DATA = os.environ.get("QLCV_DATA_DIR") or os.path.join(
    os.path.dirname(os.path.dirname(HERE)), "memory", "local", "qlcv")
os.makedirs(DATA, exist_ok=True)
TOKF   = os.path.join(DATA, "token.txt")

def _post(url, body, cookie=None, headers=None):
    data = json.dumps(body).encode("utf-8") if body is not None else None
    req = urllib.request.Request(url, data=data, method="POST")
    req.add_header("content-type", "application/json")
    req.add_header("origin", ORIGIN); req.add_header("referer", ORIGIN + "/")
    if cookie: req.add_header("cookie", cookie)
    for k, v in (headers or {}).items(): req.add_header(k, v)
    try:
        with urllib.request.urlopen(req, timeout=25) as r:
            return r.status, json.loads(r.read().decode("utf-8"))
    except urllib.error.HTTPError as e:
        return e.code, None

def _user():
    """userName: env SB_USER > creds.json(user). KHÔNG có default cứng — thiếu thì báo lỗi rõ."""
    u = os.environ.get("SB_USER")
    if not u:
        cf = os.path.join(DATA, "creds.json")
        if os.path.exists(cf):
            u = json.load(io.open(cf, encoding="utf-8")).get("user")
    if not u:
        sys.exit("THIEU_USER: thiếu userName — set env SB_USER hoặc điền \"user\" trong creds.json "
                 "(tạo từ creds.json.template).")
    return u

def _creds():
    """Ưu tiên env SB_PASS; fallback creds.json (local, gitignore) để tự login."""
    pw = os.environ.get("SB_PASS")
    user = None
    cf = os.path.join(DATA, "creds.json")
    if os.path.exists(cf):
        c = json.load(io.open(cf, encoding="utf-8"))
        user = c.get("user")
        if not pw: pw = c.get("pass")
    user = os.environ.get("SB_USER") or user
    return user, pw

def login():
    user, pw = _creds()
    if not user:
        sys.exit("THIEU_USER: thiếu userName trong creds.json (hoặc env SB_USER).")
    if not pw:
        sys.exit("THIEU_MAT_KHAU: set env SB_PASS hoặc điền \"pass\" trong creds.json.")
    st, js = _post(f"{BASE}/auth/api/Authen/login", {"userName": user, "passWord": pw})
    if not js or not js.get("success"):
        sys.exit(f"LOGIN_FAIL ({st}): {js.get('message') if js else 'no resp'} "
                 "(có thể bị throttle nếu login nhiều lần — chờ vài phút).")
    tok = js["data"]["token"]
    open(TOKF, "w").write(tok)
    return tok

def token():
    if os.path.exists(TOKF):
        t = open(TOKF).read().strip()
        if t: return t
    return login()

_ME = None
def me():
    """DEVELOPER id của người đang đăng nhập — decode claim `id` trong JWT (token.txt).
    Danh tính KHÔNG hardcode: mỗi người chạy tự ra id của mình từ token của mình."""
    global _ME
    if _ME is not None: return _ME
    tok = token()
    try:
        payload = tok.split(".")[1]
        payload += "=" * (-len(payload) % 4)   # pad base64
        claims = json.loads(base64.urlsafe_b64decode(payload).decode("utf-8"))
    except Exception as e:
        sys.exit(f"TOKEN_LOI: không decode được JWT trong token.txt ({e}). Xóa token.txt rồi chạy lại để login.")
    mid = claims.get("id")
    if not mid:
        sys.exit("TOKEN_THIEU_ID: JWT không có claim 'id' (DEVELOPER id). Token có thể sai định dạng.")
    _ME = mid
    return _ME

def api(path, body):
    """Gọi workflow API bằng cookie protoken; tự login lại 1 lần nếu 401."""
    tok = token()
    st, js = _post(f"{BASE}/{path}", body, cookie=f"protoken={tok}")
    if st == 401:
        tok = login()
        st, js = _post(f"{BASE}/{path}", body, cookie=f"protoken={tok}")
    if st != 200 or not js:
        sys.exit(f"API_FAIL {path}: HTTP {st}")
    return js

# ---------- field map (từ DmCongViecMauField) ----------
FIELD = {"DEVELOPER":"Dev phụ trách","PL-TASK":"Phân loại Task","TRANG-THAI-XU-LY":"Trạng thái xử lý",
         "SOURCE":"Nguồn","TESTER":"Tester","LY-DO-TU-CHOI":"CTO Phản hồi","DEADLINE - HOAN- THANH":"Deadline"}

_USERS = None
def users_map():
    """{id: tên} — LayDanhSachUser, cache users.json (đọc lại nếu có)."""
    global _USERS
    if _USERS is not None: return _USERS
    cf = os.path.join(DATA, "users.json")
    if os.path.exists(cf):
        try: _USERS = {u["id"]: u.get("ten") for u in json.load(io.open(cf, encoding="utf-8"))}; return _USERS
        except Exception: pass
    data = api("workflow/api/CongViec/LayDanhSachUser", None).get("data") or []
    json.dump(data, io.open(cf, "w", encoding="utf-8"), ensure_ascii=False)
    _USERS = {u["id"]: u.get("ten") for u in data}
    return _USERS

def _names(val):
    """id / '[\"id\",...]' / 'id, id' → 'Tên A, Tên B' (tra users_map)."""
    if not val: return "—"
    um = users_map(); ids = []
    s = val.strip()
    if s.startswith("["):
        try: ids = json.loads(s)
        except Exception: ids = []
    else:
        ids = [x.strip() for x in s.replace(",", " ").split() if x.strip()]
    return ", ".join(um.get(i, i) for i in ids) if ids else val

def fetch_all(page_size=300, max_pages=8):
    """Kéo hết CongViec (paging) — trả list task. (fallback/tổng quát)"""
    out, page = [], 1
    while page <= max_pages:
        js = api("workflow/api/CongViec/TimTheoDieuKien",
                 {"pageInfo":{"page":page,"pageSize":page_size},"sorts":[]})
        r = js.get("data") or []
        out += r
        if len(out) >= (js.get("totalRecord") or 0) or not r: break
        page += 1
    return out

# ---------- TEAM roster (userName -> (tên, id)) ----------
# ROSTER VÍ DỤ (Team 2 SandBox) — mỗi team tự sửa cho đúng người của mình.
# Chỉ dùng cho lệnh `assign` (map userName -> uid tiện tay). KHÔNG ảnh hưởng "tôi là ai"
# (danh tính người chạy luôn suy từ token qua me()). Có file local `team.json` thì nạp ĐÈ.
TEAM = {
    "thiennq": ("Nguyễn Quang Thiện", "0c657f4d-0dbf-4586-b654-f52497795293"),
    "nganlq":  ("Lê Quang Ngân",      "d8c884f9-b773-45f0-9b53-33e25969ec3a"),
    "tiendv":  ("Đinh Văn Tiến",      "b63418a0-2b87-40bb-a306-31765d56e245"),
    "nghiatd": ("Trần Đại Nghĩa",     "0546b299-16ea-4ac9-a97d-8689ce203a24"),
}
def _load_team():
    """Nạp đè roster từ team.json (local, gitignore) nếu có.
    Format: {"userName": ["Tên", "uid"], ...} hoặc {"userName": {"ten","id"}}."""
    global TEAM
    cf = os.path.join(DATA, "team.json")
    if not os.path.exists(cf): return
    try:
        raw = json.load(io.open(cf, encoding="utf-8"))
    except Exception:
        return
    for k, v in (raw or {}).items():
        if isinstance(v, dict):
            TEAM[k] = (v.get("ten") or v.get("name") or k, v.get("id") or v.get("uid"))
        elif isinstance(v, (list, tuple)) and len(v) >= 2:
            TEAM[k] = (v[0], v[1])
_load_team()

ACTIVE_ST   = {"8. Dev tiếp nhận", "9. Đang dev", "11. Dev ReOpen"}   # đang làm
UPCOMING_ST = {"7. CTO đã phân tích", "2. Task đã duyệt", "10.2. Sẵn sàng Test"}  # sắp tới

def fetch_dev(uid, include_updated=False):
    """Việc của 1 dev — LỌC SERVER-SIDE (tabFilter ext_DEVELOPER) + re-verify + fallback."""
    out, page = [], 1
    while page <= 5:
        js = api("workflow/api/CongViec/TimTheoDieuKien",
                 {"pageInfo":{"page":page,"pageSize":100},"sorts":[],"chiLayGoc":False,
                  "tabFilter":{"matchMode":"all","conditions":[
                      {"field":"ext_DEVELOPER","operator":"equals","value":uid}]}})
        r = js.get("data") or []
        out += r
        if len(out) >= (js.get("totalRecord") or 0) or not r: break
        page += 1
    out = [t for t in out if uid in ((t.get("extFields") or {}).get("DEVELOPER") or "")]
    if not out and uid == me():
        out = [t for t in fetch_all() if me() in ((t.get("extFields") or {}).get("DEVELOPER") or "")]
    if not include_updated:
        out = [t for t in out if (t.get("extFields") or {}).get("TRANG-THAI-XU-LY") != "15. Đã Update"]
    return out

def fetch_mine(include_updated=False):
    return fetch_dev(me(), include_updated)

def cmd_mytasks(include_updated=False):
    mine = fetch_mine(include_updated)
    print(f"# VIỆC CỦA TÔI ({_user()}) — {len(mine)} việc "
          f"{'(gồm đã Update)' if include_updated else '(chưa Update)'}\n")
    print(f"{'Mã':<10} {'Trạng thái xử lý':<24} {'TT':<14} Tiêu đề")
    print("-"*100)
    for t in sorted(mine, key=lambda x:(x.get('extFields') or {}).get('TRANG-THAI-XU-LY') or 'zz'):
        ex = t.get("extFields") or {}
        print(f"{(t.get('ma') or ''):<10} {(ex.get('TRANG-THAI-XU-LY') or '-')[:23]:<24} "
              f"{(t.get('tenTrangThai') or '-')[:13]:<14} {(t.get('tieuDe') or '')[:55]}")
    json.dump(mine, io.open(os.path.join(DATA,"mytasks.json"),"w",encoding="utf-8"), ensure_ascii=False)

def cmd_detail(ma):
    """Chi tiết 1 việc theo mã (đọc id từ cache mytasks.json, fallback fetch_all)."""
    idv = None
    cache = os.path.join(DATA, "mytasks.json")
    if os.path.exists(cache):
        for t in json.load(io.open(cache, encoding="utf-8")):
            if (t.get("ma") or "").lower() == ma.lower(): idv = t["id"]; break
    if not idv:
        for t in fetch_all():
            if (t.get("ma") or "").lower() == ma.lower(): idv = t["id"]; break
    if not idv: sys.exit(f"Không thấy việc mã '{ma}' trong danh sách của bạn.")
    d = api(f"workflow/api/CongViec/TimTheoId/{idv}", None).get("data") or {}
    ex = d.get("extFields") or {}
    import re
    mota = re.sub("<[^>]+>", "", d.get("moTa") or "").strip()
    print(f"# {d.get('ma')} — {d.get('tieuDe')}")
    print(f"Ưu tiên   : {d.get('tenMucDoUuTien')}")
    print(f"Estimate  : {d.get('estimate')}h")
    print(f"Dự kiến   : {(d.get('ngayBatDauDuKien') or '')[:10]} → {(d.get('ngayKetThucDuKien') or '')[:10]}")
    print(f"TT xử lý  : {ex.get('TRANG-THAI-XU-LY')}")
    print(f"Phân loại : {ex.get('PL-TASK')} | Khách: {ex.get('KHACH-HANG')} | Sales: {ex.get('SALES')}")
    print(f"Dev       : {_names(ex.get('DEVELOPER'))} | BA: {_names(ex.get('BA'))} | Tester: {_names(ex.get('TESTER'))}")
    print(f"Theo dõi  : {_names(ex.get('NGUOI-THEO-DOI'))} | Người tạo: {d.get('nguoiTaoUserName')}")
    print(f"\n--- Mô tả ---\n{mota}")

def cmd_timeline(include_updated=False):
    """Timeline việc của tôi: ngày dự kiến + estimate + ưu tiên (gọi detail từng việc)."""
    mine = fetch_mine(include_updated)
    rows = []
    for t in mine:
        d = (api(f"workflow/api/CongViec/TimTheoId/{t['id']}", None).get("data") or {})
        rows.append({
            "ma": d.get("ma"), "tieuDe": d.get("tieuDe"),
            "uuTien": d.get("tenMucDoUuTien"), "estimate": d.get("estimate"),
            "batDau": (d.get("ngayBatDauDuKien") or "")[:10],
            "ketThuc": (d.get("ngayKetThucDuKien") or "")[:10],
            "ttxl": (d.get("extFields") or {}).get("TRANG-THAI-XU-LY"),
        })
    rows.sort(key=lambda r: (r["batDau"] or "9999", r["uuTien"] or "z"))
    print(f"# TIMELINE việc của tôi — {len(rows)} việc\n")
    print(f"{'Mã':<9} {'Bắt đầu':<11}{'Kết thúc':<11}{'Est':<5}{'Ưu tiên':<22} Tiêu đề")
    print("-"*100)
    for r in rows:
        print(f"{(r['ma'] or ''):<9} {(r['batDau'] or '—'):<11}{(r['ketThuc'] or '—'):<11}"
              f"{(str(r['estimate'])+'h' if r['estimate'] is not None else '—'):<5}"
              f"{(r['uuTien'] or '—')[:21]:<22} {(r['tieuDe'] or '')[:45]}")
    json.dump(rows, io.open(os.path.join(DATA,"timeline.json"),"w",encoding="utf-8"), ensure_ascii=False)

def _my_detailed():
    """Trả list dict {id, ma, tieuDe, batDau, ketThuc, estimate} cho việc của tôi (chưa Update)."""
    mine = fetch_mine(False)
    out = []
    for t in mine:
        d = (api(f"workflow/api/CongViec/TimTheoId/{t['id']}", None).get("data") or {})
        out.append({"id": d.get("id"), "ma": d.get("ma"), "tieuDe": d.get("tieuDe"),
                    "batDau": d.get("ngayBatDauDuKien"), "ketThuc": d.get("ngayKetThucDuKien"),
                    "estimate": d.get("estimate")})
    return out

def cmd_suggest(from_date, to_date, hours=8):
    """GỢI Ý sắp lịch (TuDongSapXepThoiGian — KHÔNG ghi DB). Chỉ hiển thị đề xuất cũ→mới."""
    rows = _my_detailed()
    payload = {"Tasks": [{"Id": r["id"], "NgayBatDauDuKien": r["batDau"],
                          "NgayKetThucDuKien": r["ketThuc"], "Estimate": r["estimate"]} for r in rows],
               "From": from_date, "To": to_date, "HoursPerDay": hours, "Algorithm": "work-cap"}
    res = api("workflow/api/CongViec/TuDongSapXepThoiGian", payload).get("data") or {}
    by_id = {r["id"]: r for r in rows}
    ups = res.get("Updates") or res.get("updates") or []
    print(f"# GỢI Ý SẮP LỊCH ({from_date[:10]} → {to_date[:10]}, {hours}h/ngày) — KHÔNG ghi DB")
    print(f"Tổng xét: {res.get('TotalConsidered') or res.get('totalConsidered')} | cần đổi: {len(ups)} | "
          f"không xếp được: {len(res.get('SkippedNoFit') or res.get('skippedNoFit') or [])}\n")
    if not ups:
        print("→ Lịch hiện tại đã hợp lý, không cần đổi."); return
    print(f"{'Mã':<9} {'Bắt đầu cũ→mới':<26}{'Kết thúc cũ→mới':<26} Tiêu đề")
    print("-"*100)
    for u in ups:
        tid = u.get("TaskId") or u.get("taskId"); r = by_id.get(tid, {})
        nb = (u.get("NewNgayBatDau") or u.get("newNgayBatDau") or "")[:10]
        nk = (u.get("NewNgayKetThuc") or u.get("newNgayKetThuc") or "")[:10]
        ob = (r.get("batDau") or "—")[:10]; ok = (r.get("ketThuc") or "—")[:10]
        print(f"{(r.get('ma') or ''):<9} {ob+' → '+nb:<26}{ok+' → '+nk:<26} {(r.get('tieuDe') or '')[:40]}")
    print("\n(đây chỉ là ĐỀ XUẤT — chốt việc nào mình mới gọi CapNhatTruong ghi việc đó)")

def _resolve_id(ma):
    cache = os.path.join(DATA, "mytasks.json")
    if os.path.exists(cache):
        for t in json.load(io.open(cache, encoding="utf-8")):
            if (t.get("ma") or "").lower() == ma.lower(): return t["id"]
    for t in fetch_all():
        if (t.get("ma") or "").lower() == ma.lower(): return t["id"]
    return None

# Chỉ cho phép ghi các trường lịch (an toàn). Trạng thái/ưu tiên có side-effect → không mở qua CLI này.
WRITABLE = {"ngayBatDauDuKien", "ngayKetThucDuKien", "estimate", "moTa"}

def cmd_apply(ma, field, value):
    """GHI 1 trường (CapNhatTruong). CHỈ gọi sau khi user đã chốt thay đổi này.
    ngày -> 'YYYY-MM-DD'; estimate -> số. value 'null'/'' = xóa."""
    if field not in WRITABLE:
        sys.exit(f"Từ chối: field '{field}' không nằm trong danh sách ghi an toàn {sorted(WRITABLE)}.")
    idv = _resolve_id(ma)
    if not idv: sys.exit(f"Không thấy việc mã '{ma}'.")
    val = None if value in ("null", "", None) else value
    import time
    body = {"Id": idv, "Field": field, "Value": val, "ClientTimestamp": int(time.time()*1000)}
    js = api("workflow/api/CongViec/CapNhatTruong", body)
    ok = js.get("success"); print(f"{'✅ ĐÃ GHI' if ok else '❌ LỖI'}: {ma} | {field} = {val}"
                                  f"{'' if ok else ' | '+str(js.get('message'))}")

# Trạng thái xử lý_Tính năng (ext select) — id + giá trị hợp lệ (từ DmCongViecMauField).
XULY_FIELD_ID = "9bb25840-a9a7-4a22-ad58-7838d8c6a354"
XULY_ITEMS = ["1. Task cần duyệt","2. Task đã duyệt","3. BA - Đang xử lý","4. BA - Hoàn thành",
    "4.1 BA leader cần duyệt","5. BA - Pending","6. BA - Cần phân tích thêm","1.1 Trùng yêu cầu",
    "7. CTO đã phân tích","7.1 CTO tạm bỏ qua","8. Dev tiếp nhận","9. Đang dev","10. Dev hoàn thành",
    "10.1 Dev Pending","11. Dev ReOpen","10.2. Sẵn sàng Test","12.  Đang Test","13. Test hoàn thành",
    "15. Đã Update","16. Dev phân tích thêm","14. Leader đã review","16. Đã viết tài liệu & training"]

def cmd_setxuly(ma, value):
    """GHI Trạng thái xử lý_Tính năng (ext field). Hành động NGHIỆP VỤ — chỉ gọi sau khi user chốt."""
    if value not in XULY_ITEMS:
        sys.exit(f"Giá trị '{value}' không có trong danh mục. Hợp lệ: {XULY_ITEMS}")
    idv = _resolve_id(ma)
    if not idv: sys.exit(f"Không thấy việc mã '{ma}'.")
    import time
    body = {"Id": idv, "Field": f"ext:{XULY_FIELD_ID}", "Value": value, "ClientTimestamp": int(time.time()*1000)}
    js = api("workflow/api/CongViec/CapNhatTruong", body)
    ok = js.get("success")
    print(f"{'✅ ĐÃ GHI' if ok else '❌ LỖI'}: {ma} | Trạng thái xử lý = {value}"
          f"{'' if ok else ' | '+str(js.get('message'))}")

# Field-id + template để TẠO task (Save). BAKE đủ 4 field để task lọt filter timeline.
DEV_FIELD_ID = "b375dd8e-216b-472e-a4c6-6497c7215c92"   # Dev phụ trách
PL_FIELD_ID  = "4695c22a-ca1d-4865-b4cd-2e7f6ba1b3c5"   # Phân loại Task
MAU_BA       = "c69088f3-57fb-4d34-a4e1-024f92a4e2c1"   # Công việc mẫu BA

def cmd_create(parent_id, tieuDe, estimate, bd, kt, status="8. Dev tiếp nhận", pl="Task", dev=None, mota=""):
    """TẠO việc con (Save). Set sẵn ĐỦ 4 field để hiện trên timeline: DEVELOPER + status + estimate + PL-TASK.
    CHỈ gọi sau khi user chốt. dev mặc định = người đang đăng nhập (me())."""
    dev = dev or me()
    if status not in XULY_ITEMS: sys.exit(f"status '{status}' không hợp lệ.")
    if pl not in ("Task", "Bug", "Feature", "Module"): sys.exit(f"PL '{pl}' không hợp lệ.")
    body = {"ma": None, "tieuDe": tieuDe, "moTa": mota or f"<p>{tieuDe}</p>",
            "estimate": int(estimate), "ngayBatDauDuKien": bd, "ngayKetThucDuKien": kt,
            "congViecMauId": MAU_BA, "idCongViecCha": parent_id, "listIdUser": [dev],
            "fields": [{"idCongViecMauField": DEV_FIELD_ID, "giaTri": dev},
                       {"idCongViecMauField": XULY_FIELD_ID, "giaTri": status},
                       {"idCongViecMauField": PL_FIELD_ID, "giaTri": pl}]}
    r = api("workflow/api/CongViec/Save", body)
    print(f"{'✅ ĐÃ TẠO' if r.get('success') else '❌ LỖI'}: {tieuDe[:40]} → id {r.get('data')} "
          f"({estimate}h, {bd}→{kt}, {status}, {pl}){'' if r.get('success') else ' | '+str(r.get('message'))}")

# ---------- ASSIGN: gán người phụ trách vào ĐÚNG cột theo VAI ----------
# Mỗi vai = 1 field khác nhau (dev != test). field-id lấy từ DmCongViecMauField (mẫu BA).
ASSIGN_FIELDS = {
    "dev":     ("DEVELOPER",      DEV_FIELD_ID,                           "Dev phụ trách"),
    "test":    ("TESTER",         "55acafb7-1aa2-4de2-8af2-7e668e0c19d9", "Tester"),
    "ba":      ("BA",             "6e676762-d23a-41c9-a8ff-7092c80f0173", "BA"),
    "theodoi": ("NGUOI-THEO-DOI", "cb512697-42d0-41b3-a86c-1908759985ff", "Người theo dõi"),
}

def _resolve_uid(who):
    """who = userName trong TEAM -> id; hoặc chính là 1 guid uid -> dùng luôn (tester ngoài team)."""
    if who in TEAM: return TEAM[who][1], TEAM[who][0]
    if len(who) == 36 and who.count("-") == 4: return who, users_map().get(who, who)
    return None, None

def cmd_assign(ma, who, role="dev", yes=False):
    """GÁN người vào ĐÚNG cột theo vai (dev->DEVELOPER, test->TESTER, ...). Hệ thống TỰ noti.
    Mặc định PREVIEW (không ghi). CHỈ ghi khi yes=True (--yes) — và chỉ sau khi user confirm."""
    role = role.lower()
    if role not in ASSIGN_FIELDS:
        sys.exit(f"role '{role}' không hợp lệ. Chọn: {list(ASSIGN_FIELDS)}")
    code, fid, label = ASSIGN_FIELDS[role]
    uid, ten = _resolve_uid(who)
    if not uid:
        sys.exit(f"Không rõ người '{who}'. Dùng userName trong team {list(TEAM)} hoặc 1 uid (guid).")
    idv = _resolve_id(ma)
    if not idv: sys.exit(f"Không thấy việc mã '{ma}'.")
    d = api(f"workflow/api/CongViec/TimTheoId/{idv}", None).get("data") or {}
    cur = (d.get("extFields") or {}).get(code)
    print(f"# ASSIGN {ma} — {d.get('tieuDe')}")
    print(f"  Vai/Cột : {role}  →  '{label}' (code {code})")
    print(f"  Hiện tại: {_names(cur)}")
    print(f"  Gán mới : {ten}  ({uid})")
    if not yes:
        print("  → PREVIEW (CHƯA ghi). Thêm --yes để thực hiện. Hệ thống sẽ tự bắn noti khi ghi.")
        return
    import time
    body = {"Id": idv, "Field": f"ext:{fid}", "Value": uid, "ClientTimestamp": int(time.time()*1000)}
    js = api("workflow/api/CongViec/CapNhatTruong", body)
    ok = js.get("success")
    print(f"{'✅ ĐÃ GÁN' if ok else '❌ LỖI'}: {ma} | {label} = {ten}"
          f"{'' if ok else ' | '+str(js.get('message'))}")

def cmd_tabs():
    js = api("workflow/api/CongViecTab/TimTheoDieuKien",{"pageInfo":{"page":1,"pageSize":100},"sorts":[{"field":"thuTu","dir":1}]})
    for t in js.get("data") or []:
        print(t.get("id"),"|",t.get("tenTab"),"|",t.get("cheDoXem"))

def cmd_users(keyword=""):
    um = users_map()
    k = keyword.lower()
    for uid, ten in um.items():
        if not k or k in (ten or "").lower(): print(uid, "|", ten)

import datetime as _dt

def _label(v):
    """PL-TASK / TRANG-THAI-XU-LY có thể là dict hoặc chuỗi JSON → lấy 'label'."""
    if not v: return ""
    if isinstance(v, dict): return v.get("label") or ""
    s = str(v).strip()
    if s.startswith("{"):
        try: return (json.loads(s) or {}).get("label") or ""
        except Exception: return s
    return s

def _date(v):
    if not v: return None
    try: return _dt.date.fromisoformat(str(v)[:10])
    except Exception: return None

DONE_ST = {"15. Đã Update", "14. Đóng", "13. Test hoàn thành"}  # coi như xong, không nhắc

# ---------- SƠ ĐỒ TEAM (team-map.json, COMMIT) — session tự nhận team theo user ----------
# 'domains' = keyword tiêu đề để "care" việc CHƯA GÁN thuộc nghiệp vụ team. Bổ sung dần qua đúc-kết.
_FALLBACK_DOMAIN = [
    "hóa đơn", "hđđt", "hddt", "xuất hóa đơn", "invoice", "ticket", "hỗ trợ", "591",
    "đơn hàng", "giao vận", "giao hàng", "vận chuyển", "đăng đơn", "ghn", "ghtk",
    "viettelpost", "spx", "shopee", "tiktok", "lazada", "đồng bộ đơn", "đồng bộ sàn", "tmđt",
    "kho", "tồn kho", "phiếu nhập", "phiếu xuất", "phiếu chuyển", "imei",
    "hoa hồng", "thu ngân", "công nợ", "tiền nợ", "zalo", "zns", " oa ",
]
def _load_team_map():
    """Đọc team-map.json: ưu tiên bản COMMIT (HERE), rồi override local (DATA)."""
    tm = {}
    for p in (os.path.join(HERE, "team-map.json"), os.path.join(DATA, "team-map.json")):
        if os.path.exists(p):
            try:
                raw = json.load(io.open(p, encoding="utf-8"))
                if isinstance(raw, dict) and raw.get("teams"): tm = raw
            except Exception: pass
    return tm
TEAM_MAP = _load_team_map()

def _my_team():
    """(roster {uid:ten}, domains[list], label) của team chứa user hiện tại. Fallback: TEAM code."""
    uid = me()
    for key, tm in (TEAM_MAP.get("teams") or {}).items():
        ids = {m.get("id"): (m.get("ten") or u) for u, m in (tm.get("members") or {}).items()}
        if uid in ids:
            doms = [str(x).lower() for x in (tm.get("domains") or [])]
            return ids, (doms or _FALLBACK_DOMAIN), tm.get("label", key)
    return ({u_id: ten for (ten, u_id) in TEAM.values()}, _FALLBACK_DOMAIN, "Team (fallback code)")

def cmd_radar(days_soon=3):
    """RADAR — quét CẢ TEAM (roster) + việc CHƯA GÁN, phân nhóm quá hạn/hôm nay/mới/sắp, BUG ưu tiên.
    Read-only. Ghi kèm sổ theo dõi memory/local/qlcv/RADAR.md."""
    today = _dt.date.today()
    um = users_map()
    roster, domains, tlabel = _my_team()
    seen = {}
    for uid in roster:
        for t in fetch_dev(uid, include_updated=False):
            seen[t["id"]] = t
    for t in fetch_all():
        ex = t.get("extFields") or {}
        if (ex.get("DEVELOPER") or "").strip(): continue   # bỏ việc đã có người
        title = (t.get("tieuDe") or "").lower()
        prio = t.get("tenMucDoUuTien") or ""
        tao = _date(t.get("ngayTao"))
        indom = any(k in title for k in domains)   # thuộc nghiệp vụ team
        # bug Cao MỚI ≤7 ngày: vẫn nêu dù chưa khớp domain (chưa gán kịp cũng phải care)
        fresh = ((_label(ex.get("PL-TASK")) == "Bug" or (t.get("ma") or "").upper().startswith("B"))
                 and "Cao" in prio and tao and (today - tao).days <= 7)
        if indom or fresh:
            seen.setdefault(t["id"], t)

    def info(t):
        ex = t.get("extFields") or {}
        return {"ma": t.get("ma") or "", "td": t.get("tieuDe") or "",
                "kt": _date(t.get("ngayKetThucDuKien")), "tao": _date(t.get("ngayTao")),
                "est": t.get("estimate"), "prio": t.get("tenMucDoUuTien") or "",
                "pl": _label(ex.get("PL-TASK")), "ttxl": _label(ex.get("TRANG-THAI-XU-LY")),
                "dev": (ex.get("DEVELOPER") or "").strip(),
                "kh": (ex.get("KHACH-HANG") or "").split("|")[0].strip()}
    rows = [r for r in (info(t) for t in seen.values()) if r["ttxl"] not in DONE_ST]

    def devname(r):
        if not r["dev"]: return "CHƯA GÁN"
        ids = [x.strip() for x in r["dev"].replace(",", " ").split() if x.strip()]
        return ", ".join(um.get(i, i[:8]) for i in ids)
    def isbug(r): return r["pl"] == "Bug" or r["ma"].upper().startswith("B")
    def iscao(r): return "Cao" in r["prio"]

    def bucket(rs):
        ov, td, nw, sn = [], [], [], []
        for r in rs:
            kt = r["kt"]
            if kt and kt < today: r["late"] = (today - kt).days; ov.append(r)
            elif kt and kt == today: td.append(r)
            elif r["tao"] and (today - r["tao"]).days <= 2: nw.append(r)
            elif kt and today < kt <= today + _dt.timedelta(days=days_soon): sn.append(r)
        ov.sort(key=lambda r: (not isbug(r), not iscao(r), -r.get("late", 0)))
        for g in (td, nw, sn): g.sort(key=lambda r: (not isbug(r), not iscao(r), r["ma"]))
        return ov, td, nw, sn

    # TÁCH 2 loại: việc team (đã gán 1 trong roster) vs CHƯA GÁN (nghiệp vụ team)
    assigned   = [r for r in rows if r["dev"]]
    unassigned = [r for r in rows if not r["dev"]]
    a_ov, a_td, a_nw, a_sn = bucket(assigned)
    # chưa gán: chỉ NÊU cái MỚI (≤3d) để triage gán; tồn cũ quá hạn chỉ ĐẾM + top
    u_new = sorted([r for r in unassigned if r["tao"] and (today - r["tao"]).days <= 3],
                   key=lambda r: (not isbug(r), not iscao(r), r["ma"]))
    u_ov = [r for r in unassigned if r["kt"] and r["kt"] < today]
    for r in u_ov: r["late"] = (today - r["kt"]).days
    u_ov.sort(key=lambda r: (not iscao(r), -r["late"]))

    def fmt(r, late=False):
        tag = "🐛" if isbug(r) else "·"
        lt = f" (trễ {r['late']}d)" if late and r.get("late") else ""
        return (f"  {tag} {r['ma']:<9} {(r['prio'] or '-')[:8]:<8} {str(r['est'] or '-'):>3}h "
                f"{devname(r)[:14]:<14} {(r['kh'] or '')[:12]:<12} {r['td'][:40]}{lt}")
    def section(title, g, late=False):
        print(f"\n{title} ({len(g)})")
        for r in g: print(fmt(r, late))

    print(f"# 🛰️  RADAR — {tlabel} — {today.isoformat()} | việc team: {len(assigned)} "
          f"(bug {sum(1 for r in assigned if isbug(r))}) | chưa-gán domain: {len(unassigned)} | roster {len(roster)}")
    print("\n===== VIỆC TEAM (đã gán — PHẢI XỬ LÝ) =====")
    section("🔴 QUÁ HẠN", a_ov, late=True)
    section("⏰ HẠN HÔM NAY", a_td)
    section("🆕 MỚI (≤2 ngày)", a_nw)
    section(f"🟠 SẮP ĐẾN (≤{days_soon}d)", a_sn)
    print("\n===== CHƯA GÁN thuộc nghiệp vụ team (TRIAGE — cần gán ai?) =====")
    section("🆘 MỚI về ≤3 ngày (gán gấp)", u_new)
    print(f"\n📊 Tồn CHƯA GÁN quá hạn: {len(u_ov)} bug/việc — top 8 lâu nhất:")
    for r in u_ov[:8]: print(fmt(r, late=True))
    print(f"   → xem đủ trong RADAR.md")

    # sổ theo dõi (đầy đủ để drill)
    try:
        stamp = _dt.datetime.now().strftime("%Y-%m-%d %H:%M")
        with io.open(os.path.join(DATA, "RADAR.md"), "w", encoding="utf-8") as f:
            f.write(f"# 🛰️ RADAR — sổ theo dõi việc {tlabel} (auto)\n\n> Quét **{stamp}**. "
                    f"Việc team (đã gán): {len(assigned)} · Chưa-gán domain: {len(unassigned)} · "
                    f"bug: {sum(1 for r in rows if isbug(r))}. Chạy lại: `python sb_work.py radar`.\n")
            f.write("\n# VIỆC TEAM (đã gán)\n")
            for title, g, late in [("🔴 QUÁ HẠN", a_ov, True), ("⏰ HÔM NAY", a_td, False),
                                   ("🆕 MỚI ≤2d", a_nw, False), ("🟠 SẮP ĐẾN", a_sn, False)]:
                f.write(f"\n## {title} ({len(g)})\n")
                for r in g:
                    lt = f" — trễ {r['late']}d" if late and r.get("late") else ""
                    f.write(f"- `{r['ma']}` [{r['prio']}] {r['est'] or '-'}h · {devname(r)} · {r['kh']} · {r['td']}{lt}\n")
            f.write("\n# CHƯA GÁN (nghiệp vụ team — triage)\n")
            f.write(f"\n## 🆘 MỚI ≤3 ngày ({len(u_new)})\n")
            for r in u_new:
                f.write(f"- `{r['ma']}` [{r['prio']}] {r['est'] or '-'}h · {r['kh']} · {r['td']}\n")
            f.write(f"\n## 📊 Tồn quá hạn chưa gán ({len(u_ov)})\n")
            for r in u_ov:
                f.write(f"- `{r['ma']}` [{r['prio']}] {r['est'] or '-'}h · {r['kh']} · {r['td']} — trễ {r['late']}d\n")
        print(f"\n→ Đã ghi sổ: {os.path.join(DATA, 'RADAR.md')}")
    except Exception as e:
        print(f"\n(⚠ không ghi được RADAR.md: {e})")

def cmd_eod():
    """CHỐT NGÀY — đối chiếu timeline việc team (đã gán) vs thực tế → ĐỀ XUẤT chỉnh (KHÔNG ghi).
    Chỉ đề xuất dời NGÀY; KHÔNG đụng estimate việc đang doing (est gốc = căn cứ KPI)."""
    today = _dt.date.today()
    um = users_map()
    roster, domains, tlabel = _my_team()
    tasks = {}
    for uid in roster:
        for t in fetch_dev(uid, include_updated=False):
            tasks[t["id"]] = t
    import re
    def stnum(s):
        m = re.match(r"\s*(\d+(?:\.\d+)?)", s or "")
        return float(m.group(1)) if m else -1.0
    def is_done(s):   n = stnum(s); return 10 <= n <= 15 and n != 11   # dev xong → QA/deploy/đóng
    def not_start(s): n = stnum(s); return n < 0 or n in (1, 2, 5, 7, 7.1, 8)
    props = []
    for t in tasks.values():
        ex = t.get("extFields") or {}
        ttxl = _label(ex.get("TRANG-THAI-XU-LY"))
        if is_done(ttxl): continue          # xong phần dev → timeline khỏi chỉnh
        kt = _date(t.get("ngayKetThucDuKien")); bd = _date(t.get("ngayBatDauDuKien"))
        ma = t.get("ma") or ""; td = (t.get("tieuDe") or "")[:36]
        dev = ", ".join(um.get(i, i[:6]) for i in (ex.get("DEVELOPER") or "").replace(",", " ").split())
        prio = t.get("tenMucDoUuTien") or ""; est = t.get("estimate") or "-"
        try: bump = max(1, -(-int(est) // 8))   # gợi ý dời = hôm nay + ceil(est/8) ngày
        except Exception: bump = 1
        newkt = (today + _dt.timedelta(days=bump)).isoformat()
        if kt and kt < today:
            props.append((ma, "ngayKetThucDuKien", kt.isoformat(), newkt,
                          f"quá hạn {(today - kt).days}d, '{ttxl or '—'}'", dev, prio, td))
        elif kt == today and not_start(ttxl):
            props.append((ma, "ngayKetThucDuKien", kt.isoformat(), newkt,
                          f"đến hạn HÔM NAY mà '{ttxl or '—'}'", dev, prio, td))
        elif bd and bd < today and not_start(ttxl):
            props.append((ma, "ngayBatDauDuKien", bd.isoformat(), today.isoformat(),
                          f"lịch bắt đầu {bd.isoformat()} đã qua, '{ttxl or '—'}'", dev, prio, td))
    props.sort(key=lambda p: ("Cao" not in p[6], p[0]))
    print(f"# 🌙 CHỐT NGÀY — {tlabel} — {today.isoformat()} | {len(props)} đề xuất chỉnh timeline (CHƯA GHI)")
    print("Duyệt cái nào → `apply <mã> <field> <YYYY-MM-DD>`. KHÔNG đổi estimate việc đang doing.\n")
    if props:
        print(f"{'Mã':<9} {'Field':<20} {'Cũ':<11}{'→ Gợi ý':<12}{'Ưu tiên':<9}{'Dev':<9} Lý do")
        print("-" * 116)
        for ma, fld, old, new, why, dev, prio, td in props:
            print(f"{ma:<9} {fld:<20} {old:<11}{new:<12}{(prio or '-')[:8]:<9}{(dev or '-')[:8]:<9} {why} · {td}")
    else:
        print("  (không có gì cần chỉnh — timeline khớp thực tế 👍)")
    try:
        stamp = _dt.datetime.now().strftime("%Y-%m-%d %H:%M")
        with io.open(os.path.join(DATA, "EOD-proposals.md"), "w", encoding="utf-8") as f:
            f.write(f"# 🌙 CHỐT NGÀY {today.isoformat()} — đề xuất chỉnh timeline ({stamp})\n")
            f.write("> CHƯA GHI. Duyệt từng cái → `python sb_work.py apply <mã> <field> <YYYY-MM-DD>`. "
                    "Không đổi estimate việc đang doing.\n\n")
            for ma, fld, old, new, why, dev, prio, td in props:
                f.write(f"- [ ] `{ma}` **{fld}**: `{old}` → `{new}` · {prio} · {dev} · {why} · {td}\n")
        print(f"\n→ Ghi: {os.path.join(DATA, 'EOD-proposals.md')}")
    except Exception as e:
        print(f"(⚠ {e})")

def cmd_radaralert():
    """WATCHER (chạy ngầm) — chỉ IN bug Cao MỚI xuất hiện kể từ lần quét trước (so radar-seen.json).
    Không có gì mới → im lặng. Lần đầu: seed im lặng (không flood)."""
    roster, domains, tlabel = _my_team()
    cur = {}
    def consider(t, assigned):
        ex = t.get("extFields") or {}
        if _label(ex.get("TRANG-THAI-XU-LY")) in DONE_ST: return
        if "Cao" not in (t.get("tenMucDoUuTien") or ""): return
        ma = t.get("ma") or ""
        if not (_label(ex.get("PL-TASK")) == "Bug" or ma.upper().startswith("B")): return
        cur[ma] = {"td": (t.get("tieuDe") or "")[:44],
                   "kh": (ex.get("KHACH-HANG") or "").split("|")[0].strip(), "assigned": assigned}
    for uid in roster:
        for t in fetch_dev(uid, include_updated=False): consider(t, True)
    for t in fetch_all():
        ex = t.get("extFields") or {}
        if (ex.get("DEVELOPER") or "").strip(): continue
        if any(k in (t.get("tieuDe") or "").lower() for k in domains): consider(t, False)
    seenf = os.path.join(DATA, "radar-seen.json")
    old = {}
    if os.path.exists(seenf):
        try: old = json.load(io.open(seenf, encoding="utf-8"))
        except Exception: old = {}
    json.dump(cur, io.open(seenf, "w", encoding="utf-8"), ensure_ascii=False)
    if not old: return                       # lần đầu: seed im lặng
    new_ma = [m for m in cur if m not in old]
    if new_ma:
        stamp = _dt.datetime.now().strftime("%H:%M")
        print(f"RADAR-ALERT {stamp}: {len(new_ma)} BUG CAO MỚI ({tlabel})")
        for m in new_ma:
            c = cur[m]
            print(f"  BUG {m} · {c['kh']}{'' if c['assigned'] else ' · CHƯA GÁN'} · {c['td']}")

def main():
    a = sys.argv[1:] or ["mytasks"]
    cmd = a[0]
    if cmd == "radar": cmd_radar(int(a[1]) if len(a) > 1 else 3)
    elif cmd == "eod": cmd_eod()
    elif cmd == "radaralert": cmd_radaralert()
    elif cmd == "mytasks": cmd_mytasks("--all" in a)
    elif cmd == "detail": cmd_detail(a[1])
    elif cmd == "timeline": cmd_timeline("--all" in a)
    elif cmd == "suggest":
        frm = a[1] if len(a) > 1 else "2026-07-04T00:00:00"
        to  = a[2] if len(a) > 2 else "2026-08-04T00:00:00"
        cmd_suggest(frm, to, int(a[3]) if len(a) > 3 else 8)
    elif cmd == "apply": cmd_apply(a[1], a[2], a[3] if len(a) > 3 else "")
    elif cmd == "setxuly": cmd_setxuly(a[1], a[2])
    elif cmd == "create":  # create <parentId> <tieuDe> <estimate> <bd> <kt> [status] [pl] [moTa]
        cmd_create(a[1], a[2], a[3], a[4], a[5],
                   a[6] if len(a) > 6 else "8. Dev tiếp nhận", a[7] if len(a) > 7 else "Task",
                   mota=a[8] if len(a) > 8 else "")
    elif cmd == "assign":  # assign <Mã> <userName|uid> [role] [--yes]
        yes = "--yes" in a
        rest = [x for x in a[1:] if x != "--yes"]
        cmd_assign(rest[0], rest[1], rest[2] if len(rest) > 2 else "dev", yes)
    elif cmd == "tabs":  cmd_tabs()
    elif cmd == "users": cmd_users(a[1] if len(a) > 1 else "")
    elif cmd == "raw":
        body = json.loads(a[2]) if len(a) > 2 and a[2] else None
        print(json.dumps(api(a[1], body), ensure_ascii=False, indent=1)[:4000])
    else: sys.exit(__doc__)

if __name__ == "__main__":
    sys.stdout.reconfigure(encoding="utf-8")
    main()
