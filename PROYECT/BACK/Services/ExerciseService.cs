using DorjaData.Repositories;
using DorjaModelado;
using DorjaModelado.Repositories;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BACK.Services
{
    public class ExerciseService
    {
        private readonly IProblemaRepository _problemaRepository;
        private readonly IProgreso_ProblemaRepository _progresoProblemaRepository;
        private readonly IUserRepository _userRepository;

        public ExerciseService(
            IProblemaRepository problemaRepository,
            IProgreso_ProblemaRepository progresoProblemaRepository,
            IUserRepository userRepository)
        {
            _problemaRepository = problemaRepository;
            _progresoProblemaRepository = progresoProblemaRepository;
            _userRepository = userRepository;
        }

        /// <summary>
        /// Obtiene un problema aleatorio para el usuario basado en su nivel actual
        /// Excluye problemas que el usuario ya ha completado
        /// </summary>
        public async Task<Problema> GetRandomProblemForUser(int userId)
        {
            // Obtener nivel actual del usuario
            var user = await _userRepository.GetDetails(userId);
            if (user == null)
            {
                throw new Exception("Usuario no encontrado");
            }

            int nivelActual = user.NivelActual;

            // Obtener todos los problemas para el nivel actual
            var problemasNivel = await _problemaRepository.GetProblemasByNivel(nivelActual);
            var problemasList = problemasNivel.ToList();

            if (problemasList.Count == 0)
            {
                throw new Exception("No hay problemas disponibles para tu nivel actual");
            }

            // Obtener problemas completados del usuario
            var progresos = await _progresoProblemaRepository.GetByUserId(userId);
            var completedProblemaIds = progresos
                .Where(p => p.Completado)
                .Select(p => p.ProblemaId)
                .ToHashSet();

            // Filtrar problemas completados y bloqueados
            var availableProblemas = problemasList
                .Where(p => !completedProblemaIds.Contains(p.Id) && !p.Locked)
                .ToList();

            // Si todos están completados, permitir repetir (para práctica)
            if (availableProblemas.Count == 0)
            {
                availableProblemas = problemasList
                    .Where(p => !p.Locked)
                    .ToList();
            }

            if (availableProblemas.Count == 0)
            {
                throw new Exception("No hay problemas disponibles. Todos estÃ¡n bloqueados.");
            }

            // Seleccionar problema aleatorio
            var random = new Random();
            var selectedProblema = availableProblemas[random.Next(availableProblemas.Count)];

            return selectedProblema;
        }

        /// <summary>
        /// Valida una solución ejecutando el código del usuario y de la solución,
        /// luego compara sus salidas
        /// </summary>
        public async Task<ValidationResult> ValidateSolution(int userId, int problemaId, string codigo, string language = "python", int tiempoInvertido = 0, int errores = 0, int intentosFallidos = 0)
        {
            try
            {
                // Log para depuración
                Console.WriteLine($"ðŸ” Validating solution - UserId: {userId}, ProblemaId: {problemaId}, Code length: {codigo?.Length ?? 0}");
                
                if (problemaId <= 0)
                {
                    return new ValidationResult 
                    { 
                        IsCorrect = false, 
                        Message = $"ID de problema invÃ¡lido: {problemaId}. Por favor, recarga la pÃ¡gina y selecciona un problema vÃ¡lido."
                    };
                }
                
                var problema = await _problemaRepository.GetDetails(problemaId);
            if (problema == null)
            {
                // Obtener todos los problemas para ayudar a depurar
                var allProblems = await _problemaRepository.GetAllProblemas();
                var problemList = allProblems.ToList();
                var problemIds = string.Join(", ", problemList.Take(20).Select(p => $"ID:{p.Id}"));
                var totalCount = problemList.Count;
                
                Console.WriteLine($"âŒ ERROR: Problema {problemaId} not found. Total problems in DB: {totalCount}");
                Console.WriteLine($"Available problem IDs (first 20): {problemIds}");
                
                // Log de todos los problemas para depurar
                if (problemList.Count > 0)
                {
                    Console.WriteLine("All problems in database:");
                    foreach (var p in problemList.Take(10))
                    {
                        Console.WriteLine($"  ID: {p.Id}, TemaId: {p.TemaId}, TÃ­tulo: {p.Titulo}");
                    }
                }
                else
                {
                    Console.WriteLine("âš ï¸ WARNING: No hay problemas en la base de datos. La base de datos necesita ser inicializada.");
                }
                
                // Retornar mensaje de error útil
                var errorMessage = totalCount == 0
                    ? "No hay problemas en la base de datos. Por favor, reinicia el servidor para inicializar la base de datos."
                    : $"El problema con ID {problemaId} no existe en la base de datos. Hay {totalCount} problemas disponibles (IDs: {problemIds}). Por favor, recarga la pÃ¡gina y selecciona un problema vÃ¡lido.";
                
                return new ValidationResult 
                { 
                    IsCorrect = false, 
                    Message = errorMessage
                };
            }
            
            Console.WriteLine($"âœ… Problema encontrado: ID={problema.Id}, TÃ­tulo={problema.Titulo}");

            if (string.IsNullOrWhiteSpace(codigo))
            {
                return new ValidationResult { IsCorrect = false, Message = "El cÃ³digo no puede estar vacÃ­o" };
            }

            // Ejecutar código del usuario
            var userResult = await ExecuteCode(codigo, language);
            if (!userResult.Success)
            {
                return new ValidationResult 
                { 
                    IsCorrect = false, 
                    Message = $"Error al ejecutar tu cÃ³digo: {userResult.Output}" 
                };
            }

            // Ejecutar código de la solución
            var solutionResult = await ExecuteCode(problema.Solucion, language);
            if (!solutionResult.Success)
            {
                // Si el código solución tiene errores, usar comparación de cadenas
                bool isValid = ValidateByStringComparison(codigo, problema.Solucion);
                
                // Actualizar progreso - incluso si falla, retornar el resultado
                try
                {
                    await UpdateProgress(userId, problemaId, codigo, isValid, problema.PuntosOtorgados, tiempoInvertido, errores, intentosFallidos);
                }
                catch (Exception progressEx)
                {
                    // Registrar pero no fallar validación
                    Console.WriteLine($"âš ï¸ WARNING: Error updating progress (continuing anyway): {progressEx.Message}");
                }
                
                return new ValidationResult
                {
                    IsCorrect = isValid,
                    Message = isValid 
                        ? $"Â¡SoluciÃ³n correcta! Has ganado {problema.PuntosOtorgados} puntos." 
                        : "La soluciÃ³n no es correcta. Revisa tu cÃ³digo e intenta de nuevo.",
                    PuntosOtorgados = isValid ? problema.PuntosOtorgados : 0,
                    UserOutput = userResult.Output,
                    ExpectedOutput = "(No disponible - error en cÃ³digo de soluciÃ³n)"
                };
            }

            // Comparar salidas (normalizado)
            var userOutput = NormalizeOutput(userResult.Output);
            var solutionOutput = NormalizeOutput(solutionResult.Output);

            bool isCorrect = userOutput == solutionOutput;

            // Si las salidas no coinciden, probar comparación de cadenas
            if (!isCorrect)
            {
                isCorrect = ValidateByStringComparison(codigo, problema.Solucion);
            }

            // Actualizar progreso - incluso si falla, retornar el resultado
            try
            {
                await UpdateProgress(userId, problemaId, codigo, isCorrect, problema.PuntosOtorgados, tiempoInvertido, errores, intentosFallidos);
            }
            catch (Exception progressEx)
            {
                // Registrar pero no fallar validación
                Console.WriteLine($"âš ï¸ WARNING: Error updating progress (continuing anyway): {progressEx.Message}");
            }

            return new ValidationResult
            {
                IsCorrect = isCorrect,
                Message = isCorrect 
                    ? $"Â¡SoluciÃ³n correcta! Has ganado {problema.PuntosOtorgados} puntos." 
                    : "La soluciÃ³n no es correcta. Revisa tu cÃ³digo e intenta de nuevo.",
                PuntosOtorgados = isCorrect ? problema.PuntosOtorgados : 0,
                UserOutput = userResult.Output,
                ExpectedOutput = solutionResult.Output
            };
            }
            catch (Exception ex)
            {
                // Registrar error pero retornar resultado válido
                Console.WriteLine($"âŒ ERROR in ValidateSolution: {ex.Message}");
                Console.WriteLine($"   Stack trace: {ex.StackTrace}");
                
                // Retornar mensaje de error amigable
                return new ValidationResult
                {
                    IsCorrect = false,
                    Message = $"Error al validar la soluciÃ³n: {ex.Message}. Por favor, verifica que el problema existe y vuelve a intentar.",
                    PuntosOtorgados = 0,
                    UserOutput = null,
                    ExpectedOutput = null
                };
            }
        }

        /// <summary>
        /// Ejecuta código y retorna la salida
        /// </summary>
        private async Task<CodeExecutionResult> ExecuteCode(string code, string language)
        {
            try
            {
                var output = new StringBuilder();
                var error = new StringBuilder();
                string command = "";
                string tempFile = "";

                // Determinar comando y extensión según el lenguaje
                if (language.ToLower() == "python")
                {
                    command = "python3";
                    try
                    {
                        var testProcess = new Process
                        {
                            StartInfo = new ProcessStartInfo
                            {
                                FileName = command,
                                Arguments = "--version",
                                RedirectStandardOutput = true,
                                RedirectStandardError = true,
                                UseShellExecute = false,
                                CreateNoWindow = true
                            }
                        };
                        testProcess.Start();
                        testProcess.WaitForExit(1000);
                        if (testProcess.ExitCode != 0)
                        {
                            command = "python";
                        }
                    }
                    catch
                    {
                        command = "python";
                    }
                    tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"code_validation_{Guid.NewGuid()}.py");
                }
                else if (language.ToLower() == "csharp" || language.ToLower() == "c#")
                {
                    command = "dotnet";
                    tempFile = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"code_validation_{Guid.NewGuid()}.csx");
                }
                else
                {
                    return new CodeExecutionResult 
                    { 
                        Success = false, 
                        Output = $"Lenguaje no soportado: {language}" 
                    };
                }

                // Escribir código a archivo temporal
                await System.IO.File.WriteAllTextAsync(tempFile, code, Encoding.UTF8);

                try
                {
                    ProcessStartInfo processStartInfo;
                    
                    if (language.ToLower() == "csharp" || language.ToLower() == "c#")
                    {
                        processStartInfo = new ProcessStartInfo
                        {
                            FileName = "dotnet",
                            Arguments = $"script \"{tempFile}\"",
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            WorkingDirectory = System.IO.Path.GetDirectoryName(tempFile),
                            StandardOutputEncoding = Encoding.UTF8,
                            StandardErrorEncoding = Encoding.UTF8
                        };
                    }
                    else
                    {
                        processStartInfo = new ProcessStartInfo
                        {
                            FileName = command,
                            Arguments = $"\"{tempFile}\"",
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            StandardOutputEncoding = Encoding.UTF8,
                            StandardErrorEncoding = Encoding.UTF8
                        };
                    }

                    using (var process = new Process())
                    {
                        process.StartInfo = processStartInfo;
                        
                        process.OutputDataReceived += (sender, e) =>
                        {
                            if (!string.IsNullOrEmpty(e.Data))
                            {
                                output.AppendLine(e.Data);
                            }
                        };

                        process.ErrorDataReceived += (sender, e) =>
                        {
                            if (!string.IsNullOrEmpty(e.Data))
                            {
                                error.AppendLine(e.Data);
                            }
                        };

                        process.Start();
                        process.BeginOutputReadLine();
                        process.BeginErrorReadLine();

                        bool exited = process.WaitForExit(10000); // 10 second timeout for validation

                        if (!exited)
                        {
                            process.Kill();
                            return new CodeExecutionResult 
                            { 
                                Success = false, 
                                Output = "Timeout: El cÃ³digo tardÃ³ demasiado en ejecutarse" 
                            };
                        }

                        await Task.Delay(200);

                        var outputText = output.ToString().TrimEnd();
                        var errorText = error.ToString().TrimEnd();

                        if (!string.IsNullOrWhiteSpace(errorText))
                        {
                            return new CodeExecutionResult 
                            { 
                                Success = false, 
                                Output = CleanErrorMessage(errorText, tempFile) 
                            };
                        }

                        return new CodeExecutionResult 
                        { 
                            Success = true, 
                            Output = string.IsNullOrWhiteSpace(outputText) ? "(Sin salida)" : outputText 
                        };
                    }
                }
                finally
                {
                    try
                    {
                        if (System.IO.File.Exists(tempFile))
                        {
                            System.IO.File.Delete(tempFile);
                        }
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                return new CodeExecutionResult 
                { 
                    Success = false, 
                    Output = $"Error al ejecutar el cÃ³digo: {ex.Message}" 
                };
            }
        }

        private string CleanErrorMessage(string error, string tempFile)
        {
            if (string.IsNullOrWhiteSpace(error))
                return error;

            var cleaned = error;
            if (!string.IsNullOrWhiteSpace(tempFile))
            {
                cleaned = cleaned.Replace(tempFile, "[archivo temporal]");
                var tempDir = System.IO.Path.GetDirectoryName(tempFile);
                if (!string.IsNullOrWhiteSpace(tempDir))
                {
                    cleaned = cleaned.Replace(tempDir, "[directorio temporal]");
                }
            }

            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"[A-Z]:\\[^\\]+\\Temp\\[^\s]+", "[archivo temporal]");
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"/tmp/[^\s]+", "[archivo temporal]");
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"C:\\Users\\[^\\]+\\AppData\\Local\\Temp\\[^\s]+", "[archivo temporal]");

            return cleaned.Trim();
        }

        private string NormalizeOutput(string output)
        {
            if (string.IsNullOrEmpty(output)) return "";
            // Limpiar espacios y normalizar saltos de línea
            return output.TrimEnd().Replace("\r\n", "\n").Replace("\r", "\n");
        }

        private bool ValidateByStringComparison(string userCode, string solutionCode)
        {
            // Normalizar ambos códigos para comparación
            var normalizedUser = NormalizeCode(userCode);
            var normalizedSolution = NormalizeCode(solutionCode);
            
            // Comprobar elementos clave de la solución
            // Esto es un respaldo si falla la ejecución
            return normalizedUser.Contains(normalizedSolution) || 
                   normalizedSolution.Contains(normalizedUser) ||
                   AreFunctionallySimilar(normalizedUser, normalizedSolution);
        }

        private string NormalizeCode(string code)
        {
            if (string.IsNullOrEmpty(code)) return "";
            // Quitar comentarios, espacios y normalizar
            var lines = code.Split('\n');
            var cleaned = new StringBuilder();
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                // Omitir líneas vacías y comentarios
                if (!string.IsNullOrEmpty(trimmed) && !trimmed.StartsWith("#") && !trimmed.StartsWith("//"))
                {
                    cleaned.Append(trimmed.Replace(" ", "").Replace("\t", ""));
                }
            }
            return cleaned.ToString().ToLower();
        }

        private bool AreFunctionallySimilar(string code1, string code2)
        {
            // Chequeo de similitud simple
            if (code1.Length == 0 || code2.Length == 0) return false;
            
            // Calcular ratio de similitud
            int matches = 0;
            int minLength = Math.Min(code1.Length, code2.Length);
            for (int i = 0; i < minLength; i++)
            {
                if (code1[i] == code2[i]) matches++;
            }
            
            double similarity = (double)matches / Math.Max(code1.Length, code2.Length);
            return similarity > 0.8; // 80% similarity threshold
        }

        private async Task UpdateProgress(int userId, int problemaId, string codigo, bool isCorrect, int puntosOtorgados, int tiempoInvertido = 0, int errores = 0, int intentosFallidos = 0)
        {
            try
            {
                // Validar usuario
                var user = await _userRepository.GetDetails(userId);
                if (user == null)
                {
                    Console.WriteLine($"âš ï¸ WARNING: Usuario con ID {userId} no existe. No se puede actualizar el progreso.");
                    return; // Fallar silenciosamente sin excepción
                }

                // Validar problema asegurando su existencia
                var problem = await _problemaRepository.GetDetails(problemaId);
                if (problem == null)
                {
                    // Intentar obtener todos los problemas
                    var allProblems = await _problemaRepository.GetAllProblemas();
                    var problemList = allProblems.ToList();
                    var problemIds = problemList.Count > 0 
                        ? string.Join(", ", problemList.Take(20).Select(p => p.Id))
                        : "ninguno";
                    
                    Console.WriteLine($"âš ï¸ WARNING: Problema con ID {problemaId} no existe en la base de datos.");
                    Console.WriteLine($"   Total de problemas disponibles: {problemList.Count}");
                    if (problemList.Count > 0)
                    {
                        Console.WriteLine($"   IDs disponibles (primeros 20): {problemIds}");
                        // Loguear primeros problemas para depurar
                        foreach (var p in problemList.Take(5))
                        {
                            Console.WriteLine($"     - ID: {p.Id}, TÃ­tulo: {p.Titulo}, TemaId: {p.TemaId}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"   âš ï¸ CRÃTICO: No hay problemas en la base de datos. La base de datos necesita ser inicializada.");
                    }
                    
                    // No lanzar excepción - solo registrar
                    // Esto permite continuar validación
                    return;
                }

                Console.WriteLine($"âœ… Actualizando progreso: UserId={userId}, ProblemaId={problemaId}, Correcto={isCorrect}");

                // Obtener o crear progreso
                var progreso = await _progresoProblemaRepository.GetByUserAndProblema(userId, problemaId);

                if (progreso == null)
                {
                    // No existe, crear nuevo registro
                    progreso = new Progreso_Problema
                    {
                        UserId = userId,
                        ProblemaId = problemaId,
                        Completado = isCorrect,
                        Puntuacion = isCorrect ? puntosOtorgados : 0,
                        Intentos = 1,
                        Errores = errores,
                        IntentosFallidos = !isCorrect ? intentosFallidos + 1 : intentosFallidos,
                        TiempoInvertido = tiempoInvertido,
                        UltimoCodigo = codigo,
                        FechaCompletado = isCorrect ? DateTime.UtcNow : (DateTime?)null
                    };
                    
                    var inserted = await _progresoProblemaRepository.InsertProgreso_Problemas(progreso);
                    if (!inserted)
                    {
                        Console.WriteLine($"âš ï¸ WARNING: No se pudo insertar el progreso para UserId={userId}, ProblemaId={problemaId}");
                        // Verificar existencia de problema
                        var verifyProblem = await _problemaRepository.GetDetails(problemaId);
                        if (verifyProblem == null)
                        {
                            Console.WriteLine($"   âš ï¸ El problema {problemaId} ya no existe. Puede haber sido eliminado.");
                        }
                        return; // Fallar silenciosamente
                    }
                    Console.WriteLine($"âœ… Progreso insertado exitosamente para UserId={userId}, ProblemaId={problemaId}");
                }
                else
                {
                    // Existe, actualizar registro
                    Console.WriteLine($"ðŸ“ Progreso existente encontrado - Id: {progreso.Id}, Completado: {progreso.Completado}");
                    
                    // Asegurarse de que los IDs estÃ¡n correctos
                    if (progreso.Id <= 0)
                    {
                        Console.WriteLine($"âš ï¸ WARNING: Progreso encontrado tiene Id invÃ¡lido ({progreso.Id}). Intentando obtener ID correcto...");
                        // Obtener el progreso de nuevo para asegurar que tiene el ID correcto
                        var progresoVerificado = await _progresoProblemaRepository.GetByUserAndProblema(userId, problemaId);
                        if (progresoVerificado != null && progresoVerificado.Id > 0)
                        {
                            progreso = progresoVerificado;
                            Console.WriteLine($"âœ… Progreso verificado - Id correcto: {progreso.Id}");
                        }
                        else
                        {
                            Console.WriteLine($"âš ï¸ WARNING: No se pudo obtener ID correcto. Insertando nuevo registro...");
                            // Si no se puede obtener el ID, insertar como nuevo
                            progreso = new Progreso_Problema
                            {
                                UserId = userId,
                                ProblemaId = problemaId,
                                Completado = isCorrect,
                                Puntuacion = isCorrect ? puntosOtorgados : 0,
                                Intentos = progreso.Intentos + 1,
                                Errores = progreso.Errores + errores,
                                IntentosFallidos = !isCorrect ? progreso.IntentosFallidos + 1 : progreso.IntentosFallidos,
                                TiempoInvertido = progreso.TiempoInvertido + tiempoInvertido,
                                UltimoCodigo = codigo,
                                FechaCompletado = isCorrect ? DateTime.UtcNow : (DateTime?)null
                            };
                            var inserted = await _progresoProblemaRepository.InsertProgreso_Problemas(progreso);
                            if (inserted)
                            {
                                Console.WriteLine($"âœ… Progreso reinsertado exitosamente");
                                return;
                            }
                            else
                            {
                                Console.WriteLine($"âš ï¸ WARNING: No se pudo reinsertar el progreso");
                                return;
                            }
                        }
                    }
                    
                    if (isCorrect && !progreso.Completado)
                    {
                        progreso.Completado = true;
                        progreso.Puntuacion = puntosOtorgados;
                        progreso.FechaCompletado = DateTime.UtcNow;
                        await UpdateUserPoints(userId, puntosOtorgados);
                    }

                    progreso.Intentos++;
                    progreso.Errores += errores;
                    if (!isCorrect) progreso.IntentosFallidos++;
                    progreso.TiempoInvertido += tiempoInvertido;
                    progreso.UltimoCodigo = codigo;
                    
                    Console.WriteLine($"ðŸ”„ Actualizando progreso - Id: {progreso.Id}, UserId: {progreso.UserId}, ProblemaId: {progreso.ProblemaId}, Completado: {progreso.Completado}");
                    
                    var updated = await _progresoProblemaRepository.UpdateProgreso_Problemas(progreso);
                    if (!updated)
                    {
                        Console.WriteLine($"âš ï¸ WARNING: No se pudo actualizar el progreso para UserId={userId}, ProblemaId={problemaId}, ProgresoId={progreso.Id}");
                        return; // Fallar silenciosamente
                    }
                    Console.WriteLine($"âœ… Progreso actualizado exitosamente para UserId={userId}, ProblemaId={problemaId}");
                }
            }
            catch (Exception ex) when (ex.Message.Contains("FOREIGN KEY constraint failed") || 
                                       ex.Message.Contains("SQLITE_CONSTRAINT_FOREIGNKEY") ||
                                       ex.Message.Contains("no such column") ||
                                       ex.Message.Contains("no such table"))
            {
                // Log the error but don't throw - allow validation to continue
                Console.WriteLine($"âš ï¸ WARNING: Error de base de datos al guardar progreso:");
                Console.WriteLine($"   UserId: {userId}, ProblemaId: {problemaId}");
                Console.WriteLine($"   Error: {ex.Message}");
                Console.WriteLine($"   El progreso no se guardÃ³, pero la validaciÃ³n continuarÃ¡.");
                // Don't throw - silently fail so validation can complete
                return;
            }
            catch (Exception ex)
            {
                // Log the error but don't throw - allow validation to continue
                Console.WriteLine($"âš ï¸ WARNING: Error inesperado en UpdateProgress:");
                Console.WriteLine($"   UserId: {userId}, ProblemaId: {problemaId}");
                Console.WriteLine($"   Error: {ex.Message}");
                Console.WriteLine($"   Stack trace: {ex.StackTrace}");
                // Don't throw - silently fail so validation can complete
                return;
            }
        }

        private async Task UpdateUserPoints(int userId, int puntos)
        {
            var user = await _userRepository.GetDetails(userId);
            if (user != null)
            {
                user.PuntosTotales += puntos;
                // Level up logic could be added here
                await _userRepository.UpdateUsuarios(user);
            }
        }
    }

    public class ValidationResult
    {
        public bool IsCorrect { get; set; }
        public string Message { get; set; } = "";
        public int PuntosOtorgados { get; set; }
        public string? UserOutput { get; set; }
        public string? ExpectedOutput { get; set; }
    }

    internal class CodeExecutionResult
    {
        public bool Success { get; set; }
        public string Output { get; set; } = "";
    }
}
