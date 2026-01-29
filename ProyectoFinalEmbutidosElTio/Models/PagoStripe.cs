using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProyectoFinalEmbutidosElTio.Models
{
    [Table("Pagos_Stripe")]
    public class PagoStripe
    {
        [Key]
        [Column("id_pago")]
        public int IdPago { get; set; }

        [Column("id_pedido")]
        public int IdPedido { get; set; }
        [ForeignKey("IdPedido")]
        public Pedido? Pedido { get; set; }

        [Required]
        [Column("id_sesion_stripe")]
        [StringLength(255)]
        public string IdSesionStripe { get; set; } = string.Empty;

        [Required]
        [Column("id_transaccion_stripe")]
        [StringLength(255)]
        public string? IdTransaccionStripe { get; set; }

        [Required]
        [Column("monto_pagado", TypeName = "decimal(18,2)")]
        public decimal MontoPagado { get; set; }

        [Column("estado_pago")]
        [StringLength(50)]
        public string? EstadoPago { get; set; }

        [Column("fecha_pago")]
        public DateTime? FechaPago { get; set; } = DateTime.Now;
    }
}
