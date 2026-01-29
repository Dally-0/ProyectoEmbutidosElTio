using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoFinalEmbutidosElTio.Data;
using ProyectoFinalEmbutidosElTio.Models;
using ProyectoFinalEmbutidosElTio.Models.ViewModels;
using System.Security.Claims;

namespace ProyectoFinalEmbutidosElTio.Controllers
{
    [Authorize]
    public class ClienteController : Controller
    {
        private readonly AppDbContext _context;

        public ClienteController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Perfil()
        {
            var userIdClaim = User.FindFirst("IdUsuario");
            if (userIdClaim == null)
            {
                return RedirectToAction("Login", "Account");
            }

            int userId = int.Parse(userIdClaim.Value);

            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.IdUsuario == userId);

            if (usuario == null)
            {
                return NotFound();
            }

            var pedidos = await _context.Pedidos
                .Include(p => p.EstadoPedido)
                .Where(p => p.IdUsuario == userId)
                .OrderByDescending(p => p.FechaPedido)
                .ToListAsync();

            var viewModel = new PerfilViewModel
            {
                Usuario = usuario,
                Pedidos = pedidos
            };

            return View(viewModel);
        }
    }
}
