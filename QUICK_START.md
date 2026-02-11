# Quick Start Guide

## Problem: Backend Not Running (ERR_CONNECTION_REFUSED)

If you see `ERR_CONNECTION_REFUSED`, the backend server is not running.

## Solution 1: Start Backend Manually (Recommended for Development)

**Open a terminal and run:**

```bash
cd PROYECT/BACK
dotnet run
```

You should see:
```
Now listening on: http://localhost:5222
Application started. Press Ctrl+C to shut down.
```

**Keep this terminal open** - the backend must stay running.

Then in another terminal or browser, access the frontend.

## Solution 2: Use Electron (Desktop App)

If you want Electron to start the backend automatically:

```bash
npm start
```

**Note:** Electron will try to start the backend, but if it fails, you'll need to start it manually (see Solution 1).

## Solution 3: Check if Backend is Already Running

Sometimes the backend might already be running on a different port. Check:

1. Look for any terminal windows running `dotnet run`
2. Try accessing: http://localhost:5222/api/Users
3. If it responds, the backend is running

## Troubleshooting

### "dotnet: command not found"
- Install .NET SDK from https://dotnet.microsoft.com/download
- Restart your terminal after installation

### "Port 5222 is already in use"
- Close any other instances of the backend
- Or change the port in `PROYECT/BACK/Properties/launchSettings.json`

### Backend starts but immediately crashes
- Check the error messages in the terminal
- Make sure SQLite database can be created (check file permissions)
- Verify all NuGet packages are restored: `dotnet restore`

## Recommended Workflow

**For Development:**
1. Terminal 1: `cd PROYECT/BACK && dotnet run` (keep running)
2. Terminal 2: `cd PROYECT/FRONT && dotnet run` (for web) OR use Electron: `npm start`

**For Production/Desktop:**
- Just run `npm start` - Electron handles everything

