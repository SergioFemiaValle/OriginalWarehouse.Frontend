using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OriginalWarehouse.Application.Interfaces;
using OriginalWarehouse.Application.Managers;
using OriginalWarehouse.Domain.Entities;
using OriginalWarehouse.Infrastructure;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// 🔹 **Configurar la cultura global en español**
var cultureInfo = new CultureInfo("es-ES")
{
    NumberFormat = { NumberDecimalSeparator = ".", CurrencyDecimalSeparator = "." }
};
CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

// 🔹 **Configurar conexión a la base de datos**
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// 🔹 **Registrar los servicios de `Infrastructure`**
builder.Services.AddInfrastructure(connectionString);

// 🔹 **Agregar Identity**
builder.Services.AddIdentity<Usuario, Rol>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// 🔹 **Agregar Autenticación y Autorización**
builder.Services.AddAuthentication();
builder.Services.AddAuthorization();

// 🔹 **Configurar MVC y Razor Pages**
builder.Services.AddControllersWithViews()
    .AddRazorOptions(options =>
    {
        options.ViewLocationFormats.Add("/MVC/Views/{1}/{0}.cshtml"); // 📌 Indicar dónde están las vistas de MVC
        options.ViewLocationFormats.Add("/MVC/Views/Shared/{0}.cshtml"); // 📌 Indicar dónde están las vistas compartidas de MVC
    });

builder.Services.AddRazorPages(); // 📌 Esto permite usar Identity con Razor Pages

// 🔹 **Registrar Managers de `Application`**
builder.Services.AddScoped<IProductoManager, ProductoManager>();
builder.Services.AddScoped<IBultoManager, BultoManager>();
builder.Services.AddScoped<IDetalleBultoManager, DetalleBultoManager>();
builder.Services.AddScoped<ICategoriaProductoManager, CategoriaProductoManager>();
builder.Services.AddScoped<IAlmacenamientoEspecialManager, AlmacenamientoEspecialManager>();
builder.Services.AddScoped<IMovimientoManager, MovimientoManager>();
builder.Services.AddScoped<IEntradaManager, EntradaManager>();
builder.Services.AddScoped<ISalidaManager, SalidaManager>();
builder.Services.AddScoped<IEstadoBultoManager, EstadoBultoManager>();

var app = builder.Build();

// 🔹 **Configurar Middleware**
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// 🔹 **Configurar Rutas**
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapRazorPages();

// 🔹 **Forzar que el usuario pase por el login antes de ver algo**
app.Use(async (context, next) =>
{
    if (!context.User.Identity.IsAuthenticated && !context.Request.Path.StartsWithSegments("/Account/Login"))
    {
        context.Response.Redirect("/Account/Login");
        return;
    }

    await next();
});

app.Run();
