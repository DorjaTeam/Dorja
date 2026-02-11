const { contextBridge } = require('electron');

// Expose a minimal API that just indicates we're in Electron
// The actual API methods are defined in api.js and use HTTP to communicate with the backend
contextBridge.exposeInMainWorld('electronAPI', {
    platform: 'electron'
});