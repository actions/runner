/**
 * Content Script - Debugger UI
 *
 * Injects the debugger pane into GitHub Actions job pages and handles
 * all UI interactions.
 */

// State
let debuggerPane = null;
let currentFrameId = 0;
let isConnected = false;
let replHistory = [];
let replHistoryIndex = -1;

// HTML escape helper
function escapeHtml(text) {
  const div = document.createElement('div');
  div.textContent = text;
  return div.innerHTML;
}

/**
 * Strip result indicator suffix from step name
 * e.g., "Run tests [running]" -> "Run tests"
 */
function stripResultIndicator(name) {
  return name.replace(/\s*\[(running|success|failure|skipped|cancelled)\]$/i, '');
}

/**
 * Send DAP request to background script
 */
function sendDapRequest(command, args = {}) {
  return new Promise((resolve, reject) => {
    chrome.runtime.sendMessage({ type: 'dap-request', command, args }, (response) => {
      if (chrome.runtime.lastError) {
        reject(new Error(chrome.runtime.lastError.message));
      } else if (response && response.success) {
        resolve(response.body);
      } else {
        reject(new Error(response?.error || 'Unknown error'));
      }
    });
  });
}

/**
 * Build map of steps from DOM
 */
function buildStepMap() {
  const steps = document.querySelectorAll('check-step');
  const map = new Map();
  steps.forEach((el, idx) => {
    map.set(idx, {
      element: el,
      number: parseInt(el.dataset.number),
      name: el.dataset.name,
      conclusion: el.dataset.conclusion,
      externalId: el.dataset.externalId,
    });
  });
  return map;
}

/**
 * Find step element by name
 */
function findStepByName(stepName) {
  return document.querySelector(`check-step[data-name="${CSS.escape(stepName)}"]`);
}

/**
 * Find step element by number
 */
function findStepByNumber(stepNumber) {
  return document.querySelector(`check-step[data-number="${stepNumber}"]`);
}

/**
 * Get all step elements
 */
function getAllSteps() {
  return document.querySelectorAll('check-step');
}

/**
 * Create the debugger pane HTML
 */
function createDebuggerPaneHTML() {
  return `
    <div class="dap-header d-flex flex-items-center p-2 border-bottom">
      <svg class="octicon mr-2" viewBox="0 0 16 16" width="16" height="16">
        <path fill="currentColor" d="M4.72.22a.75.75 0 0 1 1.06 0l1 1a.75.75 0 0 1-1.06 1.06l-.22-.22-.22.22a.75.75 0 0 1-1.06-1.06l1-1Z"/>
        <path fill="currentColor" d="M11.28.22a.75.75 0 0 0-1.06 0l-1 1a.75.75 0 0 0 1.06 1.06l.22-.22.22.22a.75.75 0 0 0 1.06-1.06l-1-1Z"/>
        <path fill="currentColor" d="M8 4a4 4 0 0 0-4 4v1h1v2.5a2.5 2.5 0 0 0 2.5 2.5h1a2.5 2.5 0 0 0 2.5-2.5V9h1V8a4 4 0 0 0-4-4Z"/>
        <path fill="currentColor" d="M5 9H3.5a.5.5 0 0 0-.5.5v2a.5.5 0 0 0 .5.5H5V9ZM11 9h1.5a.5.5 0 0 1 .5.5v2a.5.5 0 0 1-.5.5H11V9Z"/>
      </svg>
      <span class="text-bold">Debugger</span>
      <span class="dap-step-info color-fg-muted ml-2">Connecting...</span>
      <span class="Label dap-status-label ml-auto">CONNECTING</span>
    </div>
    
    <div class="dap-content d-flex" style="height: 300px;">
      <!-- Scopes Panel -->
      <div class="dap-scopes border-right overflow-auto" style="width: 33%;">
        <div class="dap-scope-header p-2 text-bold border-bottom">Variables</div>
        <div class="dap-scope-tree p-2">
          <div class="color-fg-muted">Connect to view variables</div>
        </div>
      </div>
      
      <!-- REPL Console -->
      <div class="dap-repl d-flex flex-column" style="width: 67%;">
        <div class="dap-repl-header p-2 text-bold border-bottom">Console</div>
        <div class="dap-repl-output overflow-auto flex-auto p-2 text-mono text-small">
          <div class="color-fg-muted">Welcome to Actions DAP Debugger</div>
          <div class="color-fg-muted">Enter expressions like: \${{ github.ref }}</div>
          <div class="color-fg-muted">Or shell commands: !ls -la</div>
        </div>
        <div class="dap-repl-input border-top p-2">
          <input type="text" class="form-control input-sm text-mono" 
                 placeholder="Enter expression or !command" disabled>
        </div>
      </div>
    </div>
    
    <!-- Control buttons -->
    <div class="dap-controls d-flex flex-items-center p-2 border-top">
      <button class="btn btn-sm mr-2" data-action="reverseContinue" title="Reverse Continue (go to first checkpoint)" disabled>
        <svg viewBox="0 0 16 16" width="16" height="16"><path fill="currentColor" d="M2 2v12h2V8.5l5 4V8.5l5 4V2.5l-5 4V2.5l-5 4V2z"/></svg>
      </button>
      <button class="btn btn-sm mr-2" data-action="stepBack" title="Step Back" disabled>
        <svg viewBox="0 0 16 16" width="16" height="16"><path fill="currentColor" d="M2 2v12h2V2H2zm3 6 7 5V3L5 8z"/></svg>
      </button>
      <button class="btn btn-sm btn-primary mr-2" data-action="continue" title="Continue" disabled>
        <svg viewBox="0 0 16 16" width="16" height="16"><path fill="currentColor" d="M4 2l10 6-10 6z"/></svg>
      </button>
      <button class="btn btn-sm mr-2" data-action="next" title="Step to Next" disabled>
        <svg viewBox="0 0 16 16" width="16" height="16"><path fill="currentColor" d="M2 3l7 5-7 5V3zm7 5l5 0V2h2v12h-2V8.5l-5 0z"/></svg>
      </button>
      <span class="dap-step-counter color-fg-muted ml-auto text-small">
        Not connected
      </span>
    </div>
  `;
}

/**
 * Inject debugger pane into the page
 */
function injectDebuggerPane() {
  // Remove existing pane if any
  const existing = document.querySelector('.dap-debugger-pane');
  if (existing) existing.remove();

  // Find where to inject
  const stepsContainer = document.querySelector('check-steps');
  if (!stepsContainer) {
    console.warn('[Content] No check-steps container found');
    return null;
  }

  // Create pane
  const pane = document.createElement('div');
  pane.className = 'dap-debugger-pane mx-2 mb-2 border rounded-2';
  pane.innerHTML = createDebuggerPaneHTML();

  // Insert at the top of steps container
  stepsContainer.insertBefore(pane, stepsContainer.firstChild);

  // Setup event handlers
  setupPaneEventHandlers(pane);

  debuggerPane = pane;
  return pane;
}

/**
 * Move debugger pane to before a specific step
 */
function moveDebuggerPane(stepElement, stepName) {
  if (!debuggerPane || !stepElement) return;

  // Move the pane
  stepElement.parentNode.insertBefore(debuggerPane, stepElement);

  // Update step info
  const stepInfo = debuggerPane.querySelector('.dap-step-info');
  if (stepInfo) {
    stepInfo.textContent = `Paused before: ${stepName}`;
  }
}

/**
 * Setup event handlers for debugger pane
 */
function setupPaneEventHandlers(pane) {
  // Control buttons
  pane.querySelectorAll('[data-action]').forEach((btn) => {
    btn.addEventListener('click', async () => {
      const action = btn.dataset.action;
      enableControls(false);
      updateStatus('RUNNING');

      try {
        await sendDapRequest(action, { threadId: 1 });
      } catch (error) {
        console.error(`[Content] DAP ${action} failed:`, error);
        appendOutput(`Error: ${error.message}`, 'error');
        enableControls(true);
        updateStatus('ERROR');
      }
    });
  });

  // REPL input
  const input = pane.querySelector('.dap-repl-input input');
  if (input) {
    input.addEventListener('keydown', handleReplKeydown);
  }
}

/**
 * Handle REPL input keydown
 */
async function handleReplKeydown(e) {
  const input = e.target;

  if (e.key === 'Enter') {
    const command = input.value.trim();
    if (!command) return;

    replHistory.push(command);
    replHistoryIndex = replHistory.length;
    input.value = '';

    // Show command
    appendOutput(`> ${command}`, 'input');

    // Send to DAP
    try {
      const response = await sendDapRequest('evaluate', {
        expression: command,
        frameId: currentFrameId,
        context: command.startsWith('!') ? 'repl' : 'watch',
      });
      // Only show result if it's NOT an exit code summary
      // (shell command output is already streamed via output events)
      if (response.result && !/^\(exit code: -?\d+\)$/.test(response.result)) {
        appendOutput(response.result, 'result');
      }
    } catch (error) {
      appendOutput(error.message, 'error');
    }
  } else if (e.key === 'ArrowUp') {
    if (replHistoryIndex > 0) {
      replHistoryIndex--;
      input.value = replHistory[replHistoryIndex];
    }
    e.preventDefault();
  } else if (e.key === 'ArrowDown') {
    if (replHistoryIndex < replHistory.length - 1) {
      replHistoryIndex++;
      input.value = replHistory[replHistoryIndex];
    } else {
      replHistoryIndex = replHistory.length;
      input.value = '';
    }
    e.preventDefault();
  }
}

/**
 * Append output to REPL console
 */
function appendOutput(text, type) {
  const output = document.querySelector('.dap-repl-output');
  if (!output) return;

  const line = document.createElement('div');
  line.className = `dap-output-${type}`;
  if (type === 'error') line.classList.add('color-fg-danger');
  if (type === 'input') line.classList.add('color-fg-muted');

  // Handle multi-line output
  const lines = text.split('\n');
  lines.forEach((l, i) => {
    if (i > 0) {
      output.appendChild(document.createElement('br'));
    }
    const span = document.createElement('span');
    span.textContent = l;
    if (i === 0) {
      span.className = line.className;
    }
    output.appendChild(span);
  });

  if (lines.length === 1) {
    line.textContent = text;
    output.appendChild(line);
  }

  output.scrollTop = output.scrollHeight;
}

/**
 * Enable/disable control buttons
 */
function enableControls(enabled) {
  if (!debuggerPane) return;

  debuggerPane.querySelectorAll('.dap-controls button').forEach((btn) => {
    btn.disabled = !enabled;
  });

  const input = debuggerPane.querySelector('.dap-repl-input input');
  if (input) {
    input.disabled = !enabled;
  }
}

/**
 * Update status display
 */
function updateStatus(status, extra) {
  if (!debuggerPane) return;

  const label = debuggerPane.querySelector('.dap-status-label');
  if (label) {
    label.textContent = status;
    label.className = 'Label dap-status-label ml-auto ';

    switch (status) {
      case 'PAUSED':
        label.classList.add('Label--attention');
        break;
      case 'RUNNING':
        label.classList.add('Label--success');
        break;
      case 'TERMINATED':
      case 'DISCONNECTED':
        label.classList.add('Label--secondary');
        break;
      case 'ERROR':
        label.classList.add('Label--danger');
        break;
      default:
        label.classList.add('Label--secondary');
    }
  }

  // Update step counter if extra info provided
  if (extra) {
    const counter = debuggerPane.querySelector('.dap-step-counter');
    if (counter) {
      counter.textContent = extra;
    }
  }
}

/**
 * Load scopes for current frame
 */
async function loadScopes(frameId) {
  const scopesContainer = document.querySelector('.dap-scope-tree');
  if (!scopesContainer) return;

  scopesContainer.innerHTML = '<div class="color-fg-muted">Loading...</div>';

  try {
    console.log('[Content] Loading scopes for frame:', frameId);
    const response = await sendDapRequest('scopes', { frameId });
    console.log('[Content] Scopes response:', response);

    scopesContainer.innerHTML = '';

    if (!response.scopes || response.scopes.length === 0) {
      scopesContainer.innerHTML = '<div class="color-fg-muted">No scopes available</div>';
      return;
    }

    for (const scope of response.scopes) {
      console.log('[Content] Creating tree node for scope:', scope.name, 'variablesRef:', scope.variablesReference);
      // Only mark as expandable if variablesReference > 0
      const isExpandable = scope.variablesReference > 0;
      const node = createTreeNode(scope.name, scope.variablesReference, isExpandable);
      scopesContainer.appendChild(node);
    }
  } catch (error) {
    console.error('[Content] Failed to load scopes:', error);
    scopesContainer.innerHTML = `<div class="color-fg-danger">Error: ${escapeHtml(error.message)}</div>`;
  }
}

/**
 * Create a tree node for scope/variable display
 */
function createTreeNode(name, variablesReference, isExpandable, value) {
  const node = document.createElement('div');
  node.className = 'dap-tree-node';
  node.dataset.variablesRef = variablesReference;

  const content = document.createElement('div');
  content.className = 'dap-tree-content';

  // Expand icon
  const expandIcon = document.createElement('span');
  expandIcon.className = 'dap-expand-icon';
  expandIcon.textContent = isExpandable ? '\u25B6' : ' '; // ▶ or space
  content.appendChild(expandIcon);

  // Name
  const nameSpan = document.createElement('span');
  nameSpan.className = 'text-bold';
  nameSpan.textContent = name;
  content.appendChild(nameSpan);

  // Value (if provided)
  if (value !== undefined) {
    const valueSpan = document.createElement('span');
    valueSpan.className = 'color-fg-muted';
    valueSpan.textContent = `: ${value}`;
    content.appendChild(valueSpan);
  }

  node.appendChild(content);

  if (isExpandable && variablesReference > 0) {
    content.style.cursor = 'pointer';
    content.addEventListener('click', () => toggleTreeNode(node));
  }

  return node;
}

/**
 * Toggle tree node expansion
 */
async function toggleTreeNode(node) {
  const children = node.querySelector('.dap-tree-children');
  const expandIcon = node.querySelector('.dap-expand-icon');

  if (children) {
    // Toggle visibility
    children.hidden = !children.hidden;
    expandIcon.textContent = children.hidden ? '\u25B6' : '\u25BC'; // ▶ or ▼
    return;
  }

  // Fetch children
  const variablesRef = parseInt(node.dataset.variablesRef);
  if (!variablesRef) return;

  expandIcon.textContent = '...';

  try {
    const response = await sendDapRequest('variables', { variablesReference: variablesRef });

    const childContainer = document.createElement('div');
    childContainer.className = 'dap-tree-children ml-3';

    for (const variable of response.variables) {
      const hasChildren = variable.variablesReference > 0;
      const childNode = createTreeNode(
        variable.name,
        variable.variablesReference,
        hasChildren,
        variable.value
      );
      childContainer.appendChild(childNode);
    }

    node.appendChild(childContainer);
    expandIcon.textContent = '\u25BC'; // ▼
  } catch (error) {
    console.error('[Content] Failed to load variables:', error);
    expandIcon.textContent = '\u25B6'; // ▶
  }
}

/**
 * Handle stopped event from DAP
 */
async function handleStoppedEvent(body) {
  console.log('[Content] Stopped event:', body);

  isConnected = true;
  updateStatus('PAUSED', body.reason || 'paused');
  enableControls(true);

  // Get current location
  try {
    const stackTrace = await sendDapRequest('stackTrace', { threadId: 1 });

    if (stackTrace.stackFrames && stackTrace.stackFrames.length > 0) {
      const currentFrame = stackTrace.stackFrames[0];
      currentFrameId = currentFrame.id;

      // Strip result indicator from step name for DOM lookup
      // e.g., "Run tests [running]" -> "Run tests"
      const rawStepName = stripResultIndicator(currentFrame.name);
      let stepElement = findStepByName(rawStepName);

      if (!stepElement) {
        // Fallback: use step index
        // Note: GitHub Actions UI shows "Set up job" at index 0, which is not a real workflow step
        // DAP uses 1-based frame IDs, so frame ID 1 maps to UI step index 1 (skipping "Set up job")
        const steps = getAllSteps();
        const adjustedIndex = currentFrame.id; // 1-based, happens to match after skipping "Set up job"
        if (adjustedIndex > 0 && adjustedIndex < steps.length) {
          stepElement = steps[adjustedIndex];
        }
      }

      if (stepElement) {
        moveDebuggerPane(stepElement, rawStepName);
      }

      // Update step counter
      const counter = debuggerPane?.querySelector('.dap-step-counter');
      if (counter) {
        counter.textContent = `Step ${currentFrame.id} of ${stackTrace.stackFrames.length}`;
      }

      // Load scopes
      await loadScopes(currentFrame.id);
    }
  } catch (error) {
    console.error('[Content] Failed to get stack trace:', error);
    appendOutput(`Error: ${error.message}`, 'error');
  }
}

/**
 * Handle output event from DAP
 */
function handleOutputEvent(body) {
  if (body.output) {
    const category = body.category === 'stderr' ? 'error' : 'stdout';
    appendOutput(body.output.trimEnd(), category);
  }
}

/**
 * Handle terminated event from DAP
 */
function handleTerminatedEvent() {
  isConnected = false;
  updateStatus('TERMINATED');
  enableControls(false);

  const stepInfo = debuggerPane?.querySelector('.dap-step-info');
  if (stepInfo) {
    stepInfo.textContent = 'Session ended';
  }
}

/**
 * Handle status change from background
 */
function handleStatusChange(status) {
  console.log('[Content] Status changed:', status);

  switch (status) {
    case 'connected':
      isConnected = true;
      updateStatus('CONNECTED');
      const stepInfo = debuggerPane?.querySelector('.dap-step-info');
      if (stepInfo) {
        stepInfo.textContent = 'Waiting for debug event...';
      }
      break;

    case 'paused':
      isConnected = true;
      updateStatus('PAUSED');
      enableControls(true);
      break;

    case 'running':
      isConnected = true;
      updateStatus('RUNNING');
      enableControls(false);
      break;

    case 'disconnected':
      isConnected = false;
      updateStatus('DISCONNECTED');
      enableControls(false);
      break;

    case 'error':
      isConnected = false;
      updateStatus('ERROR');
      enableControls(false);
      break;
  }
}

/**
 * Listen for messages from background script
 */
chrome.runtime.onMessage.addListener((message, sender, sendResponse) => {
  console.log('[Content] Received message:', message.type);

  switch (message.type) {
    case 'dap-event':
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
      break;

    case 'status-changed':
      handleStatusChange(message.status);
      break;
  }
});

/**
 * Inject debug button into GitHub Actions UI header
 */
function injectDebugButton() {
  const container = document.querySelector('.js-check-run-search');
  if (!container || container.querySelector('.dap-debug-btn-container')) {
    return; // Already injected or container not found
  }

  const buttonContainer = document.createElement('div');
  buttonContainer.className = 'ml-2 dap-debug-btn-container';
  buttonContainer.innerHTML = `
    <button type="button" class="btn btn-sm dap-debug-btn" title="Toggle DAP Debugger">
      <svg viewBox="0 0 16 16" width="16" height="16" class="octicon mr-1" style="vertical-align: text-bottom;">
        <path fill="currentColor" d="M4.72.22a.75.75 0 0 1 1.06 0l1 1a.75.75 0 0 1-1.06 1.06l-.22-.22-.22.22a.75.75 0 0 1-1.06-1.06l1-1Z"/>
        <path fill="currentColor" d="M11.28.22a.75.75 0 0 0-1.06 0l-1 1a.75.75 0 0 0 1.06 1.06l.22-.22.22.22a.75.75 0 0 0 1.06-1.06l-1-1Z"/>
        <path fill="currentColor" d="M8 4a4 4 0 0 0-4 4v1h1v2.5a2.5 2.5 0 0 0 2.5 2.5h1a2.5 2.5 0 0 0 2.5-2.5V9h1V8a4 4 0 0 0-4-4Z"/>
        <path fill="currentColor" d="M5 9H3.5a.5.5 0 0 0-.5.5v2a.5.5 0 0 0 .5.5H5V9ZM11 9h1.5a.5.5 0 0 1 .5.5v2a.5.5 0 0 1-.5.5H11V9Z"/>
      </svg>
      Debug
    </button>
  `;

  const button = buttonContainer.querySelector('button');
  button.addEventListener('click', () => {
    let pane = document.querySelector('.dap-debugger-pane');
    if (pane) {
      // Toggle visibility
      pane.hidden = !pane.hidden;
      button.classList.toggle('selected', !pane.hidden);
    } else {
      // Create and show pane
      pane = injectDebuggerPane();
      if (pane) {
        button.classList.add('selected');
        // Check connection status after creating pane
        chrome.runtime.sendMessage({ type: 'get-status' }, (response) => {
          if (response && response.status) {
            handleStatusChange(response.status);
          }
        });
      }
    }
  });

  // Insert at the beginning of the container
  container.insertBefore(buttonContainer, container.firstChild);
  console.log('[Content] Debug button injected');
}

/**
 * Initialize content script
 */
function init() {
  console.log('[Content] Actions DAP Debugger content script loaded');

  // Check if we're on a job page
  const steps = getAllSteps();
  if (steps.length === 0) {
    console.log('[Content] No steps found, waiting for DOM...');
    // Wait for steps to appear
    const observer = new MutationObserver((mutations) => {
      const steps = getAllSteps();
      if (steps.length > 0) {
        observer.disconnect();
        console.log('[Content] Steps found, injecting debug button');
        injectDebugButton();
      }
    });
    observer.observe(document.body, { childList: true, subtree: true });
    return;
  }

  // Inject debug button in header (user can click to show debugger pane)
  injectDebugButton();

  // Check current connection status
  chrome.runtime.sendMessage({ type: 'get-status' }, (response) => {
    if (response && response.status) {
      handleStatusChange(response.status);
      // If already connected/paused, auto-show the debugger pane
      if (response.status === 'paused' || response.status === 'connected') {
        const pane = document.querySelector('.dap-debugger-pane');
        if (!pane) {
          injectDebuggerPane();
          const btn = document.querySelector('.dap-debug-btn');
          if (btn) btn.classList.add('selected');
        }
      }
    }
  });
}

// Initialize when DOM is ready
if (document.readyState === 'loading') {
  document.addEventListener('DOMContentLoaded', init);
} else {
  init();
}
