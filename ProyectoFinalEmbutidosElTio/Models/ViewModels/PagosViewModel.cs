using ProyectoFinalEmbutidosElTio.Models;

namespace ProyectoFinalEmbutidosElTio.Models.ViewModels
{
    public class PagosViewModel
    {
        public List<PagoItem> Pagos { get; set; } = new List<PagoItem>();
        public decimal TotalIngresos { get; set; }
        public decimal TotalCostoProduccion { get; set; }
        public decimal TotalGanancia => TotalIngresos - TotalCostoProduccion;
        public string FiltroActual { get; set; } = "todos";
    }

    public class PagoItem
    {
        public int IdPedido { get; set; }
        public string Cliente { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
        public decimal Monto { get; set; }
        public string MetodoPago { get; set; } = string.Empty; // "PayPal" o "Efectivo/Otro"
        public string Estado { get; set; } = string.Empty;     // "Completado", "Pendiente", etc.
        public string Referencia { get; set; } = string.Empty; // ID Transacción PayPal o "-"
    }
}
