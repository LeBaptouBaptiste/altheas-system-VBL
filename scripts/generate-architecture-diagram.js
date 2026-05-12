// Generates docs/architecture.excalidraw — drag-drop into excalidraw.com.
// Layout: vertical layered architecture, client → frontend → API → data,
// with side annotations for security mechanisms and infra.

const crypto = require('crypto');
const fs = require('fs');
const path = require('path');

const nonce = () => Math.floor(Math.random() * 2 ** 31);
const id = () => crypto.randomBytes(11).toString('base64').replace(/[+/=]/g, '').slice(0, 16);

const elements = [];

const C = {
  text: '#1e1e1e',
  client: '#a5d8ff',
  frontend: '#b2f2bb',
  api: '#ffd8a8',
  data: '#eebefa',
  security: '#c92a2a',
  infra: '#2b8a3e',
  arrow: '#1e1e1e',
  arrowLabel: '#1971c2',
  dataArrow: '#7048e8',
};

function rect(x, y, w, h, color, label, opts = {}) {
  const rid = id();
  const tid = id();
  const fontSize = opts.fontSize || 16;
  const lines = label.split('\n');
  const textHeight = lines.length * fontSize * 1.25;

  elements.push({
    type: 'rectangle',
    version: 1,
    versionNonce: nonce(),
    isDeleted: false,
    id: rid,
    fillStyle: 'solid',
    strokeWidth: 2,
    strokeStyle: 'solid',
    roughness: 1,
    opacity: 100,
    angle: 0,
    x, y, width: w, height: h,
    strokeColor: C.text,
    backgroundColor: color,
    groupIds: [],
    frameId: null,
    roundness: { type: 3 },
    seed: nonce(),
    boundElements: [{ type: 'text', id: tid }],
    updated: Date.now(),
    link: null,
    locked: false,
  });

  elements.push({
    type: 'text',
    version: 1,
    versionNonce: nonce(),
    isDeleted: false,
    id: tid,
    fillStyle: 'solid',
    strokeWidth: 2,
    strokeStyle: 'solid',
    roughness: 1,
    opacity: 100,
    angle: 0,
    x: x + 10, y: y + (h - textHeight) / 2,
    width: w - 20, height: textHeight,
    strokeColor: C.text,
    backgroundColor: 'transparent',
    groupIds: [],
    frameId: null,
    roundness: null,
    seed: nonce(),
    boundElements: null,
    updated: Date.now(),
    link: null,
    locked: false,
    text: label,
    fontSize,
    fontFamily: 1,
    textAlign: 'center',
    verticalAlign: 'middle',
    containerId: rid,
    originalText: label,
    lineHeight: 1.25,
    baseline: Math.round(fontSize * 0.8),
  });

  return { rid, x, y, w, h };
}

function connect(from, to, label, opts = {}) {
  const startX = from.x + from.w / 2;
  const startY = from.y + from.h;
  const endX = to.x + to.w / 2;
  const endY = to.y;
  const aid = id();

  elements.push({
    type: 'arrow',
    version: 1, versionNonce: nonce(), isDeleted: false, id: aid,
    fillStyle: 'solid',
    strokeWidth: opts.thin ? 1.5 : 2,
    strokeStyle: opts.dashed ? 'dashed' : 'solid',
    roughness: 1, opacity: 100, angle: 0,
    x: startX, y: startY,
    width: endX - startX, height: endY - startY,
    strokeColor: opts.color || C.arrow,
    backgroundColor: 'transparent',
    groupIds: [], frameId: null, roundness: { type: 2 }, seed: nonce(),
    boundElements: [], updated: Date.now(), link: null, locked: false,
    startBinding: { elementId: from.rid, focus: 0, gap: 1 },
    endBinding: { elementId: to.rid, focus: 0, gap: 1 },
    lastCommittedPoint: null,
    startArrowhead: null,
    endArrowhead: 'arrow',
    points: [[0, 0], [endX - startX, endY - startY]],
  });

  if (label) {
    // Place label OFFSET to the right of the arrow midpoint, so it doesn't
    // sit on top of the line.
    const midX = (startX + endX) / 2;
    const midY = (startY + endY) / 2;
    const labelLines = label.split('\n');
    elements.push({
      type: 'text',
      version: 1, versionNonce: nonce(), isDeleted: false, id: id(),
      fillStyle: 'solid', strokeWidth: 1, strokeStyle: 'solid',
      roughness: 1, opacity: 100, angle: 0,
      x: midX + 12, y: midY - (labelLines.length * 14 * 1.25) / 2,
      width: 200, height: labelLines.length * 14 * 1.25,
      strokeColor: opts.color || C.arrowLabel,
      backgroundColor: 'transparent',
      groupIds: [], frameId: null, roundness: null, seed: nonce(),
      boundElements: null, updated: Date.now(), link: null, locked: false,
      text: label, fontSize: 13, fontFamily: 1,
      textAlign: 'left', verticalAlign: 'middle',
      containerId: null, originalText: label, lineHeight: 1.25, baseline: 11,
    });
  }
}

function panel(x, y, w, label, color, fontSize = 13) {
  const lines = label.split('\n');
  const h = lines.length * fontSize * 1.45;
  elements.push({
    type: 'text',
    version: 1, versionNonce: nonce(), isDeleted: false, id: id(),
    fillStyle: 'solid', strokeWidth: 1, strokeStyle: 'solid',
    roughness: 1, opacity: 100, angle: 0,
    x, y, width: w, height: h,
    strokeColor: color, backgroundColor: 'transparent',
    groupIds: [], frameId: null, roundness: null, seed: nonce(),
    boundElements: null, updated: Date.now(), link: null, locked: false,
    text: label, fontSize, fontFamily: 1,
    textAlign: 'left', verticalAlign: 'top',
    containerId: null, originalText: label, lineHeight: 1.45,
    baseline: Math.round(fontSize * 0.8),
  });
}

function title(x, y, text, fontSize, color) {
  elements.push({
    type: 'text',
    version: 1, versionNonce: nonce(), isDeleted: false, id: id(),
    fillStyle: 'solid', strokeWidth: 1, strokeStyle: 'solid',
    roughness: 1, opacity: 100, angle: 0,
    x, y, width: 800, height: fontSize * 1.5,
    strokeColor: color, backgroundColor: 'transparent',
    groupIds: [], frameId: null, roundness: null, seed: nonce(),
    boundElements: null, updated: Date.now(), link: null, locked: false,
    text, fontSize, fontFamily: 1,
    textAlign: 'left', verticalAlign: 'top',
    containerId: null, originalText: text, lineHeight: 1.25,
    baseline: Math.round(fontSize * 0.8),
  });
}

// ============================================================
// Layout — generous spacing, wider boxes
// ============================================================

// Title block (top left, free area)
title(60, 30, 'Althea Systems VBL — Architecture', 30, C.text);
title(60, 75, 'Mono-repo · .NET 10 · Next.js 16 · Docker Compose', 16, '#868e96');

// Main column centered around x=900, width 700
const mainX = 540;
const mainW = 720;
const mainCenterX = mainX + mainW / 2;

// ─── Row 1: Browser ─────────────────────────────────────
const browser = rect(
  mainCenterX - 140, 150, 280, 80,
  C.client, '👤  Browser',
  { fontSize: 22 },
);

// ─── Row 2: Frontend ────────────────────────────────────
const frontend = rect(
  mainX, 300, mainW, 200,
  C.frontend,
  'Next.js 16  (Turbopack · App Router)\n' +
  '────────────────────────────────────\n' +
  '(store)/  home · search · product · cart · checkout · account\n' +
  '(admin)/  dashboard · products · orders · invoices · users\n' +
  '\n' +
  'i18n  fr · en · ms · ar (RTL)     JWT in localStorage\n' +
  'CSP + X-Frame-Options + Referrer-Policy     Step-Up in memory',
  { fontSize: 15 },
);

// ─── Row 3: API ────────────────────────────────────────
const api = rect(
  mainX, 580, mainW, 140,
  C.api,
  'ASP.NET Core 10 API   ·   port 8080   ·   user "app" UID 1654\n' +
  '────────────────────────────────────────────────────────────\n' +
  'Middleware pipeline\n' +
  'ErrorHandler → JwtMiddleware → CORS → RateLimit → Auth',
  { fontSize: 15 },
);

// ─── Row 3b: Controllers + Services (side by side, wider)
const ctrlW = 350;
const svcW = 350;
const gap = 20;
const totalW = ctrlW + svcW + gap;
const cStart = mainCenterX - totalW / 2;

const ctrls = rect(
  cStart, 770, ctrlW, 240,
  C.api,
  'Controllers\n' +
  '──────────────────────\n' +
  'AuthController\n' +
  'UserController\n' +
  'OrderController\n' +
  'InvoiceController\n' +
  'ChatController\n' +
  'ProductController · CategoryController\n' +
  'TwoFactorController',
  { fontSize: 14 },
);

const svcs = rect(
  cStart + ctrlW + gap, 770, svcW, 240,
  C.api,
  'Services\n' +
  '──────────────────────\n' +
  'AuthService    +    LoginAttemptStore\n' +
  'TwoFactorService    (TOTP RFC 6238)\n' +
  'TokenService    (JWT purpose claim)\n' +
  'EncryptionService    (AES-256-GCM)\n' +
  'PasswordHasher    (BCrypt cost 12)\n' +
  'OrderService · InvoiceService · ...',
  { fontSize: 14 },
);

// ─── Row 4: Databases ──────────────────────────────────
const dbW = 280;
const dbGap = 30;
const dbY = 1080;

const postgres = rect(
  mainCenterX - dbW * 1.5 - dbGap, dbY, dbW, 150,
  C.data,
  '🐘  PostgreSQL 16\n' +
  '──────────────────\n' +
  'EF Core 10 · migrations\n' +
  'users · products · orders\n' +
  'invoices · addresses\n' +
  'payment methods',
  { fontSize: 14 },
);

const mongo = rect(
  mainCenterX - dbW / 2, dbY, dbW, 150,
  C.data,
  '🍃  MongoDB 7\n' +
  '──────────────────\n' +
  'Mongo.Driver\n' +
  'chat conversations\n' +
  '(audit trail bot ↔ user)\n' +
  '(carousel content)',
  { fontSize: 14 },
);

const redis = rect(
  mainCenterX + dbW / 2 + dbGap, dbY, dbW, 150,
  C.data,
  '🟥  Redis 7\n' +
  '──────────────────\n' +
  '2FA state + replay (90s)\n' +
  'Step-Up jti (SET NX EX)\n' +
  'Login attempts + lockout\n' +
  'TwoFactor brute-force gate',
  { fontSize: 14 },
);

// ─── Arrows ──────────────────────────────────────────────
connect(browser, frontend, 'HTTPS', { color: C.arrowLabel });
connect(frontend, api, 'Bearer JWT\nX-Step-Up-Token', { color: C.arrowLabel });

// API → Controllers / Services (dashed, no label, thin)
connect(api, ctrls, '', { dashed: true, thin: true, color: '#868e96' });
connect(api, svcs, '', { dashed: true, thin: true, color: '#868e96' });

// Services → Databases
connect(svcs, postgres, 'EF Core', { color: C.dataArrow });
connect(svcs, mongo, 'Mongo.Driver', { color: C.dataArrow });
connect(svcs, redis, 'StackExchange.Redis', { color: C.dataArrow });

// ─── Side panel LEFT: Infra ─────────────────────────────
panel(40, 290, 320,
  '🚢 INFRA\n\n' +
  '◆ Docker Compose\n' +
  '   front · api · postgres\n' +
  '   mongo · redis\n' +
  '   healthchecks → service_healthy\n' +
  '   non-root users (app UID 1654, nextjs)\n\n' +
  '◆ Volumes persistants\n' +
  '   postgres_data · mongo_data\n\n' +
  '◆ Endpoint /health\n' +
  '   NpgSQL + Redis checks\n' +
  '   probes Docker/k8s ready\n\n' +
  '◆ CI GitHub Actions\n' +
  '   backend (test + cache NuGet)\n' +
  '   frontend (tsc + build)\n' +
  '   triggers: branches "**"\n\n' +
  '◆ Dependabot\n' +
  '   npm · nuget · docker\n' +
  '   github-actions',
  C.infra, 13,
);

// ─── Side panel RIGHT: Sécurité ─────────────────────────
panel(1340, 290, 320,
  '🔒 SÉCURITÉ\n\n' +
  '◆ JWT (HS256, ≥ 32 octets)\n' +
  '   ClockSkew = 0 (anti-replay)\n' +
  '   purpose claim:\n' +
  '     access · challenge\n' +
  '     admin-setup · step-up\n\n' +
  '◆ 2FA TOTP RFC 6238 (Otp.NET)\n' +
  '   secret AES-256-GCM at rest\n' +
  '   recovery codes BCrypt-hashed\n' +
  '   brute-force gate 5/15min lock\n\n' +
  '◆ Step-Up\n' +
  '   Action  60s · single-use\n' +
  '   Admin   30min · réutilisable\n\n' +
  '◆ Rate-Limit (defense in depth)\n' +
  '   par IP   10/min (auth endpoints)\n' +
  '   par compte  5 fail → lock 15min\n\n' +
  '◆ AuthZ\n' +
  '   ownership check sur /users/{id}\n' +
  '   /addresses · /payments · /orders\n' +
  '   /invoices · /chat\n\n' +
  '◆ Front\n' +
  '   CSP + X-Frame-Options DENY\n' +
  '   Referrer-Policy strict-origin\n' +
  '   cart cleared on logout',
  C.security, 13,
);

// ─── Legend (bottom) ────────────────────────────────────
panel(60, 1260, 1600,
  'Légende :  bleu = client/UI  ·  vert = front Next.js  ·  orange = API/services  ·  ' +
  'violet = data layer  ·  flèche bleue = requête HTTP  ·  flèche violette = driver DB  ·  ' +
  'flèche pointillée = wiring DI interne',
  '#495057', 13,
);

// ============================================================
// Serialize
// ============================================================

const doc = {
  type: 'excalidraw',
  version: 2,
  source: 'https://github.com/altheas-system-VBL',
  elements,
  appState: {
    gridSize: null,
    viewBackgroundColor: '#ffffff',
  },
  files: {},
};

const outDir = path.resolve(__dirname, '..', 'docs');
fs.mkdirSync(outDir, { recursive: true });
const outPath = path.join(outDir, 'architecture.excalidraw');
fs.writeFileSync(outPath, JSON.stringify(doc, null, 2));
console.log(`✓ Wrote ${outPath}`);
console.log(`  ${elements.length} elements`);
