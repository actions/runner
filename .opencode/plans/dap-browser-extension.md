# DAP Browser Extension for GitHub Actions Debugging

**Status:** Planned  
**Date:** January 2026  
**Related:** [dap-debugging.md](./dap-debugging.md), [dap-step-backwards.md](./dap-step-backwards.md)

## Overview

A Chrome extension that injects a debugger UI into GitHub Actions job pages, connecting to a runner's DAP server via a WebSocket-to-TCP proxy. This enables interactive debugging (variable inspection, REPL, step control) directly in the browser.

**Goal:** Demonstrate the power of implementing a standard protocol like DAP - the same debugging capabilities available in nvim-dap can now work in a browser with minimal effort.

## Architecture

```
┌──────────────────────────────────────────────────────────────────────────────────┐
│                           GitHub Actions Job Page                                 │
│                                                                                   │
│  ┌──────────────────────────────────────────────────────────────────────────┐   │
│  │                     Content Script (injected)                             │   │
│  │  - Observes DOM for step elements                                         │   │
│  │  - Injects debugger pane above the "next pending step"                    │   │
│  │  - Renders scopes tree, REPL console, control buttons                     │   │
│  │  - Communicates with background script via chrome.runtime messaging       │   │
│  └───────────────────────────────────────┬──────────────────────────────────┘   │
│                                          │ chrome.runtime.sendMessage           │
└──────────────────────────────────────────┼──────────────────────────────────────┘
                                           │
┌──────────────────────────────────────────┼──────────────────────────────────────┐
│                  Background Script (Service Worker)                              │
│  ┌───────────────────────────────────────┴──────────────────────────────────┐   │
│  │                        DAP Client                                         │   │
│  │  - Connects to WebSocket proxy (ws://localhost:4712)                      │   │
│  │  - Implements DAP request/response handling                               │   │
│  │  - Relays events to content script                                        │   │
│  │  - Manages connection state                                               │   │
│  └───────────────────────────────────────┬──────────────────────────────────┘   │
│                                          │ WebSocket                             │
└──────────────────────────────────────────┼──────────────────────────────────────┘
                                           │
┌──────────────────────────────────────────┼──────────────────────────────────────┐
│                   WebSocket-to-TCP Proxy (Node.js)                               │
│  ┌───────────────────────────────────────┴──────────────────────────────────┐   │
│  │  - Listens on ws://localhost:4712                                         │   │
│  │  - Connects to tcp://localhost:4711 (DAP server)                          │   │
│  │  - Handles DAP message framing (Content-Length headers)                   │   │
│  │  - Bidirectional message relay                                            │   │
│  └───────────────────────────────────────┬──────────────────────────────────┘   │
│                                          │ TCP                                   │
└──────────────────────────────────────────┼──────────────────────────────────────┘
                                           │
┌──────────────────────────────────────────┼──────────────────────────────────────┐
│                      Runner DAP Server (existing)                                │
│  ┌───────────────────────────────────────┴──────────────────────────────────┐   │
│  │  tcp://localhost:4711                                                     │   │
│  │  - DapServer.cs (existing implementation)                                 │   │
│  │  - DapDebugSession.cs (handles all debug operations)                      │   │
│  └──────────────────────────────────────────────────────────────────────────┘   │
└──────────────────────────────────────────────────────────────────────────────────┘
```

## Directory Structure

```
browser-ext/
├── manifest.json              # Chrome extension manifest v3
├── background/
│   └── background.js          # Service worker - DAP client, WebSocket connection
├── content/
│   ├── content.js             # DOM manipulation, pane injection, UI logic
│   └── content.css            # Styling for debugger pane
├── popup/
│   ├── popup.html             # Extension popup (connection status, settings)
│   ├── popup.js
│   └── popup.css
├── lib/
│   └── dap-protocol.js        # DAP message types and helpers
├── proxy/
│   ├── proxy.js               # WebSocket-to-TCP bridge
│   └── package.json           # Proxy dependencies (ws)
└── icons/
    ├── icon16.png
    ├── icon48.png
    └── icon128.png
```

## GitHub Actions Job Page DOM Structure

### Step Container Element: `<check-step>`

Each step is a custom element with rich data attributes:

```html
<check-step 
  data-name="Run cat doesnotexist"      <!-- Step display name -->
  data-number="4"                        <!-- Step number (1-indexed) -->
  data-conclusion="failure"              <!-- success|failure|skipped|cancelled|in-progress|null -->
  data-external-id="759535e9-..."        <!-- UUID for step -->
  data-expand="true"                     <!-- Whether expanded -->
  data-started-at="2026-01-15T..."       <!-- Timestamp -->
  data-completed-at="2026-01-15T..."     <!-- Timestamp -->
  data-log-url="..."                     <!-- URL for logs -->
  data-job-completed=""                  <!-- Present when job is done -->
>
```

### Key Selectors

| Target | Selector |
|--------|----------|
| All steps | `check-step` or `check-steps > check-step` |
| Step by number | `check-step[data-number="4"]` |
| Failed steps | `check-step[data-conclusion="failure"]` |
| In-progress steps | `check-step[data-conclusion="in-progress"]` |
| Pending steps | `check-step:not([data-conclusion])` |
| Step name | `check-step[data-name]` attribute |
| Steps container | `check-steps` |
| Step details (expandable) | `details.CheckStep` inside `check-step` |

### Dark Mode Detection

GitHub stores theme info in:
```html
<script type="application/json" id="__PRIMER_DATA__...">{"resolvedServerColorMode":"night"}</script>
```

Can also check: `document.documentElement.dataset.colorMode` or CSS custom properties.

---

## Implementation Phases

### Phase 1: WebSocket-to-TCP Proxy (~1-2 hours)

**File: `browser-ext/proxy/proxy.js`**

A minimal Node.js script that bridges WebSocket to the DAP TCP server.

**Responsibilities:**
1. Listen for WebSocket connections on port 4712
2. For each WebSocket client, open TCP connection to localhost:4711
3. Handle DAP message framing:
   - WS→TCP: Wrap JSON with `Content-Length: N\r\n\r\n`
   - TCP→WS: Parse headers, extract JSON, send to WebSocket
4. Log messages for debugging
5. Clean disconnect handling

**Key implementation:**

```javascript
const WebSocket = require('ws');
const net = require('net');

const WS_PORT = 4712;
const DAP_HOST = '127.0.0.1';
const DAP_PORT = 4711;

const wss = new WebSocket.Server({ port: WS_PORT });

wss.on('connection', (ws) => {
  console.log('[Proxy] WebSocket client connected');
  
  const tcp = net.createConnection({ host: DAP_HOST, port: DAP_PORT });
  let buffer = '';
  
  // WebSocket → TCP (add Content-Length framing)
  ws.on('message', (data) => {
    const json = data.toString();
    const framed = `Content-Length: ${Buffer.byteLength(json)}\r\n\r\n${json}`;
    tcp.write(framed);
  });
  
  // TCP → WebSocket (parse Content-Length framing)
  tcp.on('data', (chunk) => {
    buffer += chunk.toString();
    // Parse DAP messages from buffer...
    // For each complete message, ws.send(json)
  });
  
  // Handle disconnects
  ws.on('close', () => tcp.end());
  tcp.on('close', () => ws.close());
});
```

**File: `browser-ext/proxy/package.json`**

```json
{
  "name": "dap-websocket-proxy",
  "version": "1.0.0",
  "main": "proxy.js",
  "dependencies": {
    "ws": "^8.16.0"
  }
}
```

---

### Phase 2: Chrome Extension Scaffold (~1 hour)

**File: `browser-ext/manifest.json`**

```json
{
  "manifest_version": 3,
  "name": "Actions DAP Debugger",
  "version": "0.1.0",
  "description": "Debug GitHub Actions workflows with DAP",
  "permissions": ["activeTab", "storage"],
  "host_permissions": ["https://github.com/*"],
  "background": {
    "service_worker": "background/background.js"
  },
  "content_scripts": [{
    "matches": ["https://github.com/*/*/actions/runs/*/job/*"],
    "js": ["lib/dap-protocol.js", "content/content.js"],
    "css": ["content/content.css"],
    "run_at": "document_idle"
  }],
  "action": {
    "default_popup": "popup/popup.html",
    "default_icon": {
      "16": "icons/icon16.png",
      "48": "icons/icon48.png",
      "128": "icons/icon128.png"
    }
  }
}
```

**Icons:** Create simple debug-themed icons (bug icon or similar).

---

### Phase 3: Background Script - DAP Client (~2-3 hours)

**File: `browser-ext/background/background.js`**

**Responsibilities:**

1. **Connection management:**
   - Connect to WebSocket proxy on user action (popup button)
   - Handle disconnect/reconnect
   - Track connection state (disconnected, connecting, connected, paused, running)

2. **DAP protocol handling:**
   - Sequence number tracking
   - Request/response correlation (pending requests Map)
   - Event dispatch to content script

3. **DAP commands to implement:**

| Command | Purpose |
|---------|---------|
| `initialize` | Exchange capabilities |
| `attach` | Attach to running debug session |
| `configurationDone` | Signal ready to receive events |
| `threads` | Get thread list (single thread for job) |
| `stackTrace` | Get current step + history as frames |
| `scopes` | Get scope categories for a frame |
| `variables` | Get variables for a scope/object |
| `evaluate` | Expression eval + REPL commands |
| `continue` | Run to end or next breakpoint |
| `next` | Step to next step |
| `stepBack` | Step back to previous checkpoint |
| `reverseContinue` | Go back to first checkpoint |
| `disconnect` | End debug session |

4. **Message relay structure:**

```javascript
// Content script → Background
chrome.runtime.onMessage.addListener((message, sender, sendResponse) => {
  if (message.type === 'dap-request') {
    sendDapRequest(message.command, message.args)
      .then(response => sendResponse({ success: true, body: response }))
      .catch(error => sendResponse({ success: false, error: error.message }));
    return true; // Async response
  }
  if (message.type === 'connect') {
    connectToProxy(message.host, message.port);
  }
});

// Background → Content script (events)
function broadcastEvent(event) {
  chrome.tabs.query({ url: 'https://github.com/*/*/actions/runs/*/job/*' }, (tabs) => {
    tabs.forEach(tab => {
      chrome.tabs.sendMessage(tab.id, { type: 'dap-event', event });
    });
  });
}
```

---

### Phase 4: Content Script - UI Injection (~3-4 hours)

**File: `browser-ext/content/content.js`**

#### 4.1 Step Detection & Mapping

```javascript
// Build step map: DAP frame index → DOM element
function buildStepMap() {
  const steps = document.querySelectorAll('check-step');
  const map = new Map();
  steps.forEach((el, idx) => {
    map.set(idx, {
      element: el,
      number: parseInt(el.dataset.number),
      name: el.dataset.name,
      conclusion: el.dataset.conclusion,
      externalId: el.dataset.externalId
    });
  });
  return map;
}

// Match DAP frame to DOM step
// DAP stackTrace returns frames with name = step display name
function findStepByName(stepName) {
  return document.querySelector(`check-step[data-name="${CSS.escape(stepName)}"]`);
}
```

#### 4.2 Debugger Pane Structure

```html
<div class="dap-debugger-pane px-2 mb-2 border rounded-2" data-dap-step="4">
  <!-- Header with status -->
  <div class="dap-header d-flex flex-items-center p-2 border-bottom">
    <svg class="octicon octicon-bug mr-2">...</svg>
    <span class="text-bold">Debugger</span>
    <span class="color-fg-muted ml-2">Paused before: Run cat doesnotexist</span>
    <span class="Label Label--attention ml-auto">PAUSED</span>
  </div>
  
  <!-- Main content: 1/3 scopes + 2/3 REPL -->
  <div class="dap-content d-flex" style="height: 300px;">
    <!-- Scopes Panel (1/3) -->
    <div class="dap-scopes border-right overflow-auto" style="width: 33%;">
      <div class="dap-scope-header p-2 text-bold border-bottom">Variables</div>
      <div class="dap-scope-tree p-2">
        <!-- Tree nodes rendered dynamically -->
      </div>
    </div>
    
    <!-- REPL Console (2/3) -->
    <div class="dap-repl d-flex flex-column" style="width: 67%;">
      <div class="dap-repl-header p-2 text-bold border-bottom">Console</div>
      <div class="dap-repl-output overflow-auto flex-auto p-2 text-mono text-small">
        <!-- Output lines rendered dynamically -->
      </div>
      <div class="dap-repl-input border-top p-2">
        <input type="text" class="form-control text-mono" 
               placeholder="Enter expression or !command">
      </div>
    </div>
  </div>
  
  <!-- Control buttons -->
  <div class="dap-controls d-flex flex-items-center p-2 border-top">
    <button class="btn btn-sm mr-2" data-action="reverseContinue" title="Reverse Continue">
      ⏮
    </button>
    <button class="btn btn-sm mr-2" data-action="stepBack" title="Step Back">
      ◀
    </button>
    <button class="btn btn-sm btn-primary mr-2" data-action="continue" title="Continue">
      ▶
    </button>
    <button class="btn btn-sm mr-2" data-action="next" title="Step">
      ⏭
    </button>
    <span class="color-fg-muted ml-auto text-small">
      Step 4 of 8 · Checkpoints: 3
    </span>
  </div>
</div>
```

#### 4.3 Pane Injection

```javascript
function injectDebuggerPane(beforeStep) {
  // Remove existing pane if any
  const existing = document.querySelector('.dap-debugger-pane');
  if (existing) existing.remove();
  
  // Create pane
  const pane = document.createElement('div');
  pane.className = 'dap-debugger-pane px-2 mb-2 border rounded-2';
  pane.innerHTML = PANE_HTML; // Template from above
  
  // Insert before the target step
  beforeStep.parentNode.insertBefore(pane, beforeStep);
  
  // Setup event handlers
  setupPaneEventHandlers(pane);
  
  return pane;
}

function moveDebuggerPane(newStepIndex, stepName) {
  const pane = document.querySelector('.dap-debugger-pane');
  const steps = document.querySelectorAll('check-step');
  const targetStep = steps[newStepIndex];
  
  if (pane && targetStep) {
    targetStep.parentNode.insertBefore(pane, targetStep);
    pane.querySelector('.dap-header .color-fg-muted').textContent = 
      `Paused before: ${stepName}`;
    pane.dataset.dapStep = targetStep.dataset.number;
  }
}
```

#### 4.4 Scopes Tree Rendering

```javascript
async function loadScopes(frameId) {
  const response = await sendDapRequest('scopes', { frameId });
  const scopesContainer = document.querySelector('.dap-scope-tree');
  scopesContainer.innerHTML = '';
  
  for (const scope of response.scopes) {
    const node = createTreeNode(scope.name, scope.variablesReference, true);
    scopesContainer.appendChild(node);
  }
}

function createTreeNode(name, variablesReference, isExpandable) {
  const node = document.createElement('div');
  node.className = 'dap-tree-node';
  node.dataset.variablesRef = variablesReference;
  node.innerHTML = `
    <span class="dap-expand-icon">${isExpandable ? '▶' : ' '}</span>
    <span class="text-bold">${escapeHtml(name)}</span>
  `;
  
  if (isExpandable) {
    node.addEventListener('click', () => toggleTreeNode(node));
  }
  
  return node;
}

async function toggleTreeNode(node) {
  const children = node.querySelector('.dap-tree-children');
  if (children) {
    children.hidden = !children.hidden;
    node.querySelector('.dap-expand-icon').textContent = children.hidden ? '▶' : '▼';
    return;
  }
  
  // Fetch children
  const variablesRef = parseInt(node.dataset.variablesRef);
  const response = await sendDapRequest('variables', { variablesReference: variablesRef });
  
  const childContainer = document.createElement('div');
  childContainer.className = 'dap-tree-children ml-3';
  
  for (const variable of response.variables) {
    if (variable.variablesReference > 0) {
      // Expandable
      const childNode = createTreeNode(variable.name, variable.variablesReference, true);
      childNode.querySelector('.text-bold').insertAdjacentHTML('afterend', 
        `: <span class="color-fg-muted">${escapeHtml(variable.value)}</span>`);
      childContainer.appendChild(childNode);
    } else {
      // Leaf
      const leaf = document.createElement('div');
      leaf.className = 'dap-tree-leaf';
      leaf.innerHTML = `
        <span class="color-fg-muted">${escapeHtml(variable.name)}:</span>
        <span>${escapeHtml(variable.value)}</span>
      `;
      childContainer.appendChild(leaf);
    }
  }
  
  node.appendChild(childContainer);
  node.querySelector('.dap-expand-icon').textContent = '▼';
}
```

#### 4.5 REPL Console

```javascript
function setupReplInput(pane) {
  const input = pane.querySelector('.dap-repl-input input');
  const output = pane.querySelector('.dap-repl-output');
  const history = [];
  let historyIndex = -1;
  
  input.addEventListener('keydown', async (e) => {
    if (e.key === 'Enter') {
      const command = input.value.trim();
      if (!command) return;
      
      history.push(command);
      historyIndex = history.length;
      input.value = '';
      
      // Show command
      appendOutput(output, `> ${command}`, 'input');
      
      // Send to DAP
      try {
        const response = await sendDapRequest('evaluate', {
          expression: command,
          context: command.startsWith('!') ? 'repl' : 'watch'
        });
        appendOutput(output, response.result, 'result');
      } catch (error) {
        appendOutput(output, error.message, 'error');
      }
    } else if (e.key === 'ArrowUp') {
      if (historyIndex > 0) {
        historyIndex--;
        input.value = history[historyIndex];
      }
      e.preventDefault();
    } else if (e.key === 'ArrowDown') {
      if (historyIndex < history.length - 1) {
        historyIndex++;
        input.value = history[historyIndex];
      } else {
        historyIndex = history.length;
        input.value = '';
      }
      e.preventDefault();
    }
  });
}

function appendOutput(container, text, type) {
  const line = document.createElement('div');
  line.className = `dap-output-${type}`;
  if (type === 'error') line.classList.add('color-fg-danger');
  if (type === 'input') line.classList.add('color-fg-muted');
  line.textContent = text;
  container.appendChild(line);
  container.scrollTop = container.scrollHeight;
}
```

#### 4.6 DAP Event Handling

```javascript
chrome.runtime.onMessage.addListener((message) => {
  if (message.type !== 'dap-event') return;
  
  const event = message.event;
  
  switch (event.event) {
    case 'stopped':
      handleStoppedEvent(event.body);
      break;
    case 'output':
      handleOutputEvent(event.body);
      break;
    case 'terminated':
      handleTerminatedEvent();
      break;
  }
});

async function handleStoppedEvent(body) {
  // Update status
  updateStatus('PAUSED', body.reason);
  enableControls(true);
  
  // Get current location
  const stackTrace = await sendDapRequest('stackTrace', { threadId: 1 });
  if (stackTrace.stackFrames.length > 0) {
    const currentFrame = stackTrace.stackFrames[0];
    moveDebuggerPane(currentFrame.id, currentFrame.name);
    await loadScopes(currentFrame.id);
  }
}

function handleOutputEvent(body) {
  const output = document.querySelector('.dap-repl-output');
  if (output) {
    const category = body.category === 'stderr' ? 'error' : 'stdout';
    appendOutput(output, body.output.trimEnd(), category);
  }
}

function handleTerminatedEvent() {
  updateStatus('TERMINATED');
  enableControls(false);
}
```

#### 4.7 Control Buttons

```javascript
function setupControlButtons(pane) {
  pane.querySelectorAll('[data-action]').forEach(btn => {
    btn.addEventListener('click', async () => {
      const action = btn.dataset.action;
      enableControls(false);
      updateStatus('RUNNING');
      
      try {
        await sendDapRequest(action, { threadId: 1 });
      } catch (error) {
        console.error(`DAP ${action} failed:`, error);
        appendOutput(document.querySelector('.dap-repl-output'), 
          `Error: ${error.message}`, 'error');
        enableControls(true);
        updateStatus('ERROR');
      }
    });
  });
}

function enableControls(enabled) {
  document.querySelectorAll('.dap-controls button').forEach(btn => {
    btn.disabled = !enabled;
  });
}

function updateStatus(status, reason) {
  const label = document.querySelector('.dap-header .Label');
  if (label) {
    label.textContent = status;
    label.className = 'Label ml-auto ' + {
      'PAUSED': 'Label--attention',
      'RUNNING': 'Label--success',
      'TERMINATED': 'Label--secondary',
      'ERROR': 'Label--danger'
    }[status];
  }
}
```

---

### Phase 5: Styling (~1 hour)

**File: `browser-ext/content/content.css`**

```css
/* Match GitHub's Primer design system */
.dap-debugger-pane {
  background-color: var(--bgColor-default, #0d1117);
  border-color: var(--borderColor-default, #30363d) !important;
  margin-left: 8px;
  margin-right: 8px;
}

.dap-header {
  background-color: var(--bgColor-muted, #161b22);
}

.dap-scopes {
  border-color: var(--borderColor-default, #30363d) !important;
}

.dap-scope-tree {
  font-size: 12px;
}

.dap-tree-node {
  cursor: pointer;
  padding: 2px 0;
}

.dap-tree-node:hover {
  background-color: var(--bgColor-muted, #161b22);
}

.dap-tree-leaf {
  padding: 2px 0;
  padding-left: 16px;
}

.dap-expand-icon {
  display: inline-block;
  width: 16px;
  text-align: center;
  color: var(--fgColor-muted, #8b949e);
}

.dap-repl-output {
  background-color: var(--bgColor-inset, #010409);
  font-family: ui-monospace, SFMono-Regular, SF Mono, Menlo, Consolas, monospace;
  font-size: 12px;
  line-height: 1.5;
}

.dap-output-input {
  color: var(--fgColor-muted, #8b949e);
}

.dap-output-result {
  color: var(--fgColor-default, #e6edf3);
}

.dap-output-stdout {
  color: var(--fgColor-default, #e6edf3);
}

.dap-output-error {
  color: var(--fgColor-danger, #f85149);
}

.dap-repl-input input {
  font-family: ui-monospace, SFMono-Regular, SF Mono, Menlo, Consolas, monospace;
  font-size: 12px;
  background-color: var(--bgColor-inset, #010409);
  border-color: var(--borderColor-default, #30363d);
  color: var(--fgColor-default, #e6edf3);
}

.dap-controls {
  background-color: var(--bgColor-muted, #161b22);
}

.dap-controls button {
  min-width: 32px;
}

.dap-controls button:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

/* Status labels */
.Label--attention {
  background-color: #9e6a03;
  color: #ffffff;
}

.Label--success {
  background-color: #238636;
  color: #ffffff;
}

.Label--danger {
  background-color: #da3633;
  color: #ffffff;
}

.Label--secondary {
  background-color: #30363d;
  color: #8b949e;
}
```

---

### Phase 6: Popup UI (~1 hour)

**File: `browser-ext/popup/popup.html`**

```html
<!DOCTYPE html>
<html>
<head>
  <link rel="stylesheet" href="popup.css">
</head>
<body>
  <div class="popup-container">
    <h3>Actions DAP Debugger</h3>
    
    <div class="status-section">
      <div class="status-indicator" id="status-indicator"></div>
      <span id="status-text">Disconnected</span>
    </div>
    
    <div class="config-section">
      <label>
        Proxy Host
        <input type="text" id="proxy-host" value="localhost">
      </label>
      <label>
        Proxy Port
        <input type="number" id="proxy-port" value="4712">
      </label>
    </div>
    
    <div class="actions-section">
      <button id="connect-btn" class="btn-primary">Connect</button>
      <button id="disconnect-btn" class="btn-secondary" disabled>Disconnect</button>
    </div>
    
    <div class="help-section">
      <p>1. Start the proxy: <code>cd browser-ext/proxy && node proxy.js</code></p>
      <p>2. Start a job with debug logging enabled</p>
      <p>3. Click Connect</p>
    </div>
  </div>
  <script src="popup.js"></script>
</body>
</html>
```

**File: `browser-ext/popup/popup.js`**

```javascript
document.addEventListener('DOMContentLoaded', () => {
  const statusIndicator = document.getElementById('status-indicator');
  const statusText = document.getElementById('status-text');
  const connectBtn = document.getElementById('connect-btn');
  const disconnectBtn = document.getElementById('disconnect-btn');
  const hostInput = document.getElementById('proxy-host');
  const portInput = document.getElementById('proxy-port');
  
  // Load saved config
  chrome.storage.local.get(['proxyHost', 'proxyPort'], (data) => {
    if (data.proxyHost) hostInput.value = data.proxyHost;
    if (data.proxyPort) portInput.value = data.proxyPort;
  });
  
  // Get current status from background
  chrome.runtime.sendMessage({ type: 'get-status' }, (response) => {
    updateStatusUI(response.status);
  });
  
  connectBtn.addEventListener('click', () => {
    const host = hostInput.value;
    const port = parseInt(portInput.value);
    
    // Save config
    chrome.storage.local.set({ proxyHost: host, proxyPort: port });
    
    // Connect
    chrome.runtime.sendMessage({ type: 'connect', host, port }, (response) => {
      updateStatusUI(response.status);
    });
  });
  
  disconnectBtn.addEventListener('click', () => {
    chrome.runtime.sendMessage({ type: 'disconnect' }, (response) => {
      updateStatusUI(response.status);
    });
  });
  
  function updateStatusUI(status) {
    statusText.textContent = status;
    statusIndicator.className = 'status-indicator status-' + status.toLowerCase();
    connectBtn.disabled = (status !== 'disconnected');
    disconnectBtn.disabled = (status === 'disconnected');
  }
});
```

**File: `browser-ext/popup/popup.css`**

```css
body {
  width: 300px;
  padding: 16px;
  font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Helvetica, Arial, sans-serif;
  font-size: 14px;
  background-color: #0d1117;
  color: #e6edf3;
}

h3 {
  margin: 0 0 16px 0;
  font-size: 16px;
}

.status-section {
  display: flex;
  align-items: center;
  margin-bottom: 16px;
  padding: 8px;
  background-color: #161b22;
  border-radius: 6px;
}

.status-indicator {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  margin-right: 8px;
}

.status-disconnected { background-color: #6e7681; }
.status-connecting { background-color: #9e6a03; }
.status-connected { background-color: #238636; }
.status-paused { background-color: #9e6a03; }
.status-error { background-color: #da3633; }

.config-section {
  margin-bottom: 16px;
}

.config-section label {
  display: block;
  margin-bottom: 8px;
  font-size: 12px;
  color: #8b949e;
}

.config-section input {
  width: 100%;
  padding: 8px;
  margin-top: 4px;
  background-color: #0d1117;
  border: 1px solid #30363d;
  border-radius: 6px;
  color: #e6edf3;
  font-size: 14px;
}

.actions-section {
  display: flex;
  gap: 8px;
  margin-bottom: 16px;
}

button {
  flex: 1;
  padding: 8px 16px;
  border: none;
  border-radius: 6px;
  font-size: 14px;
  cursor: pointer;
}

button:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.btn-primary {
  background-color: #238636;
  color: white;
}

.btn-secondary {
  background-color: #30363d;
  color: #e6edf3;
}

.help-section {
  font-size: 12px;
  color: #8b949e;
}

.help-section p {
  margin: 4px 0;
}

.help-section code {
  background-color: #161b22;
  padding: 2px 4px;
  border-radius: 3px;
  font-family: ui-monospace, monospace;
}
```

---

### Phase 7: DAP Protocol Helpers

**File: `browser-ext/lib/dap-protocol.js`**

```javascript
// DAP message types and constants
const DapCommands = {
  INITIALIZE: 'initialize',
  ATTACH: 'attach',
  CONFIGURATION_DONE: 'configurationDone',
  THREADS: 'threads',
  STACK_TRACE: 'stackTrace',
  SCOPES: 'scopes',
  VARIABLES: 'variables',
  EVALUATE: 'evaluate',
  CONTINUE: 'continue',
  NEXT: 'next',
  STEP_BACK: 'stepBack',
  REVERSE_CONTINUE: 'reverseContinue',
  DISCONNECT: 'disconnect'
};

const DapEvents = {
  STOPPED: 'stopped',
  OUTPUT: 'output',
  TERMINATED: 'terminated',
  INITIALIZED: 'initialized'
};

// Helper to create DAP request
function createDapRequest(seq, command, args = {}) {
  return {
    seq,
    type: 'request',
    command,
    arguments: args
  };
}

// Helper to parse DAP response/event
function parseDapMessage(json) {
  const msg = JSON.parse(json);
  return {
    isResponse: msg.type === 'response',
    isEvent: msg.type === 'event',
    seq: msg.seq,
    requestSeq: msg.request_seq,
    command: msg.command,
    event: msg.event,
    success: msg.success,
    body: msg.body,
    message: msg.message
  };
}

// Export for use in other scripts
if (typeof module !== 'undefined') {
  module.exports = { DapCommands, DapEvents, createDapRequest, parseDapMessage };
}
```

---

## DAP Protocol Flow

```
Extension                    Proxy                     Runner
    │                          │                          │
    │──── WebSocket connect ──►│                          │
    │                          │──── TCP connect ────────►│
    │◄─── "connected" ─────────│                          │
    │                          │                          │
    │──── initialize ─────────►│──── initialize ─────────►│
    │◄─── InitializeResponse ──│◄─── InitializeResponse ──│
    │                          │                          │
    │──── attach ─────────────►│──── attach ─────────────►│
    │◄─── AttachResponse ──────│◄─── AttachResponse ──────│
    │                          │                          │
    │──── configurationDone ──►│──── configurationDone ──►│
    │◄─── ConfigDoneResponse ──│◄─── ConfigDoneResponse ──│
    │                          │                          │
    │◄─── stopped event ───────│◄─── stopped event ───────│  (paused at step)
    │                          │                          │
    │──── threads ────────────►│──── threads ────────────►│
    │◄─── ThreadsResponse ─────│◄─── ThreadsResponse ─────│
    │                          │                          │
    │──── stackTrace ─────────►│──── stackTrace ─────────►│
    │◄─── StackTraceResponse ──│◄─── StackTraceResponse ──│
    │                          │                          │
    │──── scopes ─────────────►│──── scopes ─────────────►│
    │◄─── ScopesResponse ──────│◄─── ScopesResponse ──────│
    │                          │                          │
    │──── variables ──────────►│──── variables ──────────►│  (user expands scope)
    │◄─── VariablesResponse ───│◄─── VariablesResponse ───│
    │                          │                          │
    │──── evaluate ───────────►│──── evaluate ───────────►│  (REPL command)
    │◄─── output events ───────│◄─── output events ───────│  (streaming)
    │◄─── EvaluateResponse ────│◄─── EvaluateResponse ────│
    │                          │                          │
    │──── next ───────────────►│──── next ───────────────►│  (step to next)
    │◄─── NextResponse ────────│◄─── NextResponse ────────│
    │◄─── stopped event ───────│◄─── stopped event ───────│  (paused at next step)
```

---

## Files Summary

| File | Lines Est. | Purpose |
|------|------------|---------|
| `proxy/proxy.js` | ~100 | WebSocket↔TCP bridge with DAP framing |
| `proxy/package.json` | ~10 | Proxy dependencies |
| `manifest.json` | ~35 | Extension configuration |
| `background/background.js` | ~300 | DAP client, WebSocket, message relay |
| `content/content.js` | ~450 | DOM manipulation, pane injection, UI |
| `content/content.css` | ~150 | Debugger pane styling |
| `lib/dap-protocol.js` | ~50 | DAP message helpers |
| `popup/popup.html` | ~40 | Popup structure |
| `popup/popup.js` | ~80 | Popup logic |
| `popup/popup.css` | ~80 | Popup styling |

**Total: ~1,300 lines**

---

## Testing Plan

### 1. Proxy Testing
- [ ] Start proxy, verify WebSocket accepts connection
- [ ] Test with simple WebSocket client (wscat)
- [ ] Connect nvim-dap through proxy to verify passthrough works

### 2. Extension Load Testing
- [ ] Load unpacked extension in Chrome
- [ ] Navigate to GitHub Actions job page
- [ ] Verify content script activates (check console)
- [ ] Click popup, verify UI renders

### 3. Integration Testing
- [ ] Start proxy
- [ ] Run workflow with "Enable debug logging" 
- [ ] Connect extension via popup
- [ ] Verify debugger pane appears
- [ ] Test scope expansion
- [ ] Test REPL commands: `${{ github.ref }}`, `!env | grep GITHUB`
- [ ] Test step controls: next, continue
- [ ] Test step-back functionality
- [ ] Verify pane moves between steps

### 4. Edge Cases
- [ ] Disconnect/reconnect handling
- [ ] Proxy not running error
- [ ] DAP server timeout
- [ ] Large variable values
- [ ] Special characters in step names

---

## Demo Flow

1. **Setup:**
   ```bash
   cd browser-ext/proxy && npm install && node proxy.js
   ```
   
2. **Load Extension:**
   - Chrome → Extensions → Load unpacked → select `browser-ext/`

3. **Start Debug Session:**
   - Go to GitHub repo → Actions → Re-run job with "Enable debug logging"
   - Wait for runner to show "DAP debugger waiting for connection..."

4. **Connect:**
   - Open job page in Chrome
   - Click extension popup → "Connect"
   - Debugger pane appears above first step

5. **Demo Features:**
   - Expand scopes to show `github`, `env`, `steps` contexts
   - Run REPL: `${{ github.event_name }}` → shows "push"
   - Run REPL: `!ls -la` → shows files
   - Click "Step" → pane moves to next step
   - Click "Step Back" → demonstrate time-travel
   - Modify env via REPL, step forward, show fix works

---

## Estimated Effort

| Phase | Effort |
|-------|--------|
| Phase 1: WebSocket proxy | 1-2 hours |
| Phase 2: Extension scaffold | 1 hour |
| Phase 3: Background/DAP client | 2-3 hours |
| Phase 4: Content script/UI | 3-4 hours |
| Phase 5: Styling | 1 hour |
| Phase 6: Popup UI | 1 hour |
| Phase 7: DAP helpers | 0.5 hours |
| Testing & Polish | 1-2 hours |
| **Total** | **~11-14 hours** |

---

## Future Enhancements (Out of Scope)

- Firefox extension support
- Breakpoint setting UI (click on step to set breakpoint)
- Watch expressions panel
- Call stack visualization  
- Integration with GitHub Codespaces (direct connection without proxy)
- Persistent connection across page navigations
- Multiple job debugging
