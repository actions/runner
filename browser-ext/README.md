# Actions DAP Debugger - Browser Extension

A Chrome extension that enables interactive debugging of GitHub Actions workflows directly in the browser. Connects to the runner's DAP server via a WebSocket proxy.

## Features

- **Variable Inspection**: Browse workflow context variables (`github`, `env`, `steps`, etc.)
- **REPL Console**: Evaluate expressions and run shell commands
- **Step Control**: Step forward, step back, continue, and reverse continue
- **GitHub Integration**: Debugger pane injects directly into the job page

## Quick Start

### 1. Start the WebSocket Proxy

The proxy bridges WebSocket connections from the browser to the DAP TCP server.

```bash
cd browser-ext/proxy
npm install
node proxy.js
```

The proxy listens on `ws://localhost:4712` and connects to the DAP server at `tcp://localhost:4711`.

### 2. Load the Extension in Chrome

1. Open Chrome and navigate to `chrome://extensions/`
2. Enable "Developer mode" (toggle in top right)
3. Click "Load unpacked"
4. Select the `browser-ext` directory

### 3. Start a Debug Session

1. Go to your GitHub repository
2. Navigate to Actions and select a workflow run
3. Click "Re-run jobs" → check "Enable debug logging"
4. Wait for the runner to display "DAP debugger waiting for connection..."

### 4. Connect the Extension

1. Navigate to the job page (`github.com/.../actions/runs/.../job/...`)
2. Click the extension icon in Chrome toolbar
3. Click "Connect"
4. The debugger pane will appear above the first workflow step

## Usage

### Variable Browser (Left Panel)

Click on scope names to expand and view variables:
- **Globals**: `github`, `env`, `runner` contexts
- **Job Outputs**: Outputs from previous jobs
- **Step Outputs**: Outputs from previous steps

### Console (Right Panel)

Enter expressions or commands:

```bash
# Evaluate expressions
${{ github.ref }}
${{ github.event_name }}
${{ env.MY_VAR }}

# Run shell commands (prefix with !)
!ls -la
!cat package.json
!env | grep GITHUB

# Modify variables
!export MY_VAR=new_value
```

### Control Buttons

| Button | Action | Description |
|--------|--------|-------------|
| ⏮ | Reverse Continue | Go back to first checkpoint |
| ◀ | Step Back | Go to previous checkpoint |
| ▶ | Continue | Run until next breakpoint/end |
| ⏭ | Step (Next) | Step to next workflow step |

## Architecture

```
Browser Extension ──WebSocket──► Proxy ──TCP──► Runner DAP Server
    (port 4712)                              (port 4711)
```

The WebSocket proxy handles DAP message framing (Content-Length headers) and provides a browser-compatible connection.

## Configuration

### Proxy Settings

| Environment Variable | Default | Description |
|---------------------|---------|-------------|
| `WS_PORT` | 4712 | WebSocket server port |
| `DAP_HOST` | 127.0.0.1 | DAP server host |
| `DAP_PORT` | 4711 | DAP server port |

Or use CLI arguments:
```bash
node proxy.js --ws-port 4712 --dap-host 127.0.0.1 --dap-port 4711
```

### Extension Settings

Click the extension popup to configure:
- **Proxy Host**: Default `localhost`
- **Proxy Port**: Default `4712`

## File Structure

```
browser-ext/
├── manifest.json           # Extension configuration
├── background/
│   └── background.js       # Service worker - DAP client
├── content/
│   ├── content.js          # UI injection and interaction
│   └── content.css         # Debugger pane styling
├── popup/
│   ├── popup.html          # Extension popup UI
│   ├── popup.js            # Popup logic
│   └── popup.css           # Popup styling
├── lib/
│   └── dap-protocol.js     # DAP message helpers
├── proxy/
│   ├── proxy.js            # WebSocket-to-TCP bridge
│   └── package.json        # Proxy dependencies
└── icons/
    ├── icon16.png
    ├── icon48.png
    └── icon128.png
```

## Troubleshooting

### "Failed to connect to DAP server"

1. Ensure the proxy is running: `node proxy.js`
2. Ensure the runner is waiting for a debugger connection
3. Check that debug logging is enabled for the job

### Debugger pane doesn't appear

1. Verify you're on a job page (`/actions/runs/*/job/*`)
2. Open DevTools and check for console errors
3. Reload the page after loading the extension

### Variables don't load

1. Wait for the "stopped" event (status shows PAUSED)
2. Click on a scope to expand it
3. Check the console for error messages

## Development

### Modifying the Extension

After making changes:
1. Go to `chrome://extensions/`
2. Click the refresh icon on the extension card
3. Reload the GitHub job page

### Debugging

- **Background script**: Inspect via `chrome://extensions/` → "Inspect views: service worker"
- **Content script**: Use DevTools on the GitHub page
- **Proxy**: Watch terminal output for message logs

## Security Note

The proxy and extension are designed for local development. The proxy only accepts connections from localhost. Do not expose the proxy to the network without additional security measures.
