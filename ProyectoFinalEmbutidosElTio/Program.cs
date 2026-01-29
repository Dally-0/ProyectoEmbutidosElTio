using Microsoft.EntityFrameworkCore;
using ProyectoFinalEmbutidosElTio.Data;
using ProyectoFinalEmbutidosElTio.Models;
using System.Security.Cryptography;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

builder.Services.AddDbContext<ProyectoFinalEmbutidosElTio.Data.AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddAuthentication(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
    });


builder.Services.AddScoped<ProyectoFinalEmbutidosElTio.Services.PayPalService>();
builder.Services.AddScoped<ProyectoFinalEmbutidosElTio.Services.StripeService>();

var app = builder.Build();

// --- DATABASE SEEDER (Fixes Login Issues) ---
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        context.Database.EnsureCreated(); // Ensure DB exists

        // 1. Ensure Roles Exist
        if (!context.Roles.Any())
        {
            context.Roles.AddRange(
                new Rol { NombreRol = "Administrador" },
                new Rol { NombreRol = "Cliente" }
            );
            context.SaveChanges();
        }

        // 2. Ensure Admin User Exists
        if (!context.Usuarios.Any(u => u.Correo == "admin@eltio.com"))
        {
            context.Usuarios.Add(new Usuario
            {
                Nombre = "Admin",
                ApPaterno = "Sistema",
                ApMaterno = "Principal",
                Celular = "00000000",
                Correo = "admin@eltio.com",
                PasswordHash = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes("Admin123!")), // Default Password
                IdRol = 1, // Admin (assuming 1 is Admin based on insert order)
                IdEstadoUsuario = 1,
                FechaRegistro = DateTime.Now
            });
            context.SaveChanges();
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred seeding the DB.");
    }
}
// --------------------------------------------

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.UseSession();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
