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

            // 2. Obtener otros pedidos que NO tienen pago PayPal (asumimos efectivo/transferencia si el estado es Pagado/Enviado/Entregado)
            // Esto es una simplificación. Idealmente tendríamos una tabla de pagos unificada.
            // Para este ejemplo, tomamos pedidos con estado "Pagado" (id 2), "Enviado" (3), "Entregado" (4)
            // que NO estén en la tabla de pagos paypal.
            
            // 2. Obtener otros pedidos que NO tienen pago PayPal
            // Para evitar problemas de compatibilidad SQL (OPENJSON) con listas grandes en Contains, 
            // traemos los candidatos y filtramos en memoria (adecuado para volumen de datos de este proyecto).
            
            var pedidosIdsEnPaypal = pagosPaypal.Select(p => p.IdPedido).ToHashSet(); // HashSet para búsqueda rápida

            var pedidosCandidatos = await _context.Pedidos
                .Include(p => p.Usuario)
                .Include(p => p.EstadoPedido)
                .Where(p => p.IdEstadoPedido == 2 || p.IdEstadoPedido == 3 || p.IdEstadoPedido == 4)
                .ToListAsync();

            var pedidosOtros = pedidosCandidatos
                .Where(p => !pedidosIdsEnPaypal.Contains(p.IdPedido))
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

                totalCostoProduccion = detalles.Sum(d => d.Cantidad * d.Producto.PrecioProduccion);
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

        // Additional Actions for Managing Products/News could go here
    }
}
