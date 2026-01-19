using ProyectoFinalEmbutidosElTio.Models;

namespace ProyectoFinalEmbutidosElTio.Models.ViewModels
{
    public class CartItemDto
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }

    public class CartItemViewModel
    {
        public Producto Producto { get; set; } = new Producto();
        public int Quantity { get; set; }
        public decimal Subtotal => Producto.Precio * Quantity;
    }

    public class CartViewModel
    {
        public List<CartItemViewModel> Items { get; set; } = new List<CartItemViewModel>();
        public decimal Total => Items.Sum(i => i.Subtotal);
    }
}
