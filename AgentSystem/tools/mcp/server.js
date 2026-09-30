import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { StdioServerTransport } from "@modelcontextprotocol/sdk/server/stdio.js";
import { z } from "zod";

// ── Config ────────────────────────────────────────────────────────────────────
const API_URL  = process.env.AGENT_API_URL  ?? "http://localhost:5137";
const USERNAME = process.env.AGENT_USERNAME ?? "admin";
const PASSWORD = process.env.AGENT_PASSWORD ?? "";

// ── Auth ─────────────────────────────────────────────────────────────────────
let _token = null;

async function getToken() {
  if (_token) return _token;
  const res = await fetch(`${API_URL}/api/auth/login`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ username: USERNAME, password: PASSWORD }),
  });
  if (!res.ok) throw new Error(`Login failed: ${res.status}`);
  const data = await res.json();
  _token = data.token;
  return _token;
}

async function apiFetch(path, options = {}) {
  const token = await getToken();
  const res = await fetch(`${API_URL}${path}`, {
    ...options,
    headers: {
      "Content-Type": "application/json",
      Authorization: `Bearer ${token}`,
      ...options.headers,
    },
  });

  // Token hết hạn → re-login 1 lần
  if (res.status === 401) {
    _token = null;
    const token2 = await getToken();
    const res2 = await fetch(`${API_URL}${path}`, {
      ...options,
      headers: {
        "Content-Type": "application/json",
        Authorization: `Bearer ${token2}`,
        ...options.headers,
      },
    });
    if (!res2.ok) throw new Error(`API error ${res2.status}: ${await res2.text()}`);
    return res2.json();
  }

  if (!res.ok) throw new Error(`API error ${res.status}: ${await res.text()}`);
  return res.json();
}

// ── MCP Server ────────────────────────────────────────────────────────────────
const server = new McpServer({
  name: "agent-system",
  version: "1.0.0",
});

// Tạo session mới
server.tool(
  "create_session",
  "Tạo session mới để lưu conversation và spec. Gọi khi bắt đầu task mới.",
  { title: z.string().describe("Tiêu đề session, ví dụ: 'fix bug NCC/KH 202603'") },
  async ({ title }) => {
    const session = await apiFetch("/api/agent/session", {
      method: "POST",
      body: JSON.stringify(title),
    });
    return {
      content: [{ type: "text", text: `Session created: ${session.id}\nTitle: ${session.title}` }],
    };
  }
);

// Chat (lưu message + spec tự động)
server.tool(
  "chat",
  "Gửi message vào session và lưu vào web app. Response sẽ được lưu vào history.",
  {
    sessionId: z.string().uuid().describe("ID của session"),
    message: z.string().describe("Nội dung message"),
    agentType: z.string().optional().describe("Tên agent: code-agent, git-agent, review-agent. Null = BA agent mặc định"),
    projectKey: z.string().optional().describe("Tên project: scrm, ps_delivery, ... Dùng để load context"),
  },
  async ({ sessionId, message, agentType, projectKey }) => {
    const response = await apiFetch("/api/agent/chat", {
      method: "POST",
      body: JSON.stringify({ sessionId, message, agentType, projectKey }),
    });
    return {
      content: [{ type: "text", text: response }],
    };
  }
);

// Danh sách sessions
server.tool(
  "list_sessions",
  "Lấy danh sách sessions của user hiện tại.",
  {},
  async () => {
    const sessions = await apiFetch("/api/agent/sessions");
    const lines = sessions.map(s =>
      `[${s.id}] ${s.title} — ${new Date(s.createdAt).toLocaleDateString("vi-VN")}`
    );
    return {
      content: [{ type: "text", text: lines.join("\n") || "Không có session nào." }],
    };
  }
);

// History của session
server.tool(
  "get_history",
  "Xem lịch sử conversation của một session.",
  { sessionId: z.string().uuid().describe("ID của session") },
  async ({ sessionId }) => {
    const messages = await apiFetch(`/api/agent/history/${sessionId}`);
    const lines = messages.map(m =>
      `[${m.role.toUpperCase()}] ${m.content.slice(0, 200)}${m.content.length > 200 ? "..." : ""}`
    );
    return {
      content: [{ type: "text", text: lines.join("\n\n") || "Session trống." }],
    };
  }
);

// Xem specs của session
server.tool(
  "get_specs",
  "Xem danh sách specs đã được tạo trong session.",
  { sessionId: z.string().uuid().describe("ID của session") },
  async ({ sessionId }) => {
    const specs = await apiFetch(`/api/agent/spec/${sessionId}/versions`);
    if (!specs.length) return { content: [{ type: "text", text: "Chưa có spec nào." }] };

    const lines = specs.flatMap(s => [
      `## ${s.name}`,
      ...s.tasks.flatMap(t => [
        `  Task: ${t.name}`,
        ...t.versions.map(v => `    v${v.version} — ${new Date(v.createdAt).toLocaleDateString("vi-VN")}`),
      ]),
    ]);
    return {
      content: [{ type: "text", text: lines.join("\n") }],
    };
  }
);

// ── CodeMap Tools ─────────────────────────────────────────────────────────────

server.tool(
  "find_symbol",
  "Tìm class, interface, method, property trong source code đã scan. Dùng khi cần biết file/line/dependencies của một symbol.",
  {
    projectId: z.string().uuid().describe("ID của project đã scan"),
    name: z.string().describe("Tên symbol cần tìm, ví dụ: 'OrderService', 'CreateOrder'"),
    type: z.string().optional().describe("Lọc theo loại: Class, Interface, Method, Property, Enum"),
  },
  async ({ projectId, name, type }) => {
    const params = new URLSearchParams({ name });
    if (type) params.set("type", type);
    const symbols = await apiFetch(`/api/codemap/${projectId}/symbols?${params}`);
    if (!symbols.length) return { content: [{ type: "text", text: `Không tìm thấy symbol "${name}".` }] };

    const lines = symbols.map(s => {
      let info = `## ${s.symbolType}: ${s.fullName}\nFile: ${s.filePath}:${s.lineNumber}\nAccess: ${s.accessibility}`;
      if (s.baseTypes) info += `\nExtends: ${s.baseTypes}`;
      if (s.children?.length) {
        info += `\n\nMembers:`;
        for (const c of s.children) {
          info += `\n  ${c.symbolType} ${c.returnType || ""} ${c.name}(${c.parameters || ""}) — line ${c.lineNumber}`;
        }
      }
      if (s.dependencies?.length) {
        info += `\n\nDependencies:`;
        for (const d of s.dependencies) {
          info += `\n  ${d.dependencyType}: ${d.toSymbol}`;
        }
      }
      return info;
    });
    return { content: [{ type: "text", text: lines.join("\n\n---\n\n") }] };
  }
);

server.tool(
  "find_endpoint",
  "Tìm API endpoint theo route hoặc HTTP method. Dùng khi cần biết controller/action xử lý một API.",
  {
    projectId: z.string().uuid().describe("ID của project đã scan"),
    route: z.string().optional().describe("Route pattern, ví dụ: 'order', 'api/workflow'"),
    method: z.string().optional().describe("HTTP method: GET, POST, PUT, DELETE"),
  },
  async ({ projectId, route, method }) => {
    const params = new URLSearchParams();
    if (route) params.set("route", route);
    if (method) params.set("method", method);
    const endpoints = await apiFetch(`/api/codemap/${projectId}/endpoints?${params}`);
    if (!endpoints.length) return { content: [{ type: "text", text: `Không tìm thấy endpoint.` }] };

    const lines = endpoints.map(e => {
      let info = `${e.httpMethod} /${e.routeTemplate}\n  Controller: ${e.controllerName}.${e.actionName}\n  File: ${e.filePath}:${e.lineNumber}`;
      if (e.serviceChain?.length) info += `\n  Chain: ${e.serviceChain.join(" → ")}`;
      return info;
    });
    return { content: [{ type: "text", text: lines.join("\n\n") }] };
  }
);

server.tool(
  "get_dependencies",
  "Lấy dependency tree: class này inject gì, và ai inject class này. Dùng để hiểu quan hệ giữa các service.",
  {
    projectId: z.string().uuid().describe("ID của project đã scan"),
    symbol: z.string().describe("Tên class/interface, ví dụ: 'OrderService'"),
    depth: z.number().optional().describe("Độ sâu tìm kiếm (mặc định 1)"),
  },
  async ({ projectId, symbol, depth }) => {
    const params = new URLSearchParams({ symbol });
    if (depth) params.set("depth", String(depth));
    const tree = await apiFetch(`/api/codemap/${projectId}/dependencies?${params}`);

    let text = `## Dependencies of ${tree.symbol}\n`;
    if (tree.dependsOn?.length) {
      text += `\nDepends on:`;
      for (const d of tree.dependsOn) text += `\n  ${d.dependencyType}: ${d.toSymbol}`;
    }
    if (tree.usedBy?.length) {
      text += `\n\nUsed by:`;
      for (const d of tree.usedBy) text += `\n  ${d.fromSymbol} (${d.dependencyType})`;
    }
    if (!tree.dependsOn?.length && !tree.usedBy?.length) text += `\nKhông có dependency nào.`;
    return { content: [{ type: "text", text }] };
  }
);

server.tool(
  "search_code",
  "Full-text search symbols trong source code đã scan. Tìm class/method/property theo keyword.",
  {
    projectId: z.string().uuid().describe("ID của project đã scan"),
    query: z.string().describe("Từ khóa tìm kiếm"),
    limit: z.number().optional().describe("Số kết quả tối đa (mặc định 20)"),
  },
  async ({ projectId, query, limit }) => {
    const params = new URLSearchParams({ q: query });
    if (limit) params.set("limit", String(limit));
    const results = await apiFetch(`/api/codemap/${projectId}/search?${params}`);
    if (!results.length) return { content: [{ type: "text", text: `Không tìm thấy "${query}".` }] };

    const lines = results.map(r =>
      `${r.symbolType} ${r.fullName} — ${r.filePath}:${r.lineNumber}`
    );
    return { content: [{ type: "text", text: lines.join("\n") }] };
  }
);

server.tool(
  "scan_codemap",
  "Quét source code project bằng Roslyn để build/rebuild source map. Chạy khi cần cập nhật index.",
  {
    projectId: z.string().uuid().describe("ID của project"),
    rootPath: z.string().describe("Đường dẫn root source code, ví dụ: 'D:\\\\PROJECT\\\\Git\\\\scrm'"),
  },
  async ({ projectId, rootPath }) => {
    const stats = await apiFetch(`/api/codemap/${projectId}/scan`, {
      method: "POST",
      body: JSON.stringify({ rootPath }),
    });
    return {
      content: [{
        type: "text",
        text: `Scan hoàn tất!\n` +
          `Files: ${stats.totalFiles}\n` +
          `Classes: ${stats.classes} | Interfaces: ${stats.interfaces}\n` +
          `Methods: ${stats.methods} | Properties: ${stats.properties}\n` +
          `Endpoints: ${stats.endpoints} | Dependencies: ${stats.dependencies}`,
      }],
    };
  }
);

// ── Start ─────────────────────────────────────────────────────────────────────
const transport = new StdioServerTransport();
await server.connect(transport);
