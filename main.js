const { app, BrowserWindow, ipcMain, dialog } = require('electron');
const path = require('path');
const { spawn } = require('child_process');
const fs = require('fs');
const { autoUpdater } = require('electron-updater');
const log = require('electron-log');
const net = require('net');

// Configure logging
log.transports.file.level = 'info';
autoUpdater.logger = log;
autoUpdater.autoDownload = false; // Optional: set to true if you want auto download


let mainWindow;
let backendProcess;
let backendPort = 5222; // Default, will be updated dynamically

// Helper to find a free port
const findFreePort = (startPort) => {
  return new Promise((resolve, reject) => {
    const server = net.createServer();
    server.listen(startPort, () => {
      const port = server.address().port;
      server.close(() => resolve(port));
    });
    server.on('error', (err) => {
      if (err.code === 'EADDRINUSE') {
        resolve(findFreePort(startPort + 1));
      } else {
        reject(err);
      }
    });
  });
};

// Start the backend server
const startBackend = async () => {
  // Find a free port starting from 5222
  try {
    backendPort = await findFreePort(5222);
    log.info(`Selected backend port: ${backendPort}`);
  } catch (err) {
    log.error('Failed to find free port:', err);
    backendPort = 0; // Let OS choose (though we prefer specific range)
  }

  const portArg = `http://localhost:${backendPort}`;
  let backendProcess;

  if (app.isPackaged) {
    // Production: Run the executable from resources
    // The backend is copied to 'resources/backend'
    const backendPath = path.join(process.resourcesPath, 'backend', 'BACK.exe');
    log.info('Starting backend from:', backendPath);

    if (!fs.existsSync(backendPath)) {
      log.error('Backend executable not found at:', backendPath);
      dialog.showErrorBox('Error de Inicio', `No se encontró el ejecutable del servidor en: ${backendPath}`);
      return null;
    }

    // Spawn the backend executable
    backendProcess = spawn(backendPath, ['--urls', portArg], {
      cwd: path.dirname(backendPath),
      stdio: ['ignore', 'pipe', 'pipe']
    });
  } else {
    // Development: Run with dotnet run
    const backendDir = path.join(__dirname, 'PROYECT', 'BACK');
    log.info('Starting backend in development mode...');
    log.info('Backend directory:', backendDir);

    if (!fs.existsSync(backendDir)) {
      console.error('ERROR: Backend directory not found:', backendDir);
      return null;
    }

    // Try running with dotnet
    backendProcess = spawn('dotnet', ['run', '--urls', portArg], {
      cwd: backendDir,
      shell: true,
      stdio: ['ignore', 'pipe', 'pipe'] // Capture output
    });
  }

  let backendReady = false;

  if (backendProcess && backendProcess.stdout) {
    backendProcess.stdout.on('data', (data) => {
      const output = data.toString();
      log.info(`Backend: ${output}`);
      if (!app.isPackaged) console.log(`Backend: ${output}`);

      // Check if backend is ready
      if (output.includes('Now listening on:') || output.includes('Application started')) {
        backendReady = true;
        log.info('✅ Backend server is ready!');
      }
    });

    backendProcess.stderr.on('data', (data) => {
      const error = data.toString();
      log.error(`Backend Error: ${error}`);
      if (!app.isPackaged) console.error(`Backend Error: ${error}`);
    });

    backendProcess.on('close', (code) => {
      log.info(`Backend process exited with code ${code}`);
      if (!app.isPackaged) console.log(`Backend process exited with code ${code}`);
      backendReady = false;

      // If code is not 0 and not null (killed), show error
      if (code !== 0 && code !== null) {
        dialog.showErrorBox('Servidor Detenido', `El servidor backend se cerró inesperadamente (Código: ${code}). Verifique que el puerto esté libre y que tenga instalados los componentes necesarios.`);
      }
    });

    backendProcess.on('error', (err) => {
      log.error('Failed to start backend:', err);
      if (!app.isPackaged) console.error('Failed to start backend:', err);
      dialog.showErrorBox('Error al Iniciar Servidor', `No se pudo iniciar el proceso del backend: ${err.message}`);
    });
  }

  return backendProcess;
};

// Stop the backend server
const stopBackend = () => {
  if (backendProcess) {
    backendProcess.kill();
    backendProcess = null;
  }
};

function createWindow() {
  mainWindow = new BrowserWindow({
    width: 1400,
    height: 900,
    minWidth: 800,
    minHeight: 600,
    webPreferences: {
      preload: path.join(__dirname, 'PROYECT', 'FRONT', 'wwwroot', 'preload.js'),
      nodeIntegration: false,
      contextIsolation: true,
      webSecurity: false // Allow local file access
    },
    show: false
  });

  // Handle IPC for config
  ipcMain.handle('get-api-config', () => {
    return {
      baseUrl: `http://localhost:${backendPort}/api`
    };
  });

  // Start backend first
  startBackend().then(proc => {
    backendProcess = proc;

    if (!proc) {
      // Backend failed to even start spawning
      console.error('❌ Backend process could not be started.');
      return;
    }

    // Wait for backend to be ready, then load the page
    const checkBackend = setInterval(() => {
      // Try to connect to the backend
      const http = require('http');
      const req = http.get(`http://localhost:${backendPort}/api/Users`, (res) => {
        if (res.statusCode === 200 || res.statusCode === 404) {
          // Backend is responding
          clearInterval(checkBackend);
          console.log('✅ Backend is ready, loading frontend...');
          const htmlPath = path.join(__dirname, 'PROYECT', 'FRONT', 'wwwroot', 'home.html');
          mainWindow.loadFile(htmlPath);
          mainWindow.show();
        }
      });

      req.on('error', () => {
        // Backend not ready yet, keep waiting
      });

      req.setTimeout(1000, () => {
        req.destroy();
      });
    }, 1000);

    // Timeout after 30 seconds
    setTimeout(() => {
      clearInterval(checkBackend);
      if (!mainWindow.isVisible()) {
        console.error('⚠️ Backend did not start in time. Loading frontend anyway...');
        const htmlPath = path.join(__dirname, 'PROYECT', 'FRONT', 'wwwroot', 'home.html');
        mainWindow.loadFile(htmlPath);
        mainWindow.show();
      }
    }, 30000);
  });


  mainWindow.on('closed', () => {
    mainWindow = null;
  });
}

app.whenReady().then(() => {
  setupAutoUpdater();
  createWindow();

  app.on('activate', () => {
    if (BrowserWindow.getAllWindows().length === 0) {
      createWindow();
    }
  });
});

app.on('window-all-closed', () => {
  stopBackend();
  if (process.platform !== 'darwin') {
    app.quit();
  }
});

function setupAutoUpdater() {
  log.info('App starting...');

  autoUpdater.on('checking-for-update', () => {
    log.info('Checking for update...');
  });

  autoUpdater.on('update-available', (info) => {
    log.info('Update available.');
    // Optional: Ask user if they want to download
    autoUpdater.downloadUpdate();
  });

  autoUpdater.on('update-not-available', (info) => {
    log.info('Update not available.');
  });

  autoUpdater.on('error', (err) => {
    log.info('Error in auto-updater. ' + err);
  });

  autoUpdater.on('download-progress', (progressObj) => {
    let log_message = "Download speed: " + progressObj.bytesPerSecond;
    log_message = log_message + ' - Downloaded ' + progressObj.percent + '%';
    log_message = log_message + ' (' + progressObj.transferred + "/" + progressObj.total + ')';
    log.info(log_message);
  });

  autoUpdater.on('update-downloaded', (info) => {
    log.info('Update downloaded');
    // Optional: Ask user to restart
    autoUpdater.quitAndInstall();
  });

  autoUpdater.checkForUpdatesAndNotify();
}

app.on('before-quit', () => {
  stopBackend();
});
