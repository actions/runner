/**
 * Popup Script
 * 
 * Handles extension popup UI and connection management.
 */

document.addEventListener('DOMContentLoaded', () => {
  const statusIndicator = document.getElementById('status-indicator');
  const statusText = document.getElementById('status-text');
  const connectBtn = document.getElementById('connect-btn');
  const disconnectBtn = document.getElementById('disconnect-btn');
  const urlInput = document.getElementById('proxy-url');

  // Load saved config
  chrome.storage.local.get(['proxyUrl'], (data) => {
    if (data.proxyUrl) urlInput.value = data.proxyUrl;
  });

  // Get current status from background
  chrome.runtime.sendMessage({ type: 'get-status' }, (response) => {
    if (response && response.status) {
      updateStatusUI(response.status);
    }
  });

  // Listen for status changes
  chrome.runtime.onMessage.addListener((message) => {
    if (message.type === 'status-changed') {
      updateStatusUI(message.status);
    }
  });

  // Connect button
  connectBtn.addEventListener('click', () => {
    const url = urlInput.value.trim() || 'ws://localhost:4712';

    // Save config
    chrome.storage.local.set({ proxyUrl: url });

    // Update UI immediately
    updateStatusUI('connecting');

    // Connect
    chrome.runtime.sendMessage({ type: 'connect', url }, (response) => {
      if (response && response.status) {
        updateStatusUI(response.status);
      }
    });
  });

  // Disconnect button
  disconnectBtn.addEventListener('click', () => {
    chrome.runtime.sendMessage({ type: 'disconnect' }, (response) => {
      if (response && response.status) {
        updateStatusUI(response.status);
      }
    });
  });

  /**
   * Update the UI to reflect current status
   */
  function updateStatusUI(status) {
    // Update text
    const statusNames = {
      disconnected: 'Disconnected',
      connecting: 'Connecting...',
      connected: 'Connected',
      paused: 'Paused',
      running: 'Running',
      error: 'Error',
    };
    statusText.textContent = statusNames[status] || status;

    // Update indicator color
    statusIndicator.className = 'status-indicator status-' + status;

    // Update button states
    const isConnected = ['connected', 'paused', 'running'].includes(status);
    const isConnecting = status === 'connecting';

    connectBtn.disabled = isConnected || isConnecting;
    disconnectBtn.disabled = !isConnected;

    // Update connect button text
    if (isConnecting) {
      connectBtn.textContent = 'Connecting...';
    } else {
      connectBtn.textContent = 'Connect';
    }

    // Disable inputs when connected
    urlInput.disabled = isConnected || isConnecting;
  }
});
