using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoFinalEmbutidosElTio.Data;
using ProyectoFinalEmbutidosElTio.Models;

namespace ProyectoFinalEmbutidosElTio.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class AdminPedidosController : Controller
    {
        private readonly AppDbContext _context;

        public AdminPedidosController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var pedidos = await _context.Pedidos
                .Include(p => p.Usuario)
                .Include(p => p.EstadoPedido)
                .OrderByDescending(p => p.FechaPedido)
                .ToListAsync();

            ViewData["Estados"] = await _context.EstadosPedido.ToListAsync();

            // Determine Payment Method
            var paypalIds = await _context.PagosPaypal.Select(p => p.IdPedido).ToListAsync();
            var stripeIds = await _context.PagosStripe.Select(p => p.IdPedido).ToListAsync();
            
            var paypalSet = new HashSet<int>(paypalIds);
            var stripeSet = new HashSet<int>(stripeIds);

            var metodosPago = new Dictionary<int, string>();
            foreach (var pedido in pedidos)
            {
                if (paypalSet.Contains(pedido.IdPedido))
                {
                    metodosPago[pedido.IdPedido] = "PayPal";
                }
                else if (stripeSet.Contains(pedido.IdPedido))
                {
                    metodosPago[pedido.IdPedido] = "Stripe";
                }
                else
                {
                    metodosPago[pedido.IdPedido] = "Físico";
                }
            }
            ViewData["MetodosPago"] = metodosPago;

            return View(pedidos);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, int idEstado)
        {
            var pedido = await _context.Pedidos.FindAsync(id);
            if (pedido == null)
            {
                return NotFound();
            }

            pedido.IdEstadoPedido = idEstado;
            _context.Update(pedido);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var pedido = await _context.Pedidos
                .Include(p => p.Usuario)
                .Include(p => p.EstadoPedido)
                .Include(p => p.DetallesPedido)
#pragma warning disable CS8620 // Argument cannot be used for parameter due to differences in the nullability of reference types.
                .ThenInclude(dp => dp.Producto)
#pragma warning restore CS8620
                .FirstOrDefaultAsync(m => m.IdPedido == id);

            if (pedido == null) return NotFound();

            return View(pedido);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmarEntrega(int id)
        {
            var pedido = await _context.Pedidos.FindAsync(id);
            if (pedido == null)
            {
                return NotFound();
            }

            // Set status to 4 (Entregado) assuming this implies Paid & Delivered
            pedido.IdEstadoPedido = 4;
            _context.Update(pedido);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}
