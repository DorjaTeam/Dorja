using DorjaData;
using DorjaData.Repositories;
using DorjaModelado.Repositories;
using BACK;
using BACK.Services;
using Microsoft.Extensions.FileProviders;

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


// REGISTRAR CONFIGURACIÓN SQLITE
var connectionString = builder.Configuration.GetConnectionString("DorjaConnection") 
    ?? throw new InvalidOperationException("Connection string 'DorjaConnection' not found.");

// Ensure the connection string uses an absolute path
var dbPath = connectionString.Replace("Data Source=", "").Trim();
if (!Path.IsPathRooted(dbPath))
{
    // Make path absolute relative to the backend directory
    dbPath = Path.Combine(Directory.GetCurrentDirectory(), dbPath);
    connectionString = $"Data Source={dbPath}";
    Console.WriteLine($"📁 Using absolute database path: {dbPath}");
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

// REGISTRAR SERVICIOS
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

app.UseCors(MyAllowSpecificOrigins);

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
Console.WriteLine($" Server will be available at: http://localhost:5222");

app.Run();
