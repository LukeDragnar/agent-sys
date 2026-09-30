# Team Quick Start — Ripgrep + AgentSystem

**Tài liệu này cho team members khi pull git mới và bắt đầu work với task mới.**

---

## 📥 Step 1: Pull Git (5 phút)

```bash
cd d:/PROJECT/Git/scrm_claude/AgentSystem

# Pull latest
git pull origin feature/agent-embedding-and-scrm-docs

# Verify ripgrep configs exist
ls .ripgreprc
ls projects/scrm/.ripgreprc
```

---

## ✅ Step 2: Verify Setup (2 phút)

```bash
# Check ripgrep installed
rg --version
# Output: ripgrep 14.1.1 (PCRE2 support)

# Test ripgrep config
rg --config-path .ripgreprc "class.*Service" --type ts --max-count 5
# Should return: search results
```

---

## 🎯 Step 3: Example Task — "Tìm tất cả TranslateSBService usage"

### OLD WAY (CodeMap):
```bash
# Problem: CodeMap API need to be live
bash codemap.sh search "TranslateSBService"
# ⚠️ Slow (HTTP request, 0.8+ seconds)
# ⚠️ Tốn token (API call)
# ⚠️ Chỉ tìm symbol, không pattern
```

### NEW WAY (Ripgrep):
```bash
# Step 1: Search files using TranslateSBService
rg --config-path .ripgreprc "TranslateSBService" --type ts -l

# Step 2: Find specific usage (this.tsb.t call)
rg --config-path .ripgreprc "this\.tsb\.t\(" --type ts -A 2

# Step 3: Find class-level issues (property initialization)
rg --config-path .ripgreprc "^\s+\w+.*=.*this\.tsb\.t\(" --type ts -C 2

# ✅ Fast (0.2 seconds, local)
# ✅ No token cost
# ✅ Regex patterns support
# ✅ Real-time (no scan delay)
```

**Result:**
- OLD: 1 search, 0.8s, limited info, token cost
- NEW: 3 searches, 0.2s each, detailed patterns, 0 token cost

---

## 🏗️ Step 4: Workflow — Với Task Mới

### Task: "Fix multi-lang issue trong search-and-filter component"

**Phase 1: SEARCH & EXPLORE (Ripgrep)**

```bash
# 1. Find the component file
rg --config-path .ripgreprc --files -g "*search-and-filter*"
# Result: src/app/components/button/search-and-filter/

# 2. Find Vietnamese text in component (not wrapped)
rg --config-path .ripgreprc "[Ứ-ỳ]" src/app/components/button/search-and-filter --type html
# Result: Lines with Vietnamese not wrapped

# 3. Find this.tsb usage in component
rg --config-path .ripgreprc "this\.tsb\.t\(" src/app/components/button/search-and-filter --type ts -A 1
# Result: Where translations used

# 4. Find similar components (reference)
rg --config-path .ripgreprc "export class.*Component" src/app/components/button --type ts -l
# Result: Similar files to reference pattern
```

**Phase 2: UNDERSTAND STRUCTURE (CodeMap - optional)**

```bash
# If need dependency understanding:
bash codemap.sh deps "TranslateSBService"
# Result: What depends on TranslateSBService
```

**Phase 3: IMPLEMENT FIX (Edit code)**

```bash
# Edit files based on search results
# Wrap Vietnamese text with tsb pipe
# Add this.tsb.t() calls for dynamic text
```

**Phase 4: VERIFY (Ripgrep again)**

```bash
# Make sure all Vietnamese wrapped
rg --config-path .ripgreprc "[Ứ-ỳ]" src/app/components/button/search-and-filter --type html
# Result: Should be empty (no unwrapped Vietnamese)

# Make sure all class-level issues fixed
rg --config-path .ripgreprc "^\s+\w+.*=.*this\.tsb\.t\(" src/app/components/button/search-and-filter --type ts
# Result: Should be empty (no class-level init issues)
```

---

## 🎨 Setup Alias (Optional - 1 phút)

**PowerShell (.profile):**
```powershell
# Add to C:\Users\<username>\Documents\PowerShell\profile.ps1
Set-Alias -Name rg-as -Value 'rg --config-path .ripgreprc'

# Reload profile
. $PROFILE

# Usage:
rg-as "TranslateSBService" --type ts
```

**Bash (.bashrc / .zshrc):**
```bash
# Add to ~/.bashrc or ~/.zshrc
alias rg-as='rg --config-path .ripgreprc'

# Reload:
source ~/.bashrc

# Usage:
rg-as "pattern" --type ts
```

---

## 📚 When to Use Which Tool

### Use RIPGREP when:
- ✅ Need file pattern search: `rg --files -g "*pattern*"`
- ✅ Need code pattern/regex: `rg "pattern" --type ts`
- ✅ Need to find comments: `rg "TODO.*translate"`
- ✅ Need fast local search (no wait for CodeMap)
- ✅ Need real-time results (code just changed)

### Use CODEMAP when:
- ✅ Need dependency graph: `bash codemap.sh deps "OrderService"`
- ✅ Need to understand symbol relationships
- ✅ Need endpoint discovery: `bash codemap.sh endpoint "order"`
- ✅ Need to find who calls this function

### Use GREP when:
- ✅ Ripgrep not installed (very rare)
- ✅ Need super simple text search

---

## 🔍 Common Search Patterns (Copy-Paste)

```bash
# SCRM Multi-lang
rg --config-path .ripgreprc "this\.tsb\.t\(" --type ts
rg --config-path .ripgreprc "[Ứ-ỳ]" --type html
rg --config-path .ripgreprc "^\s+\w+.*=.*this\.tsb\.t\(" --type ts

# Find components
rg --config-path .ripgreprc "export class.*Component" src/app --type ts -l

# Find services
rg --config-path .ripgreprc "export class.*Service" --type ts -l

# Find API endpoints (C#)
rg --config-path .ripgreprc "\[HttpPost\]|\[HttpGet\]" --type cs

# Find TODO/FIXME
rg --config-path .ripgreprc "TODO|FIXME|HACK" --type ts --type cs
```

---

## ❓ Troubleshooting

**Q: "rg: command not found"**
```bash
# Install ripgrep
choco install ripgrep  # Windows
brew install ripgrep   # macOS
apt-get install ripgrep  # Linux
```

**Q: "Can't find .ripgreprc"**
```bash
# Make sure in AgentSystem root
pwd
# Should show: d:/PROJECT/Git/scrm_claude/AgentSystem

# Verify file exists
ls -la .ripgreprc
```

**Q: "Pattern not matching?"**
```bash
# Use PCRE2 regex (more features)
# \d = digit, \s = space, \w = word
# (?:...) = non-capturing group
# [Ứ-ỳ] = Vietnamese characters

# Test online: https://regex101.com (select PCRE2)
```

**Q: "Ripgrep slow?"**
```bash
# Add --type filter (much faster)
rg "pattern" --type ts  # Only TypeScript

# Use anchors
rg "^pattern$"  # More specific = faster
```

---

## 🚀 First Task Checklist

- [ ] Git pulled successfully
- [ ] Ripgrep verified (`rg --version`)
- [ ] Can run `rg --config-path .ripgreprc` command
- [ ] Read `RIPGREP.md` (quick start)
- [ ] Tried at least 1 search pattern
- [ ] Optional: Created alias for convenience
- [ ] Ready to start new task!

---

## 📖 Documentation Links

- **Quick Start:** `RIPGREP.md` (2 min read)
- **Full Guide:** `RIPGREP_GUIDE.md` (30+ examples)
- **Implementation Plan:** `IMPLEMENTATION_PLAN.md` (team rollout)
- **Project Instructions:** `CLAUDE.md` (search priority)

---

**Questions?** Check RIPGREP.md or RIPGREP_GUIDE.md. Happy coding! 🎉
