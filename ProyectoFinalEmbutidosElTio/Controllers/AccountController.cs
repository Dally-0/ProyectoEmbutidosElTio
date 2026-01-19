using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoFinalEmbutidosElTio.Data;
using ProyectoFinalEmbutidosElTio.Models;
using ProyectoFinalEmbutidosElTio.Models.ViewModels;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace ProyectoFinalEmbutidosElTio.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;

        public AccountController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _context.Usuarios
                    .Include(u => u.Rol)
                    .FirstOrDefaultAsync(u => u.Correo == model.Correo);

                if (user != null && VerifyPassword(model.Password, user.PasswordHash))
                {
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, user.Correo),
                        new Claim("FullName", $"{user.Nombre} {user.ApPaterno}"),
                        new Claim(ClaimTypes.Role, user.Rol?.NombreRol ?? "Cliente"),
                        new Claim("IdUsuario", user.IdUsuario.ToString())
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

                    if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                        return Redirect(model.ReturnUrl);

                    return RedirectToAction("Index", "Home");
                }
                
                ModelState.AddModelError(string.Empty, "Intento de inicio de sesión inválido.");
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                if (await _context.Usuarios.AnyAsync(u => u.Correo == model.Correo))
                {
                    ModelState.AddModelError("Correo", "El correo ya está registrado.");
                    return View(model);
                }

                var usuario = new Usuario
                {
                    Nombre = model.Nombre,
                    ApPaterno = model.ApPaterno,
                    ApMaterno = model.ApMaterno,
                    Celular = model.Celular,
                    Correo = model.Correo,
                    PasswordHash = HashPassword(model.Password),
                    IdRol = 2, // 2 = Cliente (Default) based on SQL Insert order
                    IdEstadoUsuario = 1, // 1 = Activo
                    FechaRegistro = DateTime.Now
                };

                _context.Usuarios.Add(usuario);
                await _context.SaveChangesAsync();

                // Auto login after register
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, usuario.Correo),
                    new Claim("FullName", $"{usuario.Nombre} {usuario.ApPaterno}"),
                    new Claim(ClaimTypes.Role, "Cliente"),
                    new Claim("IdUsuario", usuario.IdUsuario.ToString())
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

                return RedirectToAction("Index", "Home");
            }
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        private byte[] HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                return sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            }
        }

        private bool VerifyPassword(string password, byte[] storedHash)
        {
            var hash = HashPassword(password);
            return hash.SequenceEqual(storedHash);
        }
    }
}
