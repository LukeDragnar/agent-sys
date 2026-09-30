# Ripgrep Implementation Plan for AgentSystem

## 🎯 Goal
Replace CodeMap as primary search tool for AgentSystem development. Keep CodeMap for dependency analysis + testing.

---

## 📋 Current State
✅ Ripgrep v14.1.1 installed (PCRE2 support)
✅ `.ripgreprc` at AgentSystem root
✅ Per-project `.ripgreprc` for 6 projects
✅ Test results: Ripgrep wins in speed + pattern search

---

## 🏗️ Implementation Plan

### Phase 1: Git Setup (Week 1)

**1. Commit current configs**
```bash
git add .ripgreprc projects/*/.ripgreprc RIPGREP_GUIDE.md
git commit -m "feat: add ripgrep config for AgentSystem (primary search tool)"
git push origin feature/agent-embedding-and-scrm-docs
```

**2. Create RIPGREP.md in project root** (team quick-start)
```markdown
# Ripgrep — Code Search for AgentSystem

Quick start:
  rg --config-path .ripgreprc <pattern>

Examples:
  rg "TranslateSBService" --type ts
  rg --files -g "*translate*"
  rg "this\.tsb\.t\(" --type ts

See RIPGREP_GUIDE.md for 30+ examples.
```

**3. Update CLAUDE.md**
```
## Code Search Priority

1. **Ripgrep** (primary) — Fast local search, patterns, files
   bash: rg --config-path .ripgreprc <pattern>
   
2. **CodeMap** (secondary) — Dependencies, symbol graph
   bash: bash codemap.sh symbol <name>
   
3. **Grep** (fallback) — When ripgrep unavailable
```

---

### Phase 2: Team Onboarding (Week 2)

**1. Document ripgrep setup**
- ✅ RIPGREP_GUIDE.md (30+ examples)
- ✅ Per-project configs (already in git)
- ✅ RIPGREP.md (quick start)

**2. Create aliases (PowerShell/Bash)**

**PowerShell (.profile)**
```powershell
# Alias ripgrep with AgentSystem config
Set-Alias -Name rg-as -Value 'rg --config-path .ripgreprc'

# Example usage
# rg-as "TranslateSBService"
```

**Bash (.bashrc / .zshrc)**
```bash
alias rg-as='rg --config-path .ripgreprc'
```

**3. Update team docs**
- Add to README.md → "Code Search" section
- Link to RIPGREP_GUIDE.md
- Explain CodeMap + Ripgrep workflow

---

### Phase 3: Integration (Week 2-3)

**1. Update CLAUDE.md instructions**
```
When searching code:
- If need PATTERN search → rg --config-path .ripgreprc
- If need DEPENDENCIES → bash codemap.sh deps <symbol>
- If need FILE pattern → rg --files -g "<pattern>"
```

**2. Update agents to use Ripgrep**
- `review-agent.md` → use ripgrep for code inspection
- `trace-agent.md` → use ripgrep for data flow analysis
- Remove CodeMap dependency from agents

**3. Cache ripgrep results in memory**
```
memory/reference_ripgrep_patterns.md
- Common patterns for SCRM (this.tsb.t, NgonNguTranslate, etc.)
- Per-project examples
```

---

### Phase 4: Testing (Week 3)

**1. Test with team members**
- ✅ Each project's ripgrep config works
- ✅ Aliases set up correctly
- ✅ Fallback to CodeMap when needed

**2. Document issues**
- Track what breaks
- Create .ripgreprc improvements

**3. Optimize configs**
- Fine-tune per-project excludes
- Add project-specific patterns

---

## 📊 File Structure (Final)

```
AgentSystem/
├── .ripgreprc  ← Root config (fallback)
├── RIPGREP.md  ← Quick start (NEW)
├── RIPGREP_GUIDE.md  ← Full guide
├── IMPLEMENTATION_PLAN.md  ← This file
├── CLAUDE.md  ← Updated with ripgrep priority
│
├── projects/
│   ├── scrm/.ripgreprc
│   ├── ps_delivery/.ripgreprc
│   ├── ecommerce_connect/.ripgreprc
│   ├── delivery_connect/.ripgreprc
│   ├── delivery_receive/.ripgreprc
│   └── delivery_status/.ripgreprc
│
├── agents/
│   ├── review-agent.md  ← Use ripgrep
│   ├── trace-agent.md   ← Use ripgrep
│   └── ...
│
└── memory/
    ├── reference_ripgrep_patterns.md  ← (NEW)
    └── ...
```

---

## ✅ Checklist

### Before commit
- [ ] .ripgreprc works locally
- [ ] All 6 projects have config
- [ ] RIPGREP.md written
- [ ] RIPGREP_GUIDE.md complete
- [ ] Test cases verified

### Before push to team
- [ ] CLAUDE.md updated
- [ ] Memory reference created
- [ ] Aliases documented
- [ ] Fallback plan for CodeMap

### After merge
- [ ] Team trained on ripgrep
- [ ] Feedback collected
- [ ] Configs optimized
- [ ] Update wiki/docs

---

## 🔄 Ripgrep + CodeMap Workflow

**For code search:**
```
User: "Find all this.tsb.t() calls"
  → Use: rg "this\.tsb\.t\(" --type ts (fast)
  → Result: All matches + line numbers

User: "Find TranslateSBService dependencies"
  → Use: bash codemap.sh deps "TranslateSBService"
  → Result: Who calls it, who it calls
```

**For multi-lang wrapping:**
```
1. Ripgrep: Find files with Vietnamese text
   rg "[Ứ-ỳ]" src/app --type html -l
   
2. Ripgrep: Find class-level issues
   rg "^\s+\w+.*=.*this\.tsb\.t\(" --type ts
   
3. CodeMap: Verify impact (optional)
   bash codemap.sh deps "TranslateSBService"
```

---

## 📅 Timeline

| Week | Task |
|------|------|
| **This week** | Commit configs + docs |
| **Next week** | Onboard team, test |
| **Week 3** | Optimize, train |
| **Week 4** | Full team adoption |

---

## 💬 Team Communication

**Email to team:**
```
Subject: New Code Search Tool — Ripgrep (faster, local)

Hi team,

We've added Ripgrep as primary code search tool for AgentSystem.

✅ Benefits:
- 10-100x faster than CodeMap (local vs HTTP)
- 0 token cost
- Supports regex patterns
- File pattern search

📚 How to use:
  rg --config-path .ripgreprc <pattern>

Examples:
  rg "TranslateSBService" --type ts
  rg --files -g "*translate*"

See RIPGREP_GUIDE.md for 30+ examples.

CodeMap still available for dependency analysis.

Questions? See RIPGREP.md or RIPGREP_GUIDE.md.
```

---

## 🚀 Success Criteria

- ✅ Team can search code without CodeMap
- ✅ Ripgrep faster than CodeMap (verified)
- ✅ Per-project configs work
- ✅ Fallback to grep available
- ✅ Documentation clear + complete
