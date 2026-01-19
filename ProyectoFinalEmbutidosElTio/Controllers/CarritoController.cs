using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoFinalEmbutidosElTio.Data;
using ProyectoFinalEmbutidosElTio.Extensions;
using ProyectoFinalEmbutidosElTio.Models;
using ProyectoFinalEmbutidosElTio.Models.ViewModels;
using System.Security.Claims;

namespace ProyectoFinalEmbutidosElTio.Controllers
{
    public class CarritoController : Controller
    {
        private readonly AppDbContext _context;
        private const string CartSessionKey = "Cart";

        public CarritoController(AppDbContext context)
        {
            _context = context;
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
                    PrecioUnitario = item.Producto.Precio
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
