#!/usr/bin/env node
/**
 * Claude Code Stop Hook → Token Budget Dashboard
 *
 * Đọc transcript JSONL của session hiện tại, lấy usage của assistant message
 * cuối cùng, POST lên AgentService để hiện trên dashboard.
 *
 * Config trong ~/.claude/settings.json:
 * {
 *   "hooks": {
 *     "Stop": [
 *       { "matcher": "", "hooks": [
 *         { "type": "command",
 *           "command": "node \"D:/PROJECT/Git/scrm_claude/AgentSystem/scripts/claude-code/usage-reporter.js\"",
 *           "timeout": 10 }
 *       ]}
 *     ]
 *   }
 * }
 *
 * Env vars (optional):
 *   AGENT_API_URL  - default http://10.0.161.21:50080
 *   GIT_USER_NAME  - override git user.name
 */

const fs = require('fs');
const { spawn, execSync } = require('child_process');
const path = require('path');

const API_URL = process.env.AGENT_API_URL || 'http://10.0.161.21:50080';

function exitSilent(code = 0) { process.exit(code); }

function getGitUser(cwd) {
  if (process.env.GIT_USER_NAME) return process.env.GIT_USER_NAME.trim();
  try {
    return execSync('git config user.name', { encoding: 'utf8', cwd }).trim();
  } catch {
    return process.env.USERNAME || process.env.USER || 'unknown';
  }
}

function readLastUsage(transcriptPath) {
  if (!fs.existsSync(transcriptPath)) return null;
  const content = fs.readFileSync(transcriptPath, 'utf8').trim();
  if (!content) return null;
  const lines = content.split('\n');
  // Walk from the end — find the most recent message that has usage info
  for (let i = lines.length - 1; i >= 0; i--) {
    try {
      const obj = JSON.parse(lines[i]);
      const usage = obj?.message?.usage || obj?.usage;
      if (usage && (usage.input_tokens || usage.output_tokens)) return usage;
    } catch { /* skip malformed line */ }
  }
  return null;
}

/**
 * Fire-and-forget: spawn detached child to POST data, parent exits ngay.
 * Hook return về Claude trong < 50ms, không chờ network.
 */
function fireAndForget(payload) {
  const child = spawn(
    process.execPath,
    [path.join(__dirname, 'usage-reporter-worker.js')],
    {
      detached: true,
      stdio: ['pipe', 'ignore', 'ignore'],
      windowsHide: true,
      env: { ...process.env, AGENT_API_URL: API_URL }
    }
  );
  child.unref();
  child.stdin.write(JSON.stringify(payload));
  child.stdin.end();
}

async function main() {
  let stdinData = '';
  for await (const chunk of process.stdin) stdinData += chunk;

  let hookInput;
  try { hookInput = JSON.parse(stdinData); } catch { return exitSilent(); }

  const transcriptPath = hookInput.transcript_path;
  const sessionId = hookInput.session_id;
  const cwd = hookInput.cwd;
  if (!transcriptPath || !sessionId) return exitSilent();

  const usage = readLastUsage(transcriptPath);
  if (!usage) return exitSilent();

  const inputTokens =
    (usage.input_tokens || 0) +
    (usage.cache_creation_input_tokens || 0) +
    (usage.cache_read_input_tokens || 0);
  const outputTokens = usage.output_tokens || 0;
  if (inputTokens === 0 && outputTokens === 0) return exitSilent();

  fireAndForget({
    gitUser: getGitUser(cwd),
    claudeSessionId: sessionId,
    inputTokens,
    outputTokens,
    cwd: cwd || ''
  });

  exitSilent();
}

main().catch(() => exitSilent());
