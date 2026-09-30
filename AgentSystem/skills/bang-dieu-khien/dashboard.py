# -*- coding: utf-8 -*-
"""
dashboard.py — sinh BẢNG ĐIỀU KHIỂN dạng HTML cho người dùng hiện tại.

Chạy:  python skills/qlcv/dashboard.py
Ra:    memory/local/qlcv/dashboard/YYYY-MM-DD.html  (+ index.html liệt kê theo ngày)

- Danh tính lấy từ token (giống sb_work) → ai chạy ra bảng người đó, KHÔNG hardcode.
- Chạy lại trong ngày = GHI ĐÈ đúng file ngày đó, không đẻ trùng.
- 3 ô máy không tự biết (Đang làm / Rủi ro / Chờ quyết) đọc từ file ghi tay:
      memory/local/qlcv/dashboard-notes.md
  Không có file đó thì 3 ô hiện "chưa có ghi chú" — phần số liệu vẫn chạy bình thường.
"""
import os, sys, io, re, json, datetime, subprocess, concurrent.futures as cf

sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding="utf-8", errors="replace")
HERE = os.path.dirname(os.path.abspath(__file__))
REPO = os.path.abspath(os.path.join(HERE, "..", ".."))
sys.path.insert(0, os.path.join(REPO, "skills", "qlcv"))   # mượn engine auth/API của qlcv
import sb_work as W
DATA = os.path.join(REPO, "memory", "local", "qlcv")
OUTDIR = os.path.join(DATA, "dashboard")
NOTES = os.path.join(DATA, "dashboard-notes.md")
ALLOC = os.path.join(DATA, "FOLDER-ALLOCATION.md")
GROUPS_ROOT = r"D:\PROJECT\Git\_SourceForCode"
TODAY = datetime.date.today()
NOW = datetime.datetime.now().timestamp()

DONE = ("13.", "14.", "15.")          # coi như đã xong, không tính vào tải
# Cụm thuế/HĐĐT — đang căng (nghị định mới), phải bám sát nên tách riêng 1 ô
TAX = re.compile(r"thuế|thue\b|hđđt|hddt|hóa đơn|hoá đơn|\bmst\b|\bvat\b|invoice|nghị định|hạch toán", re.I)


# ─────────────────────────── tiện ích ───────────────────────────
def lbl(v):
    """Trường động có khi là dict, có khi là chuỗi JSON — lấy .label cho đồng nhất."""
    if isinstance(v, dict):
        return v.get("label") or ""
    if isinstance(v, str) and v.strip().startswith("{"):
        try:
            return json.loads(v).get("label") or ""
        except Exception:
            return v
    return v or ""


def esc(s):
    return (str(s or "").replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;"))


def days_late(iso):
    if not iso:
        return None
    try:
        return (TODAY - datetime.date.fromisoformat(iso[:10])).days
    except Exception:
        return None


ST_SHORT = [("11.", "11 ReOpen", "r"), ("16.", "16 PT thêm", "a"), ("10.2", "10.2 chờ test", "b"),
            ("10.", "10 dev xong", "b"), ("9.", "9 đang dev", "g"), ("8.", "8 đã nhận", "a"),
            ("7.1", "7.1 bỏ qua", "m"), ("7. CTO", "7 CTO đã PT", "m"), ("7.", "7 đã PT", "m"),
            ("12.", "12 đang test", "b"), ("2.", "2 đã duyệt", "m"), ("1.1", "1.1 trùng", "m"),
            ("1.", "1 chờ duyệt", "m"), ("5.", "5 BA pending", "m"), ("4.", "4 BA", "m")]


def st_cell(st):
    """Trạng thái ra CỘT RIÊNG, rút gọn cho vừa — giữ nguyên số, di chuột ra chữ đầy đủ."""
    if not st:
        return '<span class="m">—</span>'
    for pre, short, cls in ST_SHORT:
        if st.startswith(pre):
            return f'<span class="{cls}" title="{esc(st)}">{short}</span>'
    return f'<span class="m">{esc(st[:14])}</span>'


def late_cell(n):
    if n is None:
        return '<span class="m">—</span>'
    if n > 7:
        return f'<span class="r">{n}d</span>'
    if n > 0:
        return f'<span class="a">{n}d</span>'
    if n == 0:
        return '<span class="a">hôm nay</span>'
    return f'<span class="g">còn {-n}d</span>'


def git(path, *args, timeout=90):
    """Trả None khi LỖI/TIMEOUT — KHÔNG trả chuỗi rỗng.

    Rỗng và lỗi phải phân biệt được: nuốt lỗi thành "" làm repo đang bẩn hiện ra là SẠCH,
    rồi ô folder báo 'trống' → giao việc đè lên code người khác. (Dính thật 29/07 khi quét
    song song 36 repo với timeout 12s.)
    """
    try:
        r = subprocess.run(["git", "-C", path, *args], capture_output=True, text=True,
                           timeout=timeout, encoding="utf-8", errors="replace")
        return r.stdout.strip() if r.returncode == 0 else None
    except Exception:
        return None


# ─────────────────────────── lấy dữ liệu ───────────────────────────
def fetch_all():
    js = W.api("workflow/api/CongViec/TimTheoDieuKien",
               {"pageInfo": {"page": 1, "pageSize": 3000}, "sorts": [], "chiLayGoc": False})
    return js.get("data") or []


def roster():
    js = W.api("workflow/api/CongViec/LayDanhSachUser", {})
    return {u.get("id"): (u.get("ten") or u.get("userName") or "") for u in (js.get("data") or [])}


DEVFLOW = ("7.", "8.", "9.", "10.", "11.", "16.")   # đã vào luồng dev


def fetch_tax(data, me, who):
    """Cụm THUẾ/HĐĐT — quét TOÀN TEAM (không chỉ việc mình) để không sót cái ai đó đang ôm.

    2 tầng: đang trong luồng dev → hiện từng dòng; còn ở BA/chờ duyệt → gom 1 dòng tổng,
    tránh 37 dòng lẫn lộn làm ô mất tác dụng.
    """
    out, others, queue = [], [], []
    for cv in data:
        e = cv.get("extFields") or {}
        st = lbl(e.get("TRANG-THAI-XU-LY"))
        if st.startswith(DONE) or st.startswith(("1.1", "7.1")):
            continue
        td = cv.get("tieuDe") or ""
        intitle = bool(TAX.search(td))
        if not (intitle or TAX.search(re.sub(r"<[^>]+>", " ", str(e.get("MO_TA_GOC") or ""))[:900])):
            continue
        dev = e.get("DEVELOPER")
        row = {
            "ma": cv.get("ma") or "", "td": td, "est": cv.get("estimate") or 0, "st": st,
            "kh": (e.get("KHACH-HANG") or "").split("|")[0].strip(),
            "late": days_late(cv.get("ngayKetThucDuKien")),
            "mine": dev == me, "dev": "" if dev == me else (who.get(dev, "") if dev else "CHƯA GÁN"),
            "weak": not intitle,
        }
        if row["mine"]:
            out.append(row)
        elif st.startswith(DEVFLOW) and intitle:      # người khác đang cầm → gom, không liệt kê
            others.append(row)
        else:
            queue.append(row)
    out.sort(key=lambda x: -(x["late"] if x["late"] is not None else -999))
    byname = {}
    for t in others:
        byname[t["dev"] or "?"] = byname.get(t["dev"] or "?", 0) + 1
    return {"rows": out, "others": others, "byname": byname,
            "oh": sum(t["est"] for t in others),
            "ghost": [t["ma"] for t in others if t["dev"] == "CHƯA GÁN" and t["st"].startswith("9.")],
            "qn": len(queue), "qh": sum(t["est"] for t in queue),
            "qfree": sum(1 for t in queue if t["dev"] == "CHƯA GÁN")}


def fetch_tasks(data, me):
    out = []
    for cv in data:
        e = cv.get("extFields") or {}
        if e.get("DEVELOPER") != me:
            continue
        st = lbl(e.get("TRANG-THAI-XU-LY"))
        if st.startswith(DONE):
            continue
        out.append({
            "ma": cv.get("ma") or "",
            "td": cv.get("tieuDe") or "",
            "pl": lbl(e.get("PL-TASK")),
            "st": st,
            "uu": cv.get("tenMucDoUuTien") or "",
            "est": cv.get("estimate") or 0,
            "kh": (e.get("KHACH-HANG") or "").split("|")[0].strip(),
            "late": days_late(cv.get("ngayKetThucDuKien")),
        })
    return out


def read_alloc():
    """Sổ cấp phát folder → việc đang giữ. 'git sạch ≠ trống' nên sổ mới là nguồn sự thật."""
    info, extras = {}, []
    if not os.path.isfile(ALLOC):
        return info, extras
    for raw in open(ALLOC, encoding="utf-8"):
        line = raw.strip()
        if not line.startswith("|"):
            continue
        c = [x.strip() for x in line.strip("|").split("|")]
        if len(c) < 4 or set(c[0]) <= set("-: "):      # bỏ dòng kẻ bảng
            continue
        key = c[0].strip("`* ")
        m = re.match(r"(Group\d+)", key)
        if m:
            info[m.group(1)] = {"viec": c[2], "tt": c[3]}
            continue
        # bảng "folder NGOÀI hệ Group": bóc ĐÚNG path trong backtick, ô có thể kèm chú thích phía sau
        mp = re.search(r"`([A-Za-z]:[^`]+)`", c[0])
        path = mp.group(1).strip() if mp else (key if re.match(r"[A-Za-z]:[\\/]", key) else None)
        if path:
            extras.append({"path": path, "viec": c[2], "tt": c[3]})
    return info, extras


def dirty_detail(rp):
    """Bẩn thì bẩn CÁI GÌ: tách sửa/mới, 3 file vừa đụng gần nhất, chạm lần cuối bao lâu."""
    raw = git(rp, "status", "--porcelain")
    if raw is None:                                     # quét lỗi → KHÔNG được coi là sạch
        return {"err": True, "n": 0, "mod": 0, "new": 0, "names": [], "newest": 0}
    st = [x for x in raw.splitlines() if x.strip()]
    if not st:
        return None
    mod = sum(1 for x in st if not x.startswith("??"))
    stamped, newest = [], 0
    for x in st:
        p = x[3:].strip().strip('"')
        if " -> " in p:                                 # rename
            p = p.split(" -> ")[-1]
        fp = os.path.join(rp, p.replace("/", os.sep))
        try:
            t = os.path.getmtime(fp)
            if os.path.isdir(fp):                       # thư mục untracked → lấy file mới nhất bên trong
                seen = 0
                for root, _, fs in os.walk(fp):
                    for f in fs:
                        try:
                            t = max(t, os.path.getmtime(os.path.join(root, f)))
                        except OSError:
                            pass
                        seen += 1
                        if seen > 300:
                            break
                    if seen > 300:
                        break
        except OSError:
            continue
        stamped.append((t, os.path.basename(p.rstrip("/\\")) or p))
        newest = max(newest, t)
    stamped.sort(reverse=True)
    return {"n": len(st), "mod": mod, "new": len(st) - mod,
            "names": [nm for _, nm in stamped[:3]], "newest": newest}


def scan_repo(rp, name):
    b = git(rp, "rev-parse", "--abbrev-ref", "HEAD") or ""
    return {"repo": name, "br": b, "feat": bool(b) and b not in ("release_product", "product"),
            "d": dirty_detail(rp)}


def scan_groups(extras=None):
    """Quét SONG SONG — 7 nhóm × 5 repo = 35 lượt `git status`, chạy tuần tự thì mất vài phút."""
    jobs, rows = [], []                                  # jobs: (khóa nhóm, path, tên repo)
    if os.path.isdir(GROUPS_ROOT):
        for g in sorted(os.listdir(GROUPS_ROOT)):
            gp = os.path.join(GROUPS_ROOT, g)
            if not os.path.isdir(gp):
                continue
            for r in sorted(os.listdir(gp)):
                if os.path.isdir(os.path.join(gp, r, ".git")):
                    jobs.append((g, os.path.join(gp, r), r))
    for e in extras or []:                               # folder ngoài hệ Group
        p = e["path"].rstrip("\\/")
        base = os.path.basename(p)
        if os.path.isdir(os.path.join(p, ".git")):       # bản thân nó là 1 repo (vd ecommerce_connect)
            jobs.append(("\x00" + base, p, base))
        elif os.path.isdir(p):                           # là FOLDER CHỨA nhiều repo (vd _SourceForReview)
            for r in sorted(os.listdir(p)):
                if os.path.isdir(os.path.join(p, r, ".git")):
                    jobs.append(("\x00" + base, os.path.join(p, r), r))

    with cf.ThreadPoolExecutor(max_workers=6) as ex:
        done = list(ex.map(lambda j: (j[0], scan_repo(j[1], j[2])), jobs))

    order, bag = [], {}
    for key, rep in done:
        if key not in bag:
            bag[key] = []
            order.append(key)
        bag[key].append(rep)
    for key in order:
        ngoai = key.startswith("\x00")
        rows.append({"g": key.lstrip("\x00"), "repo": len(bag[key]), "reps": bag[key],
                     "warn_repo": (not ngoai) and len(bag[key]) < 5, "ngoai": ngoai})
    return rows


def read_notes():
    """Đọc file ghi tay. Mỗi '## TÊN Ô' là 1 khối, mỗi dòng '- ' là 1 mục."""
    if not os.path.isfile(NOTES):
        return {}
    sec, cur = {}, None
    for line in open(NOTES, encoding="utf-8"):
        line = line.rstrip()
        if line.startswith("## "):
            cur = line[3:].strip().upper()
            sec[cur] = []
        elif cur and line.strip().startswith("- "):
            sec[cur].append(line.strip()[2:])
    return sec


# ─────────────────────────── dựng HTML ───────────────────────────
def notes_panel(items):
    if not items:
        return '<div class="warn">Chưa có ghi chú. Thêm vào <code>memory/local/qlcv/dashboard-notes.md</code></div>'
    li = "".join(f"<li>{fmt_inline(x)}</li>" for x in items)
    return f"<ul>{li}</ul>"


def fmt_inline(s):
    """Cho phép **đậm**, `code`, và | ngăn cột thành dấu chấm giữa."""
    s = esc(s)
    s = re.sub(r"\*\*(.+?)\*\*", r"<b>\1</b>", s)
    s = re.sub(r"~~(.+?)~~", r'<s class="m">\1</s>', s)
    s = re.sub(r"`(.+?)`", r"<code>\1</code>", s)
    return s.replace(" | ", ' <span class="m">·</span> ')


def ago(ts):
    """Chạm lần cuối — để lộ WIP bị bỏ quên (ca Group2 nằm im 41 ngày)."""
    if not ts:
        return ""
    d = int((NOW - ts) // 86400)
    if d <= 0:
        return '<span class="g">chạm hôm nay</span>'
    if d == 1:
        return '<span class="g">chạm hôm qua</span>'
    if d <= 7:
        return f'<span class="m">chạm {d} ngày trước</span>'
    return f'<span class="r">IM {d} NGÀY</span>'


def group_cell(g, alloc):
    """Ô 'đang giữ': việc theo sổ + bằng chứng git (bẩn cái gì, đụng file nào, bao lâu rồi)."""
    a = alloc.get(g["g"]) or {}
    busy_git = any(r["d"] or r["feat"] for r in g["reps"])
    tt = a.get("tt", "")
    book_busy = bool(re.search(r"BẬN|GIỮ CHỖ|ĐANG CODE", tt))
    book_free = "TRỐNG" in tt

    parts = []
    if a.get("viec") and a["viec"] not in ("—", "-", ""):
        parts.append(f'<div class="viec">{fmt_inline(a["viec"])}</div>')
    elif not busy_git:
        parts.append('<div class="m">— chưa giao việc</div>')

    for r in g["reps"]:
        if not (r["d"] or r["feat"]):
            continue
        solo = g.get("ngoai") and len(g["reps"]) == 1     # folder ngoài dạng repo đơn → khỏi lặp tên
        head = "" if solo else f'<code>{esc(r["repo"])}</code>'
        if r["d"] and r["d"].get("err"):
            parts.append(f'<div class="sub a">{head} <b>⚠ QUÉT LỖI</b> — không kết luận được '
                         f'sạch hay bẩn, chạy lại bảng</div>')
            continue
        if r["feat"]:
            head += f'<span class="v">@{esc(r["br"])}</span>'
        elif r["d"]:                                    # bẩn ngay trên nhánh base = dễ mất trắng
            head += f'<span class="r">@{esc(r["br"])} ⚠ chưa có nhánh</span>'
        bits = []
        if r["d"]:
            d = r["d"]
            cnt = []
            if d["mod"]:
                cnt.append(f'{d["mod"]} sửa')
            if d["new"]:
                cnt.append(f'<span class="a">{d["new"]} file mới</span>')
            bits.append(" + ".join(cnt))
            bits.append(esc(", ".join(d["names"])) + ("…" if d["n"] > 3 else ""))
            bits.append(ago(d["newest"]))
        parts.append(f'<div class="sub">{head} <span class="m">·</span> '
                     + ' <span class="m">·</span> '.join(x for x in bits if x) + '</div>')

    if not busy_git and book_busy:
        parts.append('<div class="sub m">git sạch — <b>đang giữ chỗ</b></div>')
    if busy_git and book_free:
        parts.append('<div class="sub a">⚠ sổ ghi TRỐNG nhưng git đang bẩn — sổ lệch</div>')
    return "".join(parts) or '<span class="m">base sạch</span>', busy_git, book_busy, book_free


def rows_tax(lst):
    out = []
    for t in lst:
        out.append(
            f'<tr><td class="ma">{esc(t["ma"])}</td><td>{t["est"]}h</td>'
            f'<td>{late_cell(t["late"])}</td><td>{st_cell(t["st"])}</td>'
            f'<td>{esc(t["td"][:62])}<span class="m"> · {esc(t["kh"][:18])}</span></td></tr>')
    return "".join(out) or '<tr><td colspan="5" class="m">(không có)</td></tr>'


def report_block(items, who):
    """Ô BÁO CÁO NGÀY — dán thẳng vào sheet team.

    Nguồn: mục `## BÁO CÁO` trong dashboard-notes.md, mỗi dòng:
        <Mã> (<Khách>): <việc> — <phạm vi>, <mốc> | SB-Bucket | <%> | <Trạng thái>
    Cột: Ngày làm · Công việc · Mô tả · Người làm · Tiến độ · Trạng thái.
    (Cột "Link Lark" ĐÃ BỎ — thiennq không dùng Lark nữa, chốt 29/07/2026.)
    Luật đầy đủ ở memory/local/qlcv/report-format.md (TAB-separated, CẤM '|', chỉ việc CÓ MÃ TASK).
    """
    ngay = f"{TODAY.day}/{TODAY.month}/{TODAY.year}"     # d/m/yyyy, không zero-pad
    rows, tsv = [], []
    for it in items:
        c = [x.strip() for x in it.split("|")]
        if len(c) < 4:
            continue
        mota, bucket, tien, tt = c[0], c[1], c[2], c[3]
        cls = "g" if tt.startswith("Đã") else ("a" if tt.startswith("Đang") else "m")
        rows.append(f'<tr><td class="m">{ngay}</td><td><b>{esc(bucket)}</b></td>'
                    f'<td>{esc(mota)}</td><td class="m">{esc(who)}</td>'
                    f'<td><b>{esc(tien)}</b></td><td class="{cls}">{esc(tt)}</td></tr>')
        tsv.append("\t".join([ngay, bucket, mota, who, tien, tt]))
    if not rows:
        return ('<div class="warn">Chưa có dòng nào. Thêm mục <code>## BÁO CÁO</code> vào '
                '<code>dashboard-notes.md</code></div>', "")
    table = ('<table><tr><th>Ngày làm</th><th>Công việc</th><th>Mô tả chi tiết</th><th>Người làm</th>'
             '<th>Tiến độ</th><th>Trạng thái</th></tr>' + "".join(rows) + "</table>")
    return table, "\n".join(tsv)


def tax_foot(tax):
    """Phần KHÔNG phải việc mình — gom 2 dòng để vẫn thấy lưới an toàn mà không rối bảng."""
    out = []
    if tax["others"]:
        ai = " · ".join(f'{esc(k)} <b>{v}</b>' for k, v in
                        sorted(tax["byname"].items(), key=lambda x: -x[1]))
        g = (f' · 🔴 <b>{", ".join(tax["ghost"])}</b> đang "9. Đang dev" mà CHƯA GÁN ai'
             if tax["ghost"] else "")
        out.append(f'Người khác đang cầm <b>{len(tax["others"])}</b> việc thuế ({tax["oh"]}h): {ai}{g}')
    if tax["qn"]:
        out.append(f'Hàng chờ: <b>{tax["qn"]}</b> việc nữa ({tax["qh"]}h) ở BA / chờ duyệt — '
                   f'<b>{tax["qfree"]}</b> chưa gán ai.')
    return "<br>".join(out)


def build(tasks, groups, notes, alloc, tax, who):
    bugs = [t for t in tasks if t["pl"] == "Bug"]
    others = [t for t in tasks if t["pl"] != "Bug"]
    hb, ho = sum(t["est"] for t in bugs), sum(t["est"] for t in others)
    overdue = len([t for t in tasks if (t["late"] or 0) > 0])

    def rows_task(lst):
        out = []
        for t in sorted(lst, key=lambda x: -(x["late"] if x["late"] is not None else -999)):
            out.append(
                f'<tr><td class="ma">{esc(t["ma"])}</td><td>{t["est"]}h</td>'
                f'<td>{late_cell(t["late"])}</td><td>{st_cell(t["st"])}</td>'
                f'<td>{esc(t["td"][:46])}<span class="m"> · {esc(t["kh"][:16])}</span></td></tr>')
        return "".join(out) or '<tr><td colspan="5" class="m">(không có)</td></tr>'

    grows = ""
    for g in groups:
        cell, busy_git, book_busy, book_free = group_cell(g, alloc)
        if busy_git and book_free:
            st, cls = "lệch sổ", "a"
        elif busy_git or book_busy:
            st, cls = "BẬN", "r"
        else:
            st, cls = "trống", "g"
        warn = ' <span class="a">⚠ thiếu repo</span>' if g.get("warn_repo") else ""
        tag = ' <span class="v">ngoài Group</span>' if g.get("ngoai") else \
              f' <span class="m">{g["repo"]} repo</span>'
        grows += (f'<tr><td><b>{esc(g["g"])}</b>{tag}{warn}</td>'
                  f'<td>{cell}</td><td class="{cls}">{st}</td></tr>')

    rep_table, rep_tsv = report_block(notes.get("BÁO CÁO", []), who)
    tpl = open(os.path.join(HERE, "dashboard.tpl.html"), encoding="utf-8").read()
    return (tpl
            .replace("{{REPORT}}", rep_table)
            .replace("{{REPTSV}}", esc(rep_tsv))
            .replace("{{NREP}}", str(len(notes.get("BÁO CÁO", []))))
            .replace("{{NGAY}}", TODAY.strftime("%d/%m/%Y"))
            .replace("{{NBUG}}", str(len(bugs))).replace("{{HBUG}}", str(hb))
            .replace("{{NTASK}}", str(len(others))).replace("{{HTASK}}", str(ho))
            .replace("{{TONGH}}", str(hb + ho))
            .replace("{{NGAYCONG}}", str(round((hb + ho) / 8)))
            .replace("{{OVERDUE}}", str(overdue))
            .replace("{{DANGLAM}}", notes_panel(notes.get("ĐANG LÀM", [])))
            .replace("{{RUIRO}}", notes_panel(notes.get("RỦI RO", [])))
            .replace("{{CHOQUYET}}", notes_panel(notes.get("CHỜ QUYẾT", [])))
            .replace("{{TAXROWS}}", rows_tax(tax["rows"]))
            .replace("{{TAXQUEUE}}", tax_foot(tax))
            .replace("{{NTAX}}", str(len(tax["rows"])))
            .replace("{{HTAX}}", str(sum(t["est"] for t in tax["rows"])))
            .replace("{{NTAXMINE}}", str(sum(1 for t in tax["rows"] if t["mine"])))
            .replace("{{NTAXLATE}}", str(sum(1 for t in tax["rows"] if (t["late"] or 0) > 0)))
            .replace("{{BUGROWS}}", rows_task(bugs))
            .replace("{{TASKROWS}}", rows_task(others))
            .replace("{{GROUPROWS}}", grows or '<tr><td colspan="3" class="m">(không quét được)</td></tr>'))


def build_index():
    files = sorted([f for f in os.listdir(OUTDIR) if re.fullmatch(r"\d{4}-\d{2}-\d{2}\.html", f)],
                   reverse=True)
    rows = ""
    for f in files:
        d = f[:-5]
        meta = ""
        try:
            head = open(os.path.join(OUTDIR, f), encoding="utf-8").read(4000)
            m = re.search(r"<!--META (.+?)-->", head)
            if m:
                j = json.loads(m.group(1))
                meta = (f'<td>{j["nbug"]} · {j["hbug"]}h</td><td>{j["ntask"]} · {j["htask"]}h</td>'
                        f'<td><b>{j["tong"]}h</b></td><td class="r">{j["overdue"]}</td>')
        except Exception:
            pass
        if not meta:
            meta = '<td colspan="4" class="m">—</td>'
        dd = datetime.date.fromisoformat(d)
        thu = ["T2", "T3", "T4", "T5", "T6", "T7", "CN"][dd.weekday()]
        rows += f'<tr><td><a href="{f}">{thu} {dd.strftime("%d/%m/%Y")}</a></td>{meta}</tr>'
    html = f"""<!doctype html><html lang="vi"><head><meta charset="utf-8">
<title>Bảng điều khiển — theo ngày</title><style>
body{{font:14px/1.5 -apple-system,"Segoe UI",Roboto,sans-serif;background:#eef0f4;color:#15181d;padding:28px}}
@media(prefers-color-scheme:dark){{body{{background:#0d0f13;color:#e6e9ed}}table{{background:#161a20}}th{{background:#1c2129}}}}
h1{{font-size:19px;margin:0 0 16px}} table{{border-collapse:collapse;background:#fff;border-radius:8px;overflow:hidden;min-width:620px}}
th,td{{padding:8px 14px;border-bottom:1px solid #e3e6ea;text-align:left;font-size:13px}}
@media(prefers-color-scheme:dark){{th,td{{border-color:#262c35}}}}
th{{font-size:10.5px;text-transform:uppercase;letter-spacing:.04em;color:#6b7280}}
tr:last-child td{{border-bottom:none}} a{{color:#2563eb;font-weight:600;text-decoration:none}} a:hover{{text-decoration:underline}}
.r{{color:#dc2626;font-weight:700}} .m{{color:#6b7280}}
</style></head><body><h1>Bảng điều khiển — theo ngày</h1><table>
<tr><th>Ngày</th><th>Bug</th><th>Task</th><th>Tổng</th><th>Quá hạn</th></tr>{rows}</table></body></html>"""
    open(os.path.join(OUTDIR, "index.html"), "w", encoding="utf-8").write(html)


def main():
    os.makedirs(OUTDIR, exist_ok=True)
    print("· lấy việc từ qlcv…")
    me, data = W.me(), fetch_all()
    ros = roster()
    who = ros.get(me) or "(không rõ tên)"
    tasks = fetch_tasks(data, me)
    tax = fetch_tax(data, me, ros)
    print(f"  {len(tasks)} việc chưa xong · cụm thuế/HĐĐT: {len(tax['rows'])} trong luồng dev "
          f"(của tôi {sum(1 for t in tax['rows'] if t['mine'])}) + {tax['qn']} ở hàng chờ")
    alloc, extras = read_alloc()
    for e in extras:                                     # folder ngoài Group cũng tra sổ được
        alloc[os.path.basename(e["path"].rstrip("\\/"))] = {"viec": e["viec"], "tt": e["tt"]}
    print(f"· sổ cấp phát: {len(alloc)} group + {len(extras)} folder ngoài")
    print("· quét git các Group…")
    groups = scan_groups(extras)
    print(f"  {len(groups)} folder")
    notes = read_notes()
    print(f"· ghi chú tay: {'có' if notes else 'CHƯA CÓ (' + NOTES + ')'}")

    html = build(tasks, groups, notes, alloc, tax, who)
    bugs = [t for t in tasks if t["pl"] == "Bug"]
    others = [t for t in tasks if t["pl"] != "Bug"]
    meta = {"nbug": len(bugs), "hbug": sum(t["est"] for t in bugs),
            "ntask": len(others), "htask": sum(t["est"] for t in others),
            "tong": sum(t["est"] for t in tasks),
            "overdue": len([t for t in tasks if (t["late"] or 0) > 0])}
    html = html.replace("<!--METAMARK-->", "<!--META " + json.dumps(meta) + "-->")

    out = os.path.join(OUTDIR, TODAY.isoformat() + ".html")
    open(out, "w", encoding="utf-8").write(html)
    build_index()
    print()
    print("✅ Hôm nay :", out)
    print("📅 Theo ngày:", os.path.join(OUTDIR, "index.html"))


if __name__ == "__main__":
    main()
