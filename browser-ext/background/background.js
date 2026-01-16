/**
 * Background Script - DAP Client
 *
 * Service worker that manages WebSocket connection to the proxy
 * and handles DAP protocol communication.
 *
 * NOTE: Chrome MV3 service workers can be terminated after ~30s of inactivity.
 * We handle this with:
 * 1. Keepalive pings to keep the WebSocket active
 * 2. Automatic reconnection when the service worker restarts
 * 3. Storing connection state in chrome.storage.session
 */

// Connection state
let ws = null;
let connectionStatus = 'disconnected'; // disconnected, connecting, connected, paused, running, error
let sequenceNumber = 1;
const pendingRequests = new Map(); // seq -> { resolve, reject, command, timeout }

// Reconnection state
let reconnectAttempts = 0;
const MAX_RECONNECT_ATTEMPTS = 10;
const RECONNECT_BASE_DELAY = 1000; // Start with 1s, exponential backoff
let reconnectTimer = null;
let lastConnectedUrl = null;
let wasConnectedBeforeIdle = false;

// Keepalive interval - send ping every 15s to keep service worker AND WebSocket alive
// Chrome MV3 service workers get suspended after ~30s of inactivity
// We need to send actual WebSocket messages to keep both alive
const KEEPALIVE_INTERVAL = 15000;
let keepaliveTimer = null;

// Default configuration
const DEFAULT_URL = 'ws://localhost:4712';

/**
 * Initialize on service worker startup - check if we should reconnect
 */
async function initializeOnStartup() {
  console.log('[Background] Service worker starting up...');

  try {
    // Restore state from session storage
    const data = await chrome.storage.session.get(['connectionUrl', 'shouldBeConnected', 'lastStatus']);

    if (data.shouldBeConnected && data.connectionUrl) {
      console.log('[Background] Restoring connection after service worker restart');
      lastConnectedUrl = data.connectionUrl;
      wasConnectedBeforeIdle = true;

      // Small delay to let things settle
      setTimeout(() => {
        connect(data.connectionUrl);
      }, 500);
    }
  } catch (e) {
    console.log('[Background] No session state to restore');
  }
}

/**
 * Save connection state to session storage (survives service worker restart)
 */
async function saveConnectionState() {
  try {
    await chrome.storage.session.set({
      connectionUrl: lastConnectedUrl,
      shouldBeConnected: connectionStatus !== 'disconnected' && connectionStatus !== 'error',
      lastStatus: connectionStatus,
    });
  } catch (e) {
    console.warn('[Background] Failed to save connection state:', e);
  }
}

/**
 * Clear connection state from session storage
 */
async function clearConnectionState() {
  try {
    await chrome.storage.session.remove(['connectionUrl', 'shouldBeConnected', 'lastStatus']);
  } catch (e) {
    console.warn('[Background] Failed to clear connection state:', e);
  }
}

/**
 * Start keepalive ping to prevent service worker termination
 * CRITICAL: We must send actual WebSocket messages to keep the connection alive.
 * Just having a timer is not enough - Chrome will suspend the service worker
 * and close the WebSocket with code 1001 after ~30s of inactivity.
 */
function startKeepalive() {
  stopKeepalive();

  keepaliveTimer = setInterval(() => {
    if (ws && ws.readyState === WebSocket.OPEN) {
      try {
        // Send a lightweight keepalive message over WebSocket
        // This does two things:
        // 1. Keeps the WebSocket connection active (prevents proxy timeout)
        // 2. Creates activity that keeps the Chrome service worker alive
        const keepaliveMsg = JSON.stringify({ type: 'keepalive', timestamp: Date.now() });
        ws.send(keepaliveMsg);
        console.log('[Background] Keepalive sent');
      } catch (e) {
        console.error('[Background] Keepalive error:', e);
        handleUnexpectedClose();
      }
    } else if (wasConnectedBeforeIdle || lastConnectedUrl) {
      // Connection was lost, try to reconnect
      console.log('[Background] Connection lost during keepalive check');
      handleUnexpectedClose();
    }
  }, KEEPALIVE_INTERVAL);

  console.log('[Background] Keepalive timer started (interval: ' + KEEPALIVE_INTERVAL + 'ms)');
}

/**
 * Stop keepalive ping
 */
function stopKeepalive() {
  if (keepaliveTimer) {
    clearInterval(keepaliveTimer);
    keepaliveTimer = null;
    console.log('[Background] Keepalive timer stopped');
  }
}

/**
 * Handle unexpected connection close - attempt reconnection
 */
function handleUnexpectedClose() {
  if (reconnectTimer) {
    return; // Already trying to reconnect
  }

  if (!lastConnectedUrl) {
    console.log('[Background] No URL to reconnect to');
    return;
  }

  if (reconnectAttempts >= MAX_RECONNECT_ATTEMPTS) {
    console.error('[Background] Max reconnection attempts reached');
    connectionStatus = 'error';
    broadcastStatus();
    clearConnectionState();
    return;
  }

  const delay = Math.min(RECONNECT_BASE_DELAY * Math.pow(2, reconnectAttempts), 30000);
  reconnectAttempts++;

  console.log(`[Background] Scheduling reconnect attempt ${reconnectAttempts}/${MAX_RECONNECT_ATTEMPTS} in ${delay}ms`);
  connectionStatus = 'connecting';
  broadcastStatus();

  reconnectTimer = setTimeout(() => {
    reconnectTimer = null;
    if (connectionStatus !== 'connected' && connectionStatus !== 'paused' && connectionStatus !== 'running') {
      connect(lastConnectedUrl);
    }
  }, delay);
}

/**
 * Connect to the WebSocket proxy
 */
function connect(url) {
  // Clean up existing connection
  if (ws) {
    try {
      ws.onclose = null; // Prevent triggering reconnect
      ws.close(1000, 'Reconnecting');
    } catch (e) {
      // Ignore
    }
    ws = null;
  }

  // Clear any pending reconnect
  if (reconnectTimer) {
    clearTimeout(reconnectTimer);
    reconnectTimer = null;
  }

  connectionStatus = 'connecting';
  broadcastStatus();

  // Use provided URL or default
  const wsUrl = url || DEFAULT_URL;
  lastConnectedUrl = wsUrl;
  console.log(`[Background] Connecting to ${wsUrl}`);

  try {
    ws = new WebSocket(wsUrl);
  } catch (e) {
    console.error('[Background] Failed to create WebSocket:', e);
    connectionStatus = 'error';
    broadcastStatus();
    handleUnexpectedClose();
    return;
  }

  ws.onopen = async () => {
    console.log('[Background] WebSocket connected');
    connectionStatus = 'connected';
    reconnectAttempts = 0; // Reset on successful connection
    wasConnectedBeforeIdle = true;
    broadcastStatus();
    saveConnectionState();
    startKeepalive();

    // Initialize DAP session
    try {
      await initializeDapSession();
    } catch (error) {
      console.error('[Background] Failed to initialize DAP session:', error);
      // Don't set error status - the connection might still be usable
      // The DAP server might just need the job to progress
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
    console.log(`[Background] WebSocket closed: ${event.code} ${event.reason || '(no reason)'}`);
    ws = null;
    stopKeepalive();

    // Reject any pending requests
    for (const [seq, pending] of pendingRequests) {
      if (pending.timeout) clearTimeout(pending.timeout);
      pending.reject(new Error('Connection closed'));
    }
    pendingRequests.clear();

    // Determine if we should reconnect
    // Code 1000 = normal closure (user initiated)
    // Code 1001 = going away (service worker idle, browser closing, etc.)
    // Code 1006 = abnormal closure (connection lost)
    // Code 1011 = server error
    const shouldReconnect = event.code !== 1000;

    if (shouldReconnect && wasConnectedBeforeIdle) {
      console.log('[Background] Unexpected close, will attempt reconnect');
      connectionStatus = 'connecting';
      broadcastStatus();
      handleUnexpectedClose();
    } else {
      connectionStatus = 'disconnected';
      wasConnectedBeforeIdle = false;
      broadcastStatus();
      clearConnectionState();
    }
  };

  ws.onerror = (event) => {
    console.error('[Background] WebSocket error:', event);
    // onclose will be called after onerror, so we handle reconnection there
  };
}

/**
 * Disconnect from the WebSocket proxy
 */
function disconnect() {
  // Stop any reconnection attempts
  if (reconnectTimer) {
    clearTimeout(reconnectTimer);
    reconnectTimer = null;
  }
  reconnectAttempts = 0;
  wasConnectedBeforeIdle = false;
  stopKeepalive();

  if (ws) {
    // Send disconnect request to DAP server first
    sendDapRequest('disconnect', {}).catch(() => {});

    // Prevent reconnection on this close
    const socket = ws;
    ws = null;
    socket.onclose = null;

    try {
      socket.close(1000, 'User disconnected');
    } catch (e) {
      // Ignore
    }
  }

  connectionStatus = 'disconnected';
  broadcastStatus();
  clearConnectionState();
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

    // Set timeout for request
    const timeout = setTimeout(() => {
      if (pendingRequests.has(seq)) {
        pendingRequests.delete(seq);
        reject(new Error(`Request timed out: ${command}`));
      }
    }, 30000);

    pendingRequests.set(seq, { resolve, reject, command, timeout });

    try {
      ws.send(JSON.stringify(request));
    } catch (e) {
      pendingRequests.delete(seq);
      clearTimeout(timeout);
      reject(new Error(`Failed to send request: ${e.message}`));
    }
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
    // Don't immediately set error status - might be transient
  } else if (message.type === 'keepalive-ack') {
    // Keepalive acknowledged by proxy - connection is healthy
    console.log('[Background] Keepalive acknowledged');
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
  if (pending.timeout) clearTimeout(pending.timeout);

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
      saveConnectionState();
      break;

    case 'continued':
      connectionStatus = 'running';
      broadcastStatus();
      saveConnectionState();
      break;

    case 'terminated':
      connectionStatus = 'disconnected';
      wasConnectedBeforeIdle = false;
      broadcastStatus();
      clearConnectionState();
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
  const statusMessage = { type: 'status-changed', status: connectionStatus };

  // Broadcast to all extension contexts (popup)
  chrome.runtime.sendMessage(statusMessage).catch(() => {});

  // Broadcast to content scripts
  chrome.tabs.query({ url: 'https://github.com/*/*/actions/runs/*/job/*' }, (tabs) => {
    if (chrome.runtime.lastError) return;
    tabs.forEach((tab) => {
      chrome.tabs.sendMessage(tab.id, statusMessage).catch(() => {});
    });
  });
}

/**
 * Broadcast DAP event to content scripts
 */
function broadcastEvent(event) {
  chrome.tabs.query({ url: 'https://github.com/*/*/actions/runs/*/job/*' }, (tabs) => {
    if (chrome.runtime.lastError) return;
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
      sendResponse({ status: connectionStatus, reconnecting: reconnectTimer !== null });
      return false;

    case 'connect':
      reconnectAttempts = 0; // Reset attempts on manual connect
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

// Initialize on startup
initializeOnStartup();

// Log startup
console.log('[Background] Actions DAP Debugger background script loaded');
