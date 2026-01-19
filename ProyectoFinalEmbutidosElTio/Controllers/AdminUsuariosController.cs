using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ProyectoFinalEmbutidosElTio.Data;
using ProyectoFinalEmbutidosElTio.Models;

namespace ProyectoFinalEmbutidosElTio.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class AdminUsuariosController : Controller
    {
        private readonly AppDbContext _context;

        public AdminUsuariosController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var usuarios = await _context.Usuarios
                .Include(u => u.Rol)
                .Include(u => u.EstadoUsuario)
                .ToListAsync();
            return View(usuarios);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null) return NotFound();

            ViewData["IdRol"] = new SelectList(_context.Roles, "IdRol", "NombreRol", usuario.IdRol);
            ViewData["IdEstadoUsuario"] = new SelectList(_context.EstadosUsuario, "IdEstadoUsuario", "NombreEstado", usuario.IdEstadoUsuario);
            return View(usuario);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("IdUsuario,Nombre,ApPaterno,ApMaterno,Celular,Correo,IdRol,IdEstadoUsuario,Activo")] Usuario usuario)
        {
            // We only bind fields we want to allow admin to change. PasswordHash is excluded.
            // However, EF Core update needs the full entity or purely attached modifications.
            // Better to fetch existing and update properties.
            
            if (id != usuario.IdUsuario) return NotFound();

            var existingUser = await _context.Usuarios.FindAsync(id);
            if (existingUser == null) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    existingUser.Nombre = usuario.Nombre;
                    existingUser.ApPaterno = usuario.ApPaterno;
                    existingUser.ApMaterno = usuario.ApMaterno;
                    existingUser.Celular = usuario.Celular;
                    existingUser.Correo = usuario.Correo;
                    existingUser.IdRol = usuario.IdRol;
                    existingUser.IdEstadoUsuario = usuario.IdEstadoUsuario;
                    existingUser.Activo = usuario.Activo;
                    
                    _context.Update(existingUser);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UsuarioExists(usuario.IdUsuario)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["IdRol"] = new SelectList(_context.Roles, "IdRol", "NombreRol", usuario.IdRol);
            ViewData["IdEstadoUsuario"] = new SelectList(_context.EstadosUsuario, "IdEstadoUsuario", "NombreEstado", usuario.IdEstadoUsuario);
            return View(usuario);
        }

        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null) return NotFound();

            usuario.Activo = !usuario.Activo;
            _context.Update(usuario);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool UsuarioExists(int id)
        {
            return _context.Usuarios.Any(e => e.IdUsuario == id);
        }
    }
}
