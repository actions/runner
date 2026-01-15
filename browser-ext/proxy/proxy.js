/**
 * DAP WebSocket-to-TCP Proxy
 *
 * Bridges WebSocket connections from browser extensions to the DAP TCP server.
 * Handles DAP message framing (Content-Length headers).
 *
 * Usage: node proxy.js [--ws-port 4712] [--dap-host 127.0.0.1] [--dap-port 4711]
 */

const WebSocket = require('ws');
const net = require('net');

// Configuration (can be overridden via CLI args)
const config = {
  wsPort: parseInt(process.env.WS_PORT) || 4712,
  dapHost: process.env.DAP_HOST || '127.0.0.1',
  dapPort: parseInt(process.env.DAP_PORT) || 4711,
};

// Parse CLI arguments
for (let i = 2; i < process.argv.length; i++) {
  switch (process.argv[i]) {
    case '--ws-port':
      config.wsPort = parseInt(process.argv[++i]);
      break;
    case '--dap-host':
      config.dapHost = process.argv[++i];
      break;
    case '--dap-port':
      config.dapPort = parseInt(process.argv[++i]);
      break;
  }
}

console.log(`[Proxy] Starting WebSocket-to-TCP proxy`);
console.log(`[Proxy] WebSocket: ws://localhost:${config.wsPort}`);
console.log(`[Proxy] DAP Server: tcp://${config.dapHost}:${config.dapPort}`);

const wss = new WebSocket.Server({ port: config.wsPort });

console.log(`[Proxy] WebSocket server listening on port ${config.wsPort}`);

wss.on('connection', (ws, req) => {
  const clientId = `${req.socket.remoteAddress}:${req.socket.remotePort}`;
  console.log(`[Proxy] WebSocket client connected: ${clientId}`);

  // Connect to DAP TCP server
  const tcp = net.createConnection({
    host: config.dapHost,
    port: config.dapPort,
  });

  let tcpBuffer = '';
  let tcpConnected = false;

  tcp.on('connect', () => {
    tcpConnected = true;
    console.log(`[Proxy] Connected to DAP server at ${config.dapHost}:${config.dapPort}`);
  });

  tcp.on('error', (err) => {
    console.error(`[Proxy] TCP error: ${err.message}`);
    if (ws.readyState === WebSocket.OPEN) {
      ws.send(
        JSON.stringify({
          type: 'proxy-error',
          message: `Failed to connect to DAP server: ${err.message}`,
        })
      );
      ws.close(1011, 'DAP server connection failed');
    }
  });

  tcp.on('close', () => {
    console.log(`[Proxy] TCP connection closed`);
    if (ws.readyState === WebSocket.OPEN) {
      ws.close(1000, 'DAP server disconnected');
    }
  });

  // WebSocket → TCP: Add Content-Length framing
  ws.on('message', (data) => {
    if (!tcpConnected) {
      console.warn(`[Proxy] TCP not connected, dropping message`);
      return;
    }

    const json = data.toString();
    try {
      // Validate it's valid JSON
      const parsed = JSON.parse(json);
      console.log(`[Proxy] WS→TCP: ${parsed.command || parsed.event || 'message'}`);

      // Add DAP framing
      const framed = `Content-Length: ${Buffer.byteLength(json)}\r\n\r\n${json}`;
      tcp.write(framed);
    } catch (err) {
      console.error(`[Proxy] Invalid JSON from WebSocket: ${err.message}`);
    }
  });

  // TCP → WebSocket: Parse Content-Length framing
  tcp.on('data', (chunk) => {
    tcpBuffer += chunk.toString();

    // Process complete DAP messages from buffer
    while (true) {
      // Look for Content-Length header
      const headerEnd = tcpBuffer.indexOf('\r\n\r\n');
      if (headerEnd === -1) break;

      const header = tcpBuffer.substring(0, headerEnd);
      const match = header.match(/Content-Length:\s*(\d+)/i);
      if (!match) {
        console.error(`[Proxy] Invalid DAP header: ${header}`);
        tcpBuffer = tcpBuffer.substring(headerEnd + 4);
        continue;
      }

      const contentLength = parseInt(match[1]);
      const messageStart = headerEnd + 4;
      const messageEnd = messageStart + contentLength;

      // Check if we have the complete message
      if (tcpBuffer.length < messageEnd) break;

      // Extract the JSON message
      const json = tcpBuffer.substring(messageStart, messageEnd);
      tcpBuffer = tcpBuffer.substring(messageEnd);

      // Send to WebSocket
      try {
        const parsed = JSON.parse(json);
        console.log(
          `[Proxy] TCP→WS: ${parsed.type} ${parsed.command || parsed.event || ''} ${parsed.request_seq ? `(req_seq: ${parsed.request_seq})` : ''}`
        );

        if (ws.readyState === WebSocket.OPEN) {
          ws.send(json);
        }
      } catch (err) {
        console.error(`[Proxy] Invalid JSON from TCP: ${err.message}`);
      }
    }
  });

  // Handle WebSocket close
  ws.on('close', (code, reason) => {
    console.log(`[Proxy] WebSocket closed: ${code} ${reason}`);
    tcp.end();
  });

  ws.on('error', (err) => {
    console.error(`[Proxy] WebSocket error: ${err.message}`);
    tcp.end();
  });
});

wss.on('error', (err) => {
  console.error(`[Proxy] WebSocket server error: ${err.message}`);
});

// Graceful shutdown
process.on('SIGINT', () => {
  console.log(`\n[Proxy] Shutting down...`);
  wss.clients.forEach((ws) => ws.close(1001, 'Server shutting down'));
  wss.close(() => {
    console.log(`[Proxy] Goodbye!`);
    process.exit(0);
  });
});
