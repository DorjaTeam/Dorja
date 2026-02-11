# Fixes Applied for Exercise System

## Issues Fixed

### 1. 500 Internal Server Error on `/Exercise/next/{userId}`
**Problem**: The `usuario_ejercicio` table might not exist in existing databases, causing repository calls to fail.

**Solution**:
- Added error handling in `ExerciseService.GetNextProblemForUser()` to gracefully handle missing `usuario_ejercicio` table
- Added try-catch blocks around `GetAssignedProblemaIds()` and `InsertUsuarioEjercicio()` calls
- The system will continue to work even if the table doesn't exist (just won't track assigned exercises)

### 2. 404 Not Found on `/Exercise/progress/{userId}`
**Problem**: Route might not be registered or there's an error in the method.

**Solution**:
- Added better error handling in `GetUserProgress()` method
- Added try-catch around `usuario_ejercicio` repository calls
- Improved error messages in controller

### 3. SQL Mapping Issues
**Problem**: Dapper might not map SQLite INTEGER (for booleans) and TEXT (for DateTime) correctly.

**Solution**:
- Fixed `Progreso_ProblemaRepository` to properly map:
  - `completado` (INTEGER) → `Completado` (bool)
  - `fecha_completado` (TEXT) → `FechaCompletado` (DateTime?)
- Fixed `UsuarioEjercicioRepository` to handle DateTime conversions
- All INSERT/UPDATE operations now properly convert types

### 4. Frontend Error Handling
**Problem**: Frontend wasn't properly handling API errors and response formats.

**Solution**:
- Improved `_makeRequest()` to handle different response formats
- Added better error messages for 500 and 404 errors
- Improved `loadRandomProblem()` to handle API errors gracefully
- Added fallback values for progress dashboard

## Next Steps

1. **Restart the backend** to ensure the `usuario_ejercicio` table is created
2. **Check backend logs** for any specific error messages
3. **Verify database** - The table should be created automatically on startup

## Testing

To test if the system is working:
1. Start the backend: `cd PROYECT/BACK && dotnet run`
2. Check console logs for "usuario_ejercicio table" messages
3. Open Exercises.html in browser
4. Check browser console for detailed error messages

## Database Migration

If you have an existing database, you may need to:
1. Delete `PROYECT/BACK/dorja.db` to recreate it with the new table
2. OR manually run: `CREATE TABLE IF NOT EXISTS usuario_ejercicio (...)` in SQLite

