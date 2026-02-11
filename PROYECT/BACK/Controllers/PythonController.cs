using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Text;
using DorjaData.Repositories;
using DorjaModelado;

namespace BACK.Controllers
{
    [Route("api/Python")]
    [ApiController]
    public class CodeExecutionController : ControllerBase
    {
        private readonly ILogrosRepository _logrosRepository;
        private readonly ILogros_UsuarioRepository _logrosUsuarioRepository;

        public CodeExecutionController(ILogrosRepository logrosRepository, ILogros_UsuarioRepository logrosUsuarioRepository)
        {
            _logrosRepository = logrosRepository;
            _logrosUsuarioRepository = logrosUsuarioRepository;
        }

        [HttpPost("execute")]
        public async Task<IActionResult> ExecuteCode([FromBody] CodeExecuteRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Code))
            {
                return BadRequest(new { message = "El código es requerido" });
            }

            if (string.IsNullOrWhiteSpace(request.Language))
            {
                return BadRequest(new { message = "El lenguaje de programación es requerido" });
            }

            try
            {
                var output = new StringBuilder();
                var error = new StringBuilder();
                string command = "";
                string tempFile = "";

                // Determine command and file extension based on language
                if (request.Language.ToLower() == "python")
                {
                    // Try python3 first (Linux/Mac), then python (Windows)
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
                    tempFile = Path.Combine(Path.GetTempPath(), $"code_exec_{Guid.NewGuid()}.py");
                }
                else if (request.Language.ToLower() == "csharp" || request.Language.ToLower() == "c#")
                {
                    // For C#, use dotnet script to run C# scripts
                    command = "dotnet";
                    tempFile = Path.Combine(Path.GetTempPath(), $"code_exec_{Guid.NewGuid()}.csx");
                }
                else
                {
                    return BadRequest(new { message = $"Lenguaje no soportado: {request.Language}. Solo se soportan Python y C#" });
                }

                // Create a temporary file to store the code
                await System.IO.File.WriteAllTextAsync(tempFile, request.Code, Encoding.UTF8);

                try
                {
                    ProcessStartInfo processStartInfo;
                    
                    if (request.Language.ToLower() == "csharp" || request.Language.ToLower() == "c#")
                    {
                        // For C#, use dotnet script to run C# scripts
                        processStartInfo = new ProcessStartInfo
                        {
                            FileName = "dotnet",
                            Arguments = $"script \"{tempFile}\"",
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true,
                            WorkingDirectory = Path.GetDirectoryName(tempFile),
                            StandardOutputEncoding = Encoding.UTF8,
                            StandardErrorEncoding = Encoding.UTF8
                        };
                    }
                    else
                    {
                        // Python execution
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
                        
                        // Set up output handlers
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

                        // Begin async reading
                        process.BeginOutputReadLine();
                        process.BeginErrorReadLine();

                    // Wait for the process to exit (with timeout)
                    bool exited = process.WaitForExit(30000); // 30 second timeout

                    if (!exited)
                    {
                        process.Kill();
                        return Ok(new
                        {
                            success = false,
                            output = "Error: El código tardó demasiado en ejecutarse (timeout de 30 segundos)",
                            error = "Timeout"
                        });
                    }

                        // Wait a bit for async output to complete
                        await Task.Delay(200);

                        var outputText = output.ToString().TrimEnd();
                        var errorText = error.ToString().TrimEnd();

                        var finalOutput = string.IsNullOrWhiteSpace(outputText) && string.IsNullOrWhiteSpace(errorText)
                            ? "(Sin salida)"
                            : (outputText + (string.IsNullOrWhiteSpace(errorText) ? "" : "\n" + CleanErrorMessage(errorText, tempFile)));

                        // Grant "Tu primer código" achievement if user provided and code executed successfully
                        bool achievementGranted = false;
                        if (request.UserId.HasValue && string.IsNullOrWhiteSpace(errorText) && process.ExitCode == 0)
                        {
                            achievementGranted = await GrantLogroIfNotExists(request.UserId.Value, "Tu primer código");
                        }

                        return Ok(new
                        {
                            success = string.IsNullOrWhiteSpace(errorText) && process.ExitCode == 0,
                            output = finalOutput,
                            exitCode = process.ExitCode,
                            achievementGranted = achievementGranted
                        });
                    }
                }
                finally
                {
                    // Clean up temporary file
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
                return StatusCode(500, new
                {
                    success = false,
                    output = $"Error al ejecutar el código: {CleanErrorMessage(ex.Message, "")}"
                });
            }
        }

        // Helper method to clean error messages (remove file paths)
        private string CleanErrorMessage(string error, string tempFile)
        {
            if (string.IsNullOrWhiteSpace(error))
                return error;

            var cleaned = error;

            // Remove temp file paths
            if (!string.IsNullOrWhiteSpace(tempFile))
            {
                cleaned = cleaned.Replace(tempFile, "[archivo temporal]");
                var tempDir = Path.GetDirectoryName(tempFile);
                if (!string.IsNullOrWhiteSpace(tempDir))
                {
                    cleaned = cleaned.Replace(tempDir, "[directorio temporal]");
                }
            }

            // Remove common temp path patterns
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"[A-Z]:\\[^\\]+\\Temp\\[^\s]+", "[archivo temporal]");
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"/tmp/[^\s]+", "[archivo temporal]");
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"C:\\Users\\[^\\]+\\AppData\\Local\\Temp\\[^\s]+", "[archivo temporal]");

            // Remove line number references to temp files but keep the error message
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"File\s+""[^""]+"",\s+line\s+\d+", "Línea");
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"in\s+<module>", "en el código");

            return cleaned.Trim();
        }

        private async Task<bool> GrantLogroIfNotExists(int userId, string logroNombre)
        {
            try
            {
                // Get logro by name
                var logro = await _logrosRepository.GetLogroByNombre(logroNombre);
                if (logro == null)
                {
                    Console.WriteLine($"Logro '{logroNombre}' no encontrado");
                    return false;
                }

                // Check if user already has this logro
                var hasLogro = await _logrosUsuarioRepository.UserHasLogro(userId, logro.Id);
                if (hasLogro)
                {
                    return false; // Already has it
                }

                // Grant the logro
                var logroUsuario = new Logros_Usuario
                {
                    Id_Usuario = userId,
                    Id_Logro = logro.Id,
                    Fecha_Obtencion = DateTime.Now
                };

                var created = await _logrosUsuarioRepository.InsertLogrosUsuario(logroUsuario);
                return created;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error granting logro '{logroNombre}' to user {userId}: {ex.Message}");
                return false;
            }
        }
    }

    public class CodeExecuteRequest
    {
        public string Code { get; set; } = string.Empty;
        public string Language { get; set; } = "python";
        public int? UserId { get; set; }
    }
}

