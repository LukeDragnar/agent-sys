# Ripgrep — Code Search for AgentSystem

**Ripgrep (rg)** is AgentSystem's primary code search tool. It's **fast, local, and free** (no token cost).

---

## ⚡ Quick Start (30 seconds)

```bash
# From AgentSystem root
rg --config-path .ripgreprc <pattern>

# Examples
rg --config-path .ripgreprc "TranslateSBService"
rg --config-path .ripgreprc --files -g "*translate*"
rg --config-path .ripgreprc "this\.tsb\.t\(" --type ts
```

---

## 🔧 Setup (one-time)

### 1. Install Ripgrep
```bash
# Windows
choco install ripgrep

# macOS
brew install ripgrep

# Linux
apt-get install ripgrep
```

### 2. Verify installation
```bash
rg --version
# Output: ripgrep 14.1.1 (PCRE2 support)
```

### 3. (Optional) Create alias
```powershell
# PowerShell
Set-Alias -Name rg-as -Value 'rg --config-path .ripgreprc'

# Then use: rg-as "pattern"
```

---

## 📚 Common Use Cases

### 1. Find class/service
```bash
rg --config-path .ripgreprc "class TranslateSBService"
rg --config-path .ripgreprc "export class.*Service" --type ts
```

### 2. Find files
```bash
# Files matching pattern
rg --config-path .ripgreprc --files -g "*translate*"

# Files containing pattern
rg --config-path .ripgreprc "import.*NgonNguTranslate" -l
```

### 3. Find code patterns
```bash
# All this.tsb.t() calls
rg --config-path .ripgreprc "this\.tsb\.t\(" --type ts

# Class-level property issues
rg --config-path .ripgreprc "^\s+\w+.*=.*this\.tsb\.t\(" --type ts -A 2
```

### 4. Find comments
```bash
rg --config-path .ripgreprc "TODO.*translate" --type ts
rg --config-path .ripgreprc "//.*ngôn ngữ" --type ts
```

### 5. Search in specific project
```bash
# SCRM project
cd projects/scrm
rg --config-path .ripgreprc <pattern>

# Or from root
rg --config-path .ripgreprc <pattern> D:/PROJECT/Git/_SourceForBuild/scrm_build
```

---

## 🎯 Ripgrep vs CodeMap

| Need | Tool | Command |
|------|------|---------|
| Find class by name | Ripgrep | `rg "class OrderService"` |
| Find method usage | Ripgrep | `rg "\.methodName\("` |
| Find file pattern | **Ripgrep** | `rg --files -g "*translate*"` |
| Find regex pattern | **Ripgrep** | `rg "^\s+\w+.*=.*this\.tsb"` |
| Find dependencies | **CodeMap** | `bash codemap.sh deps "OrderService"` |
| Find symbol graph | **CodeMap** | `bash codemap.sh symbol "Order"` |

---

## 🚀 Advanced Usage

### Show context
```bash
rg --config-path .ripgreprc "pattern" -A 3 -B 2
# -A 3: show 3 lines after match
# -B 2: show 2 lines before match
```

### Case sensitivity
```bash
rg --config-path .ripgreprc "PATTERN" -s  # Case sensitive
rg --config-path .ripgreprc "pattern" -i  # Case insensitive
```

### Count matches
```bash
rg --config-path .ripgreprc "pattern" --count
```

### Max results
```bash
rg --config-path .ripgreprc "pattern" --max-count 50
```

### Output formats
```bash
# File names only
rg --config-path .ripgreprc "pattern" -l

# Show file+line+content
rg --config-path .ripgreprc "pattern"  # Default

# JSON output
rg --config-path .ripgreprc "pattern" --json
```

---

## ⚙️ Type Filters

```bash
# TypeScript files
rg <pattern> --type ts

# C# files (Backend)
rg <pattern> --type cs

# HTML files
rg <pattern> --type html

# Multiple types
rg <pattern> --type ts --type html

# List all supported types
rg --type-list | grep ts
```

---

## 🔍 Examples for SCRM

```bash
# Find all TranslateSBService usages
rg --config-path .ripgreprc "TranslateSBService" --type ts

# Find class-level translate issues
rg --config-path .ripgreprc "^\s+\w+.*=.*this\.tsb\.t\(" --type ts

# Find NgonNguTranslate in backend
rg --config-path .ripgreprc "NgonNguTranslate" --type cs

# Find missing đa ngôn ngữ wrapping
rg --config-path .ripgreprc "[Ứ-ỳ]" --type html

# Find all translation keys
rg --config-path .ripgreprc "'([\w-]+)'\s*\|?\s*tsb" --type ts -o
```

---

## ❓ FAQ

**Q: Ripgrep not found?**
```bash
# Install it
choco install ripgrep  # Windows
brew install ripgrep   # macOS
```

**Q: .ripgreprc not found?**
```bash
# Make sure you're in AgentSystem root
cd d:/PROJECT/Git/scrm_claude/AgentSystem
ls .ripgreprc  # Should exist
```

**Q: Pattern not matching?**
```bash
# PCRE2 regex (more powerful than grep)
# Use: \d \s \w (matches digit, space, word)
# Use: (?:...) for non-capturing groups
# Test online: https://regex101.com/
```

**Q: Performance slow?**
```bash
# Add type filter
rg "pattern" --type ts  # Faster than searching all files

# Use anchors
rg "^pattern$"  # More specific = faster
```

**Q: Want to use CodeMap instead?**
```bash
# Still available for dependencies
bash codemap.sh deps "OrderService"
bash codemap.sh symbol "NgonNguTranslate"
```

---

## 📖 More Help

- **Full guide:** See `RIPGREP_GUIDE.md` (30+ examples)
- **Implementation plan:** See `IMPLEMENTATION_PLAN.md`
- **Ripgrep docs:** https://github.com/BurntSushi/ripgrep

---

**Happy searching! 🚀**
