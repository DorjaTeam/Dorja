using BACK.Middlewares;
using DorjaData;
using DorjaData.Repositories;
using DorjaModelado.Repositories;
using BACK;
using BACK.Services;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";

builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins, policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// REGISTRAR AUTENTICACIÓN JWT
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["Secret"] ?? "ClaveSecretaDorjaSuperSeguraParaJWT_ReemplazarEnProd!";
var key = Encoding.ASCII.GetBytes(secretKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

// REGISTRAR RATE LIMITING
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;
    
    // Política Global (100 peticiones por minuto)
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 100,
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(1)
            }));
            
    // Política para Auth (Login/Signup - 5 peticiones por minuto)
    options.AddPolicy("AuthLimit", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: partition => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 5,
                QueueLimit = 0,
                Window = TimeSpan.FromMinutes(1)
            }));
});


// REGISTRAR CONFIGURACIÓN SQLITE
var connectionString = builder.Configuration.GetConnectionString("DorjaConnection") 
    ?? throw new InvalidOperationException("Connection string 'DorjaConnection' not found.");

// Asegurar que la cadena de conexión use una ruta absoluta
var dbPath = connectionString.Replace("Data Source=", "").Trim();
if (!Path.IsPathRooted(dbPath))
{
    // Convertir a ruta absoluta relativa al directorio del backend
    dbPath = Path.Combine(Directory.GetCurrentDirectory(), dbPath);
    connectionString = $"Data Source={dbPath};Foreign Keys=True;";
    Console.WriteLine($"Usando ruta absoluta de base de datos: {dbPath}");
}
else
{
    // Asegurar que las Foreign Keys estén habilitadas incluso si la ruta ya era absoluta
    if (!connectionString.Contains("Foreign Keys=True", StringComparison.OrdinalIgnoreCase))
    {
        connectionString += ";Foreign Keys=True;";
    }
}

// Almacenar la cadena de conexión normalizada para su uso en toda la aplicación
var normalizedConnectionString = connectionString;
builder.Services.AddSingleton(new SQLiteConfiguration(normalizedConnectionString));


// REGISTRAR REPOSITORIO
builder.Services.AddScoped<IUserRepository, UsersRepository>();
builder.Services.AddScoped<INivelesRepository, NivelesRepository>();
builder.Services.AddScoped<ITemasRepository, TemasRepository>();
builder.Services.AddScoped<IProblemaRepository, ProblemaRepository>();
builder.Services.AddScoped<IProgreso_ProblemaRepository, Progreso_ProblemaRepository>();
builder.Services.AddScoped<ILogrosRepository, LogrosRepository>();
builder.Services.AddScoped<ILogros_UsuarioRepository, Logros_UsuarioRepository>();
builder.Services.AddScoped<ICertificadosRepository, CertificadoRepository>();
builder.Services.AddScoped<ICalificacionesRepository, CalificacionesRepository>();
builder.Services.AddScoped<IMensajesContactoRepository, MensajesContactoRepository>();

// REGISTRAR SERVICIOS Y CONFIGURACIÓN DE CORREO
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<ExerciseService>();

// Inicializar base de datos SQLite (corregirá automáticamente bases de datos incompletas)
try
{
    Console.WriteLine("Inicializando base de datos...");
    DatabaseInitializer.InitializeDatabase(normalizedConnectionString);
    Console.WriteLine("Inicialización de base de datos completada exitosamente.");
}
catch (Exception dbEx)
{
    Console.WriteLine($"ERROR al inicializar base de datos: {dbEx.Message}");
    Console.WriteLine("ADVERTENCIA: El servidor se iniciará pero la base de datos podría presentar problemas.");
    // Continuar - no evitar que el servidor se inicie
}

var app = builder.Build();

app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseRateLimiter(); // ACTIVAR RATE LIMITING

app.UseCors(MyAllowSpecificOrigins);

app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();

// Habilitar archivos estáticos para imágenes subidas
// Asegurar que el directorio wwwroot exista
var wwwrootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
if (!Directory.Exists(wwwrootPath))
{
    Directory.CreateDirectory(wwwrootPath);
}

// Configurar archivos estáticos con opciones explícitas
var staticFileOptions = new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(wwwrootPath),
    RequestPath = ""
};

app.UseStaticFiles(staticFileOptions);

app.UseAuthorization();

app.MapControllers();

// Registrar información de inicio
Console.WriteLine($"Iniciando servidor Backend...");
Console.WriteLine($"Base de datos inicializada en: {connectionString}");

// Añadir evento de ciclo de vida para registrar la dirección real una vez que el servidor inicie
app.Lifetime.ApplicationStarted.Register(() =>
{
    var addresses = app.Urls;
    foreach (var address in addresses)
    {
        Console.WriteLine($"Escuchando en: {address}");
    }
});

app.Run();
