using ProyectoFinalEmbutidosElTio.Models;

namespace ProyectoFinalEmbutidosElTio.Models.ViewModels
{
    public class PerfilViewModel
    {
        public Usuario? Usuario { get; set; }
        public List<Pedido>? Pedidos { get; set; }
    }
}
