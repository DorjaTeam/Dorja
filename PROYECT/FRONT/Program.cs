var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Habilita el uso de archivos estáticos desde wwwroot
app.UseStaticFiles();

// Configuración adicional para SPA
app.UseRouting();

// Redirige cualquier ruta no encontrada a home.html (ideal para SPA)
app.MapFallbackToFile("home.html");

app.Run();