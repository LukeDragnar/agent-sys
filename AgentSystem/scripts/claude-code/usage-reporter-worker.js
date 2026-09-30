#!/usr/bin/env node
/**
 * Detached worker — nhận payload qua stdin, POST lên API, exit.
 * Parent (usage-reporter.js) đã unref và return về Claude trước đó.
 */

const http = require('http');
const https = require('https');
const { URL } = require('url');

const API_URL = process.env.AGENT_API_URL || 'http://10.0.161.21:50080';
const TIMEOUT_MS = 10000;

(async () => {
  let stdinData = '';
  for await (const chunk of process.stdin) stdinData += chunk;
  let payload;
  try { payload = JSON.parse(stdinData); } catch { process.exit(0); }

  let url;
  try { url = new URL('/api/token-budget/cli-report', API_URL); } catch { process.exit(0); }

  const lib = url.protocol === 'https:' ? https : http;
  const body = JSON.stringify(payload);

  const req = lib.request({
    method: 'POST',
    hostname: url.hostname,
    port: url.port || (url.protocol === 'https:' ? 443 : 80),
    path: url.pathname,
    headers: {
      'Content-Type': 'application/json',
      'Content-Length': Buffer.byteLength(body)
    },
    timeout: TIMEOUT_MS
  }, res => {
    res.on('data', () => {});
    res.on('end', () => process.exit(0));
  });
  req.on('error', () => process.exit(0));
  req.on('timeout', () => { req.destroy(); process.exit(0); });
  req.write(body);
  req.end();
})();
