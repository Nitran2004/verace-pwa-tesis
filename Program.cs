using System.Globalization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using ProyectoIdentity.Datos;
using ProyectoIdentity.Models;
using ProyectoIdentity.Servicios;
using Npgsql.EntityFrameworkCore.PostgreSQL;
//using static ProyectoIdentity.Controllers.UsuariosController;

var builder = WebApplication.CreateBuilder(args);

// Configuramos la conexión a PostgreSQL
// Configurar connection string para Railway
var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL");
if (!string.IsNullOrEmpty(connectionString))
{
    // Convertir DATABASE_URL a formato Entity Framework
    var databaseUri = new Uri(connectionString);
    connectionString = $"Host={databaseUri.Host};Port={databaseUri.Port};Database={databaseUri.LocalPath.Substring(1)};Username={databaseUri.UserInfo.Split(':')[0]};Password={databaseUri.UserInfo.Split(':')[1]};SSL Mode=Require;Trust Server Certificate=true";
}
else
{
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
}

builder.Services.AddDbContext<ApplicationDbContext>(opciones =>
    opciones.UseNpgsql(connectionString)
);

// Configuración de CORS
builder.Services.AddCors(opts =>
{
    opts.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader());
});

// Configuración de controladores
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        options.SuppressModelStateInvalidFilter = false; // Valida los modelos
    });

builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();


// Configuración de Identity
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    // Configuración de opciones de Identity
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(1);
    options.Lockout.MaxFailedAccessAttempts = 10;

    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 0;
})
.AddPasswordValidator<CustomPasswordValidator>()
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddErrorDescriber<CustomIdentityErrorDescriber>()
.AddDefaultTokenProviders();

// Servicios personalizados
// Configuración de cookies de autenticación
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = new PathString("/Cuentas/Acceso");
    options.AccessDeniedPath = new PathString("/Cuentas/Denegado");
});

// Configuración de localización
builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[] { new CultureInfo("es-ES") };
    options.DefaultRequestCulture = new RequestCulture("es-ES");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
});

// Servicios de sesión y contexto HTTP
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<IEmailSender, MailJetEmailSender>();

// Configuración de logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.AddEventSourceLogger();

builder.Logging.SetMinimumLevel(LogLevel.Information);

// Construcción de la aplicación
var app = builder.Build();

// Configuración del pipeline de middleware
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<ApplicationDbContext>();
    var env = services.GetRequiredService<IWebHostEnvironment>();

    // ✅ EJECUTAR MIGRACIONES AUTOMÁTICAMENTE
    context.Database.Migrate();

    DbInitializer.Initialize(context);
    RecompensasInitializer.Initialize(context);
}
///// app.UseHttpsRedirection();

app.UseStaticFiles();
var provider = new FileExtensionContentTypeProvider();
provider.Mappings[".glb"] = "model/gltf-binary";

// Usa archivos estáticos con la configuración personalizada
app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = provider
});
app.UseRouting();

// Middleware de CORS, sesión y autenticación
app.UseCors();
app.UseSession();
app.UseRequestLocalization();
app.UseAuthentication();
app.UseAuthorization();

// Mapeo de rutas
app.MapControllers();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();