/**
 * Background Script - DAP Client
 *
 * Service worker that manages WebSocket connection to the proxy
 * and handles DAP protocol communication.
 */

// Connection state
let ws = null;
let connectionStatus = 'disconnected'; // disconnected, connecting, connected, paused, running
let sequenceNumber = 1;
const pendingRequests = new Map(); // seq -> { resolve, reject, command }

// Default configuration
const DEFAULT_URL = 'ws://localhost:4712';

/**
 * Connect to the WebSocket proxy
 */
function connect(url) {
  if (ws && ws.readyState === WebSocket.OPEN) {
    console.log('[Background] Already connected');
    return;
  }

  connectionStatus = 'connecting';
  broadcastStatus();

  // Use provided URL or default
  const wsUrl = url || DEFAULT_URL;
  console.log(`[Background] Connecting to ${wsUrl}`);

  ws = new WebSocket(wsUrl);

  ws.onopen = async () => {
    console.log('[Background] WebSocket connected');
    connectionStatus = 'connected';
    broadcastStatus();

    // Initialize DAP session
    try {
      await initializeDapSession();
    } catch (error) {
      console.error('[Background] Failed to initialize DAP session:', error);
      connectionStatus = 'error';
      broadcastStatus();
    }
  };

  ws.onmessage = (event) => {
    try {
      const message = JSON.parse(event.data);
      handleDapMessage(message);
    } catch (error) {
      console.error('[Background] Failed to parse message:', error);
    }
  };

  ws.onclose = (event) => {
    console.log(`[Background] WebSocket closed: ${event.code} ${event.reason}`);
    ws = null;
    connectionStatus = 'disconnected';
    broadcastStatus();

    // Reject any pending requests
    for (const [seq, pending] of pendingRequests) {
      pending.reject(new Error('Connection closed'));
    }
    pendingRequests.clear();
  };

  ws.onerror = (event) => {
    console.error('[Background] WebSocket error:', event);
    connectionStatus = 'error';
    broadcastStatus();
  };
}

/**
 * Disconnect from the WebSocket proxy
 */
function disconnect() {
  if (ws) {
    // Send disconnect request to DAP server first
    sendDapRequest('disconnect', {}).catch(() => {});
    ws.close(1000, 'User disconnected');
    ws = null;
  }
  connectionStatus = 'disconnected';
  broadcastStatus();
}

/**
 * Initialize DAP session (initialize + attach + configurationDone)
 */
async function initializeDapSession() {
  // 1. Initialize
  const initResponse = await sendDapRequest('initialize', {
    clientID: 'browser-extension',
    clientName: 'Actions DAP Debugger',
    adapterID: 'github-actions-runner',
    pathFormat: 'path',
    linesStartAt1: true,
    columnsStartAt1: true,
    supportsVariableType: true,
    supportsVariablePaging: true,
    supportsRunInTerminalRequest: false,
    supportsProgressReporting: false,
    supportsInvalidatedEvent: true,
  });

  console.log('[Background] Initialize response:', initResponse);

  // 2. Attach to running session
  const attachResponse = await sendDapRequest('attach', {});
  console.log('[Background] Attach response:', attachResponse);

  // 3. Configuration done
  const configResponse = await sendDapRequest('configurationDone', {});
  console.log('[Background] ConfigurationDone response:', configResponse);
}

/**
 * Send a DAP request and return a promise for the response
 */
function sendDapRequest(command, args = {}) {
  return new Promise((resolve, reject) => {
    if (!ws || ws.readyState !== WebSocket.OPEN) {
      reject(new Error('Not connected'));
      return;
    }

    const seq = sequenceNumber++;
    const request = {
      seq,
      type: 'request',
      command,
      arguments: args,
    };

    console.log(`[Background] Sending DAP request: ${command} (seq: ${seq})`);
    pendingRequests.set(seq, { resolve, reject, command });

    // Set timeout for request
    setTimeout(() => {
      if (pendingRequests.has(seq)) {
        pendingRequests.delete(seq);
        reject(new Error(`Request timed out: ${command}`));
      }
    }, 30000);

    ws.send(JSON.stringify(request));
  });
}

/**
 * Handle incoming DAP message (response or event)
 */
function handleDapMessage(message) {
  if (message.type === 'response') {
    handleDapResponse(message);
  } else if (message.type === 'event') {
    handleDapEvent(message);
  } else if (message.type === 'proxy-error') {
    console.error('[Background] Proxy error:', message.message);
    connectionStatus = 'error';
    broadcastStatus();
  }
}

/**
 * Handle DAP response
 */
function handleDapResponse(response) {
  const pending = pendingRequests.get(response.request_seq);
  if (!pending) {
    console.warn(`[Background] No pending request for seq ${response.request_seq}`);
    return;
  }

  pendingRequests.delete(response.request_seq);

  if (response.success) {
    console.log(`[Background] DAP response success: ${response.command}`);
    pending.resolve(response.body || {});
  } else {
    console.error(`[Background] DAP response error: ${response.command} - ${response.message}`);
    pending.reject(new Error(response.message || 'Unknown error'));
  }
}

/**
 * Handle DAP event
 */
function handleDapEvent(event) {
  console.log(`[Background] DAP event: ${event.event}`, event.body);

  switch (event.event) {
    case 'initialized':
      // DAP server is ready
      break;

    case 'stopped':
      connectionStatus = 'paused';
      broadcastStatus();
      break;

    case 'continued':
      connectionStatus = 'running';
      broadcastStatus();
      break;

    case 'terminated':
      connectionStatus = 'disconnected';
      broadcastStatus();
      break;

    case 'output':
      // Output event - forward to content scripts
      break;
  }

  // Broadcast event to all content scripts
  broadcastEvent(event);
}

/**
 * Broadcast connection status to popup and content scripts
 */
function broadcastStatus() {
  // Broadcast to all extension contexts
  chrome.runtime.sendMessage({ type: 'status-changed', status: connectionStatus }).catch(() => {});

  // Broadcast to content scripts
  chrome.tabs.query({ url: 'https://github.com/*/*/actions/runs/*/job/*' }, (tabs) => {
    tabs.forEach((tab) => {
      chrome.tabs.sendMessage(tab.id, { type: 'status-changed', status: connectionStatus }).catch(() => {});
    });
  });
}

/**
 * Broadcast DAP event to content scripts
 */
function broadcastEvent(event) {
  chrome.tabs.query({ url: 'https://github.com/*/*/actions/runs/*/job/*' }, (tabs) => {
    tabs.forEach((tab) => {
      chrome.tabs.sendMessage(tab.id, { type: 'dap-event', event }).catch(() => {});
    });
  });
}

/**
 * Message handler for requests from popup and content scripts
 */
chrome.runtime.onMessage.addListener((message, sender, sendResponse) => {
  console.log('[Background] Received message:', message.type);

  switch (message.type) {
    case 'get-status':
      sendResponse({ status: connectionStatus });
      return false;

    case 'connect':
      connect(message.url || DEFAULT_URL);
      sendResponse({ status: connectionStatus });
      return false;

    case 'disconnect':
      disconnect();
      sendResponse({ status: connectionStatus });
      return false;

    case 'dap-request':
      // Handle DAP request from content script
      sendDapRequest(message.command, message.args || {})
        .then((body) => {
          sendResponse({ success: true, body });
        })
        .catch((error) => {
          sendResponse({ success: false, error: error.message });
        });
      return true; // Will respond asynchronously

    default:
      console.warn('[Background] Unknown message type:', message.type);
      return false;
  }
});

// Log startup
console.log('[Background] Actions DAP Debugger background script loaded');
