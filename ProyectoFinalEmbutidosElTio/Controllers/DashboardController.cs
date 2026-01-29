using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoFinalEmbutidosElTio.Data;
using ProyectoFinalEmbutidosElTio.Models;
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

        public async Task<IActionResult> Pagos(string filtro = "todos")
        {
            var pagosList = new List<PagoItem>();

            // 1. Obtener pagos de PayPal
            var pagosPaypal = await _context.PagosPaypal
                .Include(p => p.Pedido)
                .ThenInclude(p => p.Usuario)
                .ToListAsync();

            foreach (var p in pagosPaypal)
            {
                pagosList.Add(new PagoItem
                {
                    IdPedido = p.IdPedido,
                    Cliente = p.Pedido?.Usuario != null ? $"{p.Pedido.Usuario.Nombre} {p.Pedido.Usuario.ApPaterno}" : "Desconocido",
                    Fecha = p.FechaPago ?? DateTime.MinValue,
                    Monto = p.MontoPagado,
                    MetodoPago = "PayPal",
                    Estado = p.EstadoPago ?? "Desconocido",
                    Referencia = p.IdTransaccionPaypal
                });
            }

            // 2. Obtener pagos de Stripe
            var pagosStripe = await _context.PagosStripe
                .Include(p => p.Pedido)
                .ThenInclude(p => p.Usuario)
                .ToListAsync();

            foreach (var p in pagosStripe)
            {
                pagosList.Add(new PagoItem
                {
                    IdPedido = p.IdPedido,
                    Cliente = p.Pedido?.Usuario != null ? $"{p.Pedido.Usuario.Nombre} {p.Pedido.Usuario.ApPaterno}" : "Desconocido",
                    Fecha = p.FechaPago ?? DateTime.MinValue,
                    Monto = p.MontoPagado, // Assuming MontoPagado exists in PagoStripe
                    MetodoPago = "Stripe",
                    Estado = p.EstadoPago ?? "Desconocido",
                    Referencia = p.IdTransaccionStripe
                });
            }

            // 3. Obtener otros pedidos (Efectivo/Transferencia)
            // Excluir los que ya están en PayPal o Stripe
            var pedidosIdsPagados = pagosPaypal.Select(p => p.IdPedido)
                .Union(pagosStripe.Select(s => s.IdPedido))
                .ToHashSet();

            var pedidosCandidatos = await _context.Pedidos
                .Include(p => p.Usuario)
                .Include(p => p.EstadoPedido)
                .Where(p => p.IdEstadoPedido == 2 || p.IdEstadoPedido == 3 || p.IdEstadoPedido == 4)
                .ToListAsync();

            var pedidosOtros = pedidosCandidatos
                .Where(p => !pedidosIdsPagados.Contains(p.IdPedido))
                .ToList();

            foreach (var p in pedidosOtros)
            {
                pagosList.Add(new PagoItem
                {
                    IdPedido = p.IdPedido,
                    Cliente = p.Usuario != null ? $"{p.Usuario.Nombre} {p.Usuario.ApPaterno}" : "Desconocido",
                    Fecha = p.FechaPedido ?? DateTime.MinValue,
                    Monto = p.Total,
                    MetodoPago = "Efectivo/Otro",
                    Estado = p.EstadoPedido?.NombreEstado ?? "Pagado",
                    Referencia = "-"
                });
            }

            // Filtrar
            if (filtro == "paypal")
            {
                pagosList = pagosList.Where(p => p.MetodoPago == "PayPal").ToList();
            }
            else if (filtro == "stripe")
            {
                pagosList = pagosList.Where(p => p.MetodoPago == "Stripe").ToList();
            }
            else if (filtro == "otros")
            {
                pagosList = pagosList.Where(p => p.MetodoPago == "Efectivo/Otro").ToList();
            }

            // Ordenar por fecha descendente
            pagosList = pagosList.OrderByDescending(p => p.Fecha).ToList();

            // 3. Calcular Costo de Producción Total (de todos los pedidos listados)
            decimal totalCostoProduccion = 0;
            
            // Recolectar IDs de pedidos para consulta eficiente
            var pedidoIds = pagosList.Select(p => p.IdPedido).ToList();

            if (pedidoIds.Any())
            {
                var detalles = await _context.DetallesPedido
                    .Include(d => d.Producto)
                    .Where(d => d.IdPedido.HasValue && pedidoIds.Contains(d.IdPedido.Value))
                    .ToListAsync();

                totalCostoProduccion = detalles.Sum(d => d.Cantidad * (d.Producto?.PrecioProduccion ?? 0));
            }

            var viewModel = new PagosViewModel
            {
                Pagos = pagosList,
                TotalIngresos = pagosList.Sum(p => p.Monto),
                TotalCostoProduccion = totalCostoProduccion,
                FiltroActual = filtro
            };

            return View(viewModel);
        }

        public async Task<IActionResult> Inventario(string filtro = "todos")
        {
            var query = _context.Productos
                .Include(p => p.Categoria)
                .AsQueryable();

            ViewData["FiltroActual"] = filtro;

            List<Producto> productos;

            switch (filtro)
            {
                case "vencidos":
                    // Productos ya vencidos
                    query = query.Where(p => p.FechaVencimiento < DateTime.Now);
                    productos = await query.OrderBy(p => p.FechaVencimiento).ToListAsync();
                    break;
                case "por_vencer":
                     // Vencen en los próximos 30 días
                    var limitDate = DateTime.Now.AddDays(30);
                    query = query.Where(p => p.FechaVencimiento >= DateTime.Now && p.FechaVencimiento <= limitDate);
                    productos = await query.OrderBy(p => p.FechaVencimiento).ToListAsync();
                    break;
                case "stock_bajo":
                    // Stock menor o igual al mínimo (default 10)
                    query = query.Where(p => p.Stock <= (p.StockMinimo ?? 10));
                    productos = await query.OrderBy(p => p.Stock).ToListAsync();
                    break;
                case "stock_alto":
                    // Sort quantity High to Low
                    productos = await query.OrderByDescending(p => p.Stock).ToListAsync();
                    break;
                default: // "todos" or invalid
                     // Default: Order by Stock ascending (Menor a Mayor) as requested
                    productos = await query.OrderBy(p => p.Stock).ToListAsync();
                    break;
            }

            return View(productos);
        }

        // Additional Actions for Managing Products/News could go here
    }
}
