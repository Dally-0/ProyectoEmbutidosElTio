using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoFinalEmbutidosElTio.Data;
using ProyectoFinalEmbutidosElTio.Models.ViewModels;

namespace ProyectoFinalEmbutidosElTio.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class DashboardController : Controller
    {
        private readonly AppDbContext _context;

        public DashboardController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var viewModel = new DashboardViewModel
            {
                TotalPedidos = await _context.Pedidos.CountAsync(),
                TotalUsuarios = await _context.Usuarios.CountAsync(),
                TotalNoticias = await _context.Noticias.CountAsync(),
                TotalProductos = await _context.Productos.CountAsync(),
                UltimosPedidos = await _context.Pedidos
                    .Include(p => p.Usuario)
                    .Include(p => p.EstadoPedido)
                    .OrderByDescending(p => p.FechaPedido)
                    .Take(5)
                    .ToListAsync()
            };

            return View(viewModel);
        }

        // Additional Actions for Managing Products/News could go here
    }
}
