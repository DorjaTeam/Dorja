# Startup Guide

## How to Start the Application

### Option 1: Desktop App (Electron) - RECOMMENDED

**You only need to run ONE command:**

```bash
npm start
```

**What happens:**
1. Electron automatically starts the .NET backend server using `dotnet run`
2. Electron waits 3 seconds for the backend to start
3. Electron opens a desktop window and loads your frontend
4. Everything is handled automatically!

**To stop:**
- Close the Electron window, and it will automatically stop the backend

### Option 2: Web App (Traditional)

If you want to run it as a web application instead:

**Terminal 1 - Start Backend:**
```bash
cd PROYECT/BACK
dotnet run
```

**Terminal 2 - Start Frontend:**
```bash
cd PROYECT/FRONT
dotnet run
```

Then open your browser to the URL shown (usually `http://localhost:5000` or similar).

## First Time Setup

1. **Install Node.js dependencies** (only needed once):
   ```bash
   npm install
   ```

2. **Build the .NET backend** (only needed if you make changes):
   ```bash
   cd PROYECT/BACK
   dotnet build
   ```

## Troubleshooting

### "Cannot read properties of undefined (reading 'signup')"

**Fixed!** The script paths have been updated from `/api.js` to `api.js` (relative path) in all HTML files. This was necessary because Electron's `loadFile()` doesn't handle absolute paths the same way as a web server.

### Backend not starting

- Make sure you have .NET SDK installed
- Check that the backend directory exists at `PROYECT/BACK`
- Look at the Electron console for error messages

### Port already in use

If port 5222 is already in use:
- Close any other instances of the app
- Or change the port in `PROYECT/BACK/appsettings.json` and update `api.js` accordingly

## Summary

**For Desktop App:** Just run `npm start` - that's it!

**For Web App:** Run backend and frontend separately in two terminals.

