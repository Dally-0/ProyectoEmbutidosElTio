using ProyectoFinalEmbutidosElTio.Models;

namespace ProyectoFinalEmbutidosElTio.Models.ViewModels
{
    public class DashboardViewModel
    {
        public int TotalPedidos { get; set; }
        public int TotalUsuarios { get; set; }
        public int TotalNoticias { get; set; }
        public int TotalProductos { get; set; }
        public List<Pedido> UltimosPedidos { get; set; } = new List<Pedido>();
    }
}
