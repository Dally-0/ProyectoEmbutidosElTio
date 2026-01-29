using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using ProyectoFinalEmbutidosElTio.Data;

using ProyectoFinalEmbutidosElTio.Extensions;
using ProyectoFinalEmbutidosElTio.Models;
using ProyectoFinalEmbutidosElTio.Services;
using ProyectoFinalEmbutidosElTio.Models.ViewModels;
using System.Security.Claims;

namespace ProyectoFinalEmbutidosElTio.Controllers
{
    public class CarritoController : Controller
    {
        private readonly AppDbContext _context;
        private readonly PayPalService _payPalService;
        private readonly IConfiguration _configuration;
        private const string CartSessionKey = "Cart";

        public CarritoController(AppDbContext context, PayPalService payPalService, IConfiguration configuration)
        {
            _context = context;
            _payPalService = payPalService;
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
            var cart = HttpContext.Session.Get<List<CartItemDto>>(CartSessionKey) ?? new List<CartItemDto>();
            
            var existingItem = cart.FirstOrDefault(i => i.ProductId == productId);
            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
            }
            else
            {
                cart.Add(new CartItemDto { ProductId = productId, Quantity = quantity });
            }

            HttpContext.Session.Set(CartSessionKey, cart);
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
            return View(cartViewModel);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> ConfirmOrder()
        {
            var cartViewModel = await GetCartViewModelAsync();
            if (!cartViewModel.Items.Any()) return RedirectToAction("Index");

            var userId = int.Parse(User.FindFirst("IdUsuario")?.Value ?? "0");
            
            // Create Order
            var pedido = new Pedido
            {
                IdUsuario = userId,
                IdEstadoPedido = 1, // Pendiente
                FechaPedido = DateTime.Now,
                Total = cartViewModel.Total
            };

            _context.Pedidos.Add(pedido);
            await _context.SaveChangesAsync();

            // Create Order Details
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
            }
            await _context.SaveChangesAsync();

            // Clear Cart
            HttpContext.Session.Remove(CartSessionKey);

            return RedirectToAction("OrderConfirmation", new { id = pedido.IdPedido });
        }

        public IActionResult OrderConfirmation(int id)
        {
            return View(id);
        }

        // --- PayPal Endpoints ---

        [HttpPost]
        public async Task<IActionResult> CreatePayPalOrder()
        {
            try
            {
                var cartViewModel = await GetCartViewModelAsync();
                if (!cartViewModel.Items.Any()) return BadRequest(new { message = "El carrito está vacío." });

                var order = await _payPalService.CreateOrder(cartViewModel.Total);
                
                if (order == null || string.IsNullOrEmpty(order.Id))
                {
                    return StatusCode(500, new { message = "No se pudo obtener el ID de la orden de PayPal." });
                }

                return Ok(new { id = order.Id });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PAYPAL ERROR] {DateTime.Now}: {ex.Message} {ex.StackTrace}");
                // System.IO.File.AppendAllText("debug_log.txt", $"{DateTime.Now}: Error CreatePayPalOrder: {ex.Message} {ex.StackTrace}\n");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CapturePayPalOrder(string orderId)
        {
            try
            {
                var capturedOrder = await _payPalService.CaptureOrder(orderId);

                if (capturedOrder?.Status == "COMPLETED")
                {
                    // Guardar el pedido en la base de datos
                    var cartViewModel = await GetCartViewModelAsync();
                    var userId = int.Parse(User.FindFirst("IdUsuario")?.Value ?? "0");

                    var pedido = new Pedido
                    {
                        IdUsuario = userId,
                        IdEstadoPedido = 2, // Pagado
                        FechaPedido = DateTime.Now,
                        Total = cartViewModel.Total
                    };
                    _context.Pedidos.Add(pedido);
                    await _context.SaveChangesAsync();

                    // Detalles
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
                        
                        // Opcional: Descontar stock aquí
                    }
                    await _context.SaveChangesAsync();

                    // Registrar Pago PayPal
                    var pago = new PagoPaypal
                    {
                         IdPedido = pedido.IdPedido,
                         IdTransaccionPaypal = capturedOrder.Id, // ID de la orden PayPal
                         MontoPagado = cartViewModel.Total,
                         EstadoPago = capturedOrder.Status,
                         FechaPago = DateTime.Now
                    };
                    _context.PagosPaypal.Add(pago);
                    await _context.SaveChangesAsync();

                    // Limpiar Carrito
                    HttpContext.Session.Remove(CartSessionKey);

                    return Ok(new { success = true, orderId = pedido.IdPedido });
                }

                return BadRequest(new { message = "El pago no pudo ser completado." });
            }
            catch (Exception ex)
            {
                 return StatusCode(500, new { message = ex.Message });
            }
        }

        public IActionResult PaymentSuccess(int id)
        {
            return View(id);
        }

        private async Task<CartViewModel> GetCartViewModelAsync()
        {
            var cartDtos = HttpContext.Session.Get<List<CartItemDto>>(CartSessionKey) ?? new List<CartItemDto>();
            var viewModel = new CartViewModel();

            foreach (var dto in cartDtos)
            {
                var product = await _context.Productos.FindAsync(dto.ProductId);
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
