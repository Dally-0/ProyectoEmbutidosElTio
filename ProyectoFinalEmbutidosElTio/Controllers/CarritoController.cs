using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoFinalEmbutidosElTio.Data;
using ProyectoFinalEmbutidosElTio.Extensions;
using ProyectoFinalEmbutidosElTio.Models;
using ProyectoFinalEmbutidosElTio.Models.ViewModels;
using ProyectoFinalEmbutidosElTio.Services;
using System.Security.Claims;

namespace ProyectoFinalEmbutidosElTio.Controllers
{
    public class CarritoController : Controller
    {
        private readonly AppDbContext _context;
        private readonly PayPalService _payPalService;
        private readonly StripeService _stripeService;
        private readonly IConfiguration _configuration;
        private const string CartSessionKey = "Cart";

        public CarritoController(AppDbContext context, PayPalService payPalService, StripeService stripeService, IConfiguration configuration)
        {
            _context = context;
            _payPalService = payPalService;
            _stripeService = stripeService;
            _configuration = configuration;
        }


        public async Task<IActionResult> Index()
        {
            var cartViewModel = await GetCartViewModelAsync();
            return View(cartViewModel);
        }

        [HttpPost]
        public IActionResult AddToCart(int productId, int quantity)
        {
            // Validar stock antes de agregar
            var producto = _context.Productos.Find(productId);
            if (producto == null || !producto.Activo) return NotFound();

            var cart = HttpContext.Session.Get<List<CartItemDto>>(CartSessionKey) ?? new List<CartItemDto>();
            var existingItem = cart.FirstOrDefault(i => i.ProductId == productId);

            int cantidadFinal = quantity;
            if (existingItem != null)
            {
                cantidadFinal += existingItem.Quantity;
            }

            // Si la cantidad final supera el stock, ajustamos al máximo posible
            if (cantidadFinal > producto.Stock)
            {
                cantidadFinal = producto.Stock;
                // Opcional: Agregar TempData["Error"] = "Stock insuficiente";
            }

            if (existingItem != null)
            {
                existingItem.Quantity = cantidadFinal;
            }
            else
            {
                cart.Add(new CartItemDto { ProductId = productId, Quantity = cantidadFinal });
            }

            HttpContext.Session.Set(CartSessionKey, cart);
            return RedirectToAction("Index");
        }

        // ==========================================
        // NUEVAS ACCIONES PARA LA VISTA PREMIUM
        // ==========================================

        public IActionResult IncreaseQuantity(int productId)
        {
            var cart = HttpContext.Session.Get<List<CartItemDto>>(CartSessionKey) ?? new List<CartItemDto>();
            var item = cart.FirstOrDefault(i => i.ProductId == productId);

            if (item != null)
            {
                // Verificamos stock en tiempo real
                var productoBD = _context.Productos.Find(productId);
                if (productoBD != null && item.Quantity < productoBD.Stock)
                {
                    item.Quantity++;
                    HttpContext.Session.Set(CartSessionKey, cart);
                }
            }
            return RedirectToAction("Index");
        }

        public IActionResult DecreaseQuantity(int productId)
        {
            var cart = HttpContext.Session.Get<List<CartItemDto>>(CartSessionKey) ?? new List<CartItemDto>();
            var item = cart.FirstOrDefault(i => i.ProductId == productId);

            if (item != null)
            {
                if (item.Quantity > 1)
                {
                    item.Quantity--;
                }
                else
                {
                    // Si baja de 1, lo eliminamos
                    cart.Remove(item);
                }
                HttpContext.Session.Set(CartSessionKey, cart);
            }
            return RedirectToAction("Index");
        }

        public IActionResult RemoveFromCart(int productId)
        {
            var cart = HttpContext.Session.Get<List<CartItemDto>>(CartSessionKey) ?? new List<CartItemDto>();
            var item = cart.FirstOrDefault(i => i.ProductId == productId);
            if (item != null)
            {
                cart.Remove(item);
                HttpContext.Session.Set(CartSessionKey, cart);
            }
            return RedirectToAction("Index");
        }

        public IActionResult ClearCart()
        {
            HttpContext.Session.Remove(CartSessionKey);
            return RedirectToAction("Index");
        }

        // ==========================================
        // CHECKOUT Y CONFIRMACIÓN
        // ==========================================

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            var cartViewModel = await GetCartViewModelAsync();
            if (!cartViewModel.Items.Any())
            {
                return RedirectToAction("Index", "Tienda");
            }
            ViewBag.PayPalClientId = _configuration["PayPal:ClientId"];
            ViewBag.StripePublicKey = _configuration["Stripe:PublishableKey"];
            return View(cartViewModel);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> ConfirmOrder()
        {
            var cartViewModel = await GetCartViewModelAsync();
            if (!cartViewModel.Items.Any()) return RedirectToAction("Index");

            var userId = int.Parse(User.FindFirst("IdUsuario")?.Value ?? "0");

            // Crear Pedido
            var pedido = new Pedido
            {
                IdUsuario = userId,
                IdEstadoPedido = 1, // Pendiente
                FechaPedido = DateTime.Now,
                Total = cartViewModel.Total
            };

            _context.Pedidos.Add(pedido);
            await _context.SaveChangesAsync();

            // Crear Detalles y Actualizar Stock
            foreach (var item in cartViewModel.Items)
            {
                var detalle = new DetallePedido
                {
                    IdPedido = pedido.IdPedido,
                    IdProducto = item.Producto.IdProducto,
                    Cantidad = item.Quantity,
                    PrecioUnitario = item.Producto.Precio_final
                };
                _context.DetallesPedido.Add(detalle);

                // IMPORTANTE: Restar stock al confirmar compra
                var productoDb = await _context.Productos.FindAsync(item.Producto.IdProducto);
                if (productoDb != null)
                {
                    productoDb.Stock -= item.Quantity;
                    if (productoDb.Stock < 0) productoDb.Stock = 0; // Seguridad
                }
            }
            await _context.SaveChangesAsync();

            // Limpiar Carrito
            HttpContext.Session.Remove(CartSessionKey);

            return RedirectToAction("OrderConfirmation", new { id = pedido.IdPedido });
        }

        public IActionResult OrderConfirmation(int id)
        {
            return View(id);
        }

        // ==========================================
        // PAYPAL ACTIONS
        // ==========================================

        [HttpPost]
        public async Task<IActionResult> CreatePayPalOrder()
        {
            try 
            {
                var cart = await GetCartViewModelAsync();
                if (!cart.Items.Any())
                {
                    return BadRequest(new { message = "El carrito está vacío" });
                }

                var order = await _payPalService.CreateOrder(cart.Total);
                return Ok(order);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CapturePayPalOrder(string orderId)
        {
            try
            {
                var order = await _payPalService.CaptureOrder(orderId);
                
                // Verificar que el usuario esté autenticado
                var userIdStr = User.FindFirst("IdUsuario")?.Value;
                if (string.IsNullOrEmpty(userIdStr))
                {
                    return Unauthorized(new { message = "Usuario no autenticado" });
                }
                var userId = int.Parse(userIdStr);

                // Obtener carrito
                var cart = await GetCartViewModelAsync();
                if (!cart.Items.Any())
                {
                     return BadRequest(new { message = "El carrito está vacío o expiró" });
                }

                // Crear Pedido
                var pedido = new Pedido
                {
                    IdUsuario = userId,
                    FechaPedido = DateTime.Now,
                    Total = cart.Total,
                    IdEstadoPedido = 1 // Pendiente / Pagado / Completado check EstadoPedido Enum/Table if possible, defaulting to 1
                };

                _context.Pedidos.Add(pedido);
                await _context.SaveChangesAsync();

                // Crear Detalles
                foreach (var item in cart.Items)
                {
                    var detalle = new DetallePedido
                    {
                        IdPedido = pedido.IdPedido,
                        IdProducto = item.Producto.IdProducto,
                        Cantidad = item.Quantity,
                        PrecioUnitario = item.Producto.Precio_final
                    };
                    _context.DetallesPedido.Add(detalle);

                    // Descontar Stock
                    var prod = await _context.Productos.FindAsync(item.Producto.IdProducto);
                    if (prod != null)
                    {
                         prod.Stock -= item.Quantity;
                         if (prod.Stock < 0) prod.Stock = 0;
                    }
                }

                // Registrar Pago PayPal
                var monto = order.PurchaseUnits[0].Amount?.Value;
                if (string.IsNullOrEmpty(monto) && order.PurchaseUnits[0].Payments?.Captures?.Count > 0)
                {
                    monto = order.PurchaseUnits[0].Payments.Captures[0].Amount?.Value;
                }

                if (string.IsNullOrEmpty(monto)) monto = "0";

                var pago = new PagoPaypal
                {
                    IdPedido = pedido.IdPedido,
                    IdTransaccionPaypal = order.Id,
                    MontoPagado = decimal.Parse(monto, System.Globalization.CultureInfo.InvariantCulture),
                    EstadoPago = order.Status,
                    FechaPago = DateTime.Now
                };
                _context.PagosPaypal.Add(pago);
                
                await _context.SaveChangesAsync();

                // Limpiar carrito
                HttpContext.Session.Remove(CartSessionKey);

                return Ok(new { success = true, orderId = pedido.IdPedido });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = "Error procesando el pago: " + ex.Message });
            }
        }

        public IActionResult PaymentSuccess(int id)
        {
             return RedirectToAction("OrderConfirmation", new { id = id });
        }

        // ==========================================
        // STRIPE ACTIONS
        // ==========================================

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> CreateStripeSession()
        {
            try
            {
                var cart = await GetCartViewModelAsync();
                
                // Log de depuración
                if (cart == null || !cart.Items.Any()) 
                {
                    return BadRequest(new { error = "El carrito está vacío en la sesión." });
                }

                string domain = $"{this.Request.Scheme}://{this.Request.Host}";
                var session = _stripeService.CreateCheckoutSession(cart, domain);
                
                return Ok(new { sessionId = session.Id });
            }
            catch (Exception ex)
            {
                // Esto te ayudará a ver en la consola de Chrome el error real de la API de Stripe
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> StripeSuccess(string session_id)
        {
            var session = _stripeService.GetSession(session_id);
            if (session.PaymentStatus == "paid")
            {
                var userIdStr = User.FindFirst("IdUsuario")?.Value;
                if (string.IsNullOrEmpty(userIdStr)) userIdStr = "0"; // Handle properly
                var userId = int.Parse(userIdStr);

                var cart = await GetCartViewModelAsync();
                
                 // Crear Pedido
                var pedido = new Pedido
                {
                    IdUsuario = userId,
                    FechaPedido = DateTime.Now,
                    Total = cart.Total,
                    IdEstadoPedido = 1 // Completado
                };
                _context.Pedidos.Add(pedido);
                await _context.SaveChangesAsync();

                // Detalles
                foreach (var item in cart.Items)
                {
                    var detalle = new DetallePedido
                    {
                        IdPedido = pedido.IdPedido,
                        IdProducto = item.Producto.IdProducto,
                        Cantidad = item.Quantity,
                        PrecioUnitario = item.Producto.Precio_final
                    };
                    _context.DetallesPedido.Add(detalle);

                     // Stock
                     var prod = _context.Productos.Find(item.Producto.IdProducto);
                     if(prod != null) {
                         prod.Stock -= item.Quantity;
                         if(prod.Stock < 0) prod.Stock = 0;
                     }
                }

                // Registrar Pago
                var pago = new PagoStripe
                {
                    IdPedido = pedido.IdPedido,
                    IdSesionStripe = session.Id,
                    IdTransaccionStripe = session.PaymentIntentId,
                    MontoPagado = (decimal)(session.AmountTotal ?? 0) / 100, // Convert cents to units
                    EstadoPago = session.PaymentStatus,
                    FechaPago = DateTime.Now
                };
                _context.PagosStripe.Add(pago);
                await _context.SaveChangesAsync();

                HttpContext.Session.Remove(CartSessionKey);
                return RedirectToAction("OrderConfirmation", new { id = pedido.IdPedido });
            }

            return RedirectToAction("Checkout");
        }

        // Helper
        private async Task<CartViewModel> GetCartViewModelAsync()
        {
            var cartDtos = HttpContext.Session.Get<List<CartItemDto>>(CartSessionKey) ?? new List<CartItemDto>();
            var viewModel = new CartViewModel();

            foreach (var dto in cartDtos)
            {
                var product = await _context.Productos
                                            .Include(p => p.Categoria) // Incluir categoría para mostrar el nombre en el carrito
                                            .FirstOrDefaultAsync(p => p.IdProducto == dto.ProductId);
                if (product != null)
                {
                    viewModel.Items.Add(new CartItemViewModel
                    {
                        Producto = product,
                        Quantity = dto.Quantity
                    });
                }
            }
            return viewModel;
        }
    }
}
