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

// Ensure the connection string uses an absolute path
var dbPath = connectionString.Replace("Data Source=", "").Trim();
if (!Path.IsPathRooted(dbPath))
{
    // Make path absolute relative to the backend directory
    dbPath = Path.Combine(Directory.GetCurrentDirectory(), dbPath);
    connectionString = $"Data Source={dbPath};Foreign Keys=True;";
    Console.WriteLine($"🔍 Using absolute database path: {dbPath}");
}
else
{
    // Make sure Foreign Keys are enabled even if path was already rooted
    if (!connectionString.Contains("Foreign Keys=True", StringComparison.OrdinalIgnoreCase))
    {
        connectionString += ";Foreign Keys=True;";
    }
}

// Store the normalized connection string for use throughout the app
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

// Initialize SQLite database (this will auto-fix incomplete databases)
try
{
    Console.WriteLine("🔧 Initializing database...");
    DatabaseInitializer.InitializeDatabase(normalizedConnectionString);
    Console.WriteLine("✅ Database initialization complete.");
}
catch (Exception dbEx)
{
    Console.WriteLine($"❌ ERROR initializing database: {dbEx.Message}");
    Console.WriteLine("⚠️ WARNING: Server will start but database may have issues.");
    // Continue - don't prevent server from starting
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

// Enable static files for uploaded images
// Ensure wwwroot directory exists
var wwwrootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
if (!Directory.Exists(wwwrootPath))
{
    Directory.CreateDirectory(wwwrootPath);
}

// Configure static files with explicit options
var staticFileOptions = new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(wwwrootPath),
    RequestPath = ""
};

app.UseStaticFiles(staticFileOptions);

app.UseAuthorization();

app.MapControllers();

// Log startup information
Console.WriteLine($" Backend server starting...");
Console.WriteLine($" Database initialized at: {connectionString}");

// Add a lifetime event to log the actual address once the server starts
app.Lifetime.ApplicationStarted.Register(() =>
{
    var addresses = app.Urls;
    foreach (var address in addresses)
    {
        Console.WriteLine($" Now listening on: {address}");
    }
});

app.Run();
