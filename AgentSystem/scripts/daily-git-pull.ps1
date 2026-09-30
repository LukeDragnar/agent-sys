# SessionStart hook: pull AgentSystem repo at most once per day.
# Last-pull date is tracked in memory/local/.last-git-pull (gitignored => per-machine).
# Safe by design: uses --ff-only, so a dirty working tree or missing upstream
# fails cleanly without creating merge commits or touching local changes.
$ErrorActionPreference = 'SilentlyContinue'

$stamp = 'memory/local/.last-git-pull'
$today = Get-Date -Format 'yyyy-MM-dd'

# Already pulled today? Stay silent (no per-session spam).
if ((Test-Path $stamp) -and (((Get-Content $stamp -Raw) -replace '\s', '') -eq $today)) {
    exit 0
}

# Ensure the per-machine tracking dir exists.
New-Item -ItemType Directory -Force -Path 'memory/local' | Out-Null

# Attempt a safe fast-forward-only pull of the current branch.
$out = (git pull --ff-only 2>&1 | Out-String)
$ok  = ($LASTEXITCODE -eq 0)

# Stamp today's date so we attempt at most once per day, regardless of outcome.
Set-Content -Path $stamp -Value $today -NoNewline

if ($ok) {
    if ($out -match 'up.to.date') {
        $msg = "AgentSystem: da la ban moi nhat ($today)."
    } else {
        $msg = "AgentSystem: da pull ban moi ($today). Co the can xem lai memory/agents/CLAUDE.md vua cap nhat."
    }
} else {
    $msg = "AgentSystem: KHONG pull duoc hom nay ($today) - thuong do local changes hoac branch chua co upstream. Chay 'git status' de kiem tra. Se thu lai ngay mai."
}

# SessionStart hooks read JSON from stdout; surface the result to the user.
(@{ systemMessage = $msg } | ConvertTo-Json -Compress)
exit 0
