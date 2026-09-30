# Ripgrep Guide for AgentSystem

## Quick Start

```bash
# From AgentSystem root
cd d:/PROJECT/Git/scrm_claude/AgentSystem

# Search with config
rg --config-path .ripgreprc <pattern>

# Or set alias (PowerShell)
Set-Alias -Name rg-as -Value 'rg --config-path .ripgreprc'
rg-as <pattern>
```

---

## Use Cases by Project

### SCRM (FE + BE)

**Working Paths:**
```
BE: D:\PROJECT\Git\_SourceForBuild\scrm_build
FE: D:\PROJECT\Git\_SourceForBuild\build_scrm_frontend
```

#### Search in FE only
```bash
rg <pattern> D:/PROJECT/Git/_SourceForBuild/build_scrm_frontend/src --type ts
```

#### Search in BE only
```bash
rg <pattern> D:/PROJECT/Git/_SourceForBuild/scrm_build --type cs
```

#### Search in both
```bash
rg <pattern> D:/PROJECT/Git/_SourceForBuild/scrm_build D:/PROJECT/Git/_SourceForBuild/build_scrm_frontend
```

---

### Multi-lang Examples

#### 1. Find all TranslateSBService usages
```bash
rg "TranslateSBService|this\.tsb\.t\(" --type ts
```

#### 2. Find class-level property issues (443)
```bash
rg "^\s+\w+.*=.*this\.tsb\.t\(" --type ts -A 2
```

#### 3. Find Vietnamese strings in HTML (not wrapped)
```bash
rg "[Ứ-ỳ]" --type html -C 2
```

#### 4. Find all translation keys
```bash
rg "['\"]([\w-]+)['\"].*\|\s*tsb|this\.tsb\.t\(['\"]([\w-]+)['\"]" --type ts -o
```

#### 5. Find NgonNguTranslate references (BE)
```bash
rg "NgonNguTranslate" D:/PROJECT/Git/_SourceForBuild/scrm_build --type cs
```

---

## Advanced Patterns

### Find specific patterns

#### Find TODO comments
```bash
rg "TODO.*translate|TODO.*ngôn ngữ" --type ts --type cs
```

#### Find method definitions
```bash
rg "public.*Task.*\(.*\)" --type cs
```

#### Find class definitions with pattern
```bash
rg "class\s+(\w*Service)" --type cs -o
```

#### Find imports
```bash
rg "^import\s+.*from\s+['\"]" --type ts -A 1
```

---

## Type Filters

```bash
# TypeScript/JavaScript
rg <pattern> --type ts --type js

# C# (Backend)
rg <pattern> --type cs

# HTML/Templates
rg <pattern> --type html

# All text files
rg <pattern> --type txt
```

---

## Common Flags

```bash
# Show only file names (not content)
rg <pattern> -l

# Case sensitive
rg -s <pattern>

# Word boundaries
rg -w <pattern>

# Count matches
rg <pattern> --count

# Context lines
rg <pattern> -A 2 -B 2  # After/Before 2 lines

# Max results
rg <pattern> --max-count 50

# Regex only (no literal)
rg <pattern> --type ts -E
```

---

## Performance Tips

1. **Use type filter** — `--type ts` is faster than searching all files
2. **Exclude folders** — Already configured in `.ripgreprc`
3. **Use anchors** — `^pattern` or `pattern$` for faster matches
4. **PCRE2 support** — ripgrep v14.1.1 has PCRE2, use `(?:...)` for non-capturing groups

---

## Ripgrep vs CodeMap

| Task | CodeMap | Ripgrep |
|------|---------|---------|
| Find class by name | ✅⚡ | ✅⚡ |
| Find symbol dependencies | ✅ | ❌ |
| Find file pattern | ❌ | ✅⚡ |
| Find code pattern/regex | ❌ | ✅⚡ |
| Find comments | ❌ | ✅ |
| Real-time (code updates) | ❌ | ✅ |
| Token cost | ⚠️ (API) | ✅ (0) |
| Speed | ⚠️ (HTTP) | ✅⚡ (local C++) |

---

## Setup Check

```bash
# Verify ripgrep installed
rg --version

# Verify .ripgreprc exists
cat .ripgreprc

# Test search
rg "import" --type ts --max-count 5
```

**Status:** ✅ Ripgrep v14.1.1 ready with PCRE2 support
