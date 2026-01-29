using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProyectoFinalEmbutidosElTio.Models
{
    [Table("Usuarios")]
    public class Usuario
    {
        [Key]
        [Column("id_usuario")]
        public int IdUsuario { get; set; }

        [Required]
        [Column("nombre")]
        [StringLength(100)]
        public string Nombre { get; set; } = string.Empty;

        [Required]
        [Column("ap_paterno")]
        [StringLength(100)]
        public string ApPaterno { get; set; } = string.Empty;

        [Column("ap_materno")]
        [StringLength(100)]
        public string? ApMaterno { get; set; }

        [Column("celular")]
        [StringLength(20)]
        public string? Celular { get; set; }

        [Required]
        [Column("correo")]
        [StringLength(150)]
        public string Correo { get; set; } = string.Empty;

        [Required]
        [Column("password_hash")]
        public byte[] PasswordHash { get; set; } = Array.Empty<byte>();

        [Column("id_rol")]
        public int? IdRol { get; set; }
        [ForeignKey("IdRol")]
        public Rol? Rol { get; set; }

        [Column("id_estado_usuario")]
        public int? IdEstadoUsuario { get; set; }
        [ForeignKey("IdEstadoUsuario")]
        public EstadoUsuario? EstadoUsuario { get; set; }

        [Column("fecha_registro")]
        public DateTime? FechaRegistro { get; set; } = DateTime.Now;

        [Column("activo")]
        public bool Activo { get; set; } = true;
    }

    [Table("Productos")]
    public class Producto
    {
        [Key]
        [Column("id_producto")]
        public int IdProducto { get; set; }

        [Required]
        [Column("nombre")]
        [StringLength(150)]
        public string Nombre { get; set; } = string.Empty;

        [Column("descripcion")]
        public string? Descripcion { get; set; }

        [Column("id_categoria")]
        public int? IdCategoria { get; set; }
        [ForeignKey("IdCategoria")]
        public Categoria? Categoria { get; set; }

        [Required]
        [Column("precio_produccion", TypeName = "decimal(18,2)")]
        public decimal PrecioProduccion { get; set; }

        [Required]
        [Column("precio_final", TypeName = "decimal(18,2)")]
        public decimal Precio_final { get; set; }

        [Required]
        [Column("stock")]
        public int Stock { get; set; }

        [Column("stock_minimo")]
        public int? StockMinimo { get; set; }

        [Column("fecha_vencimiento")]
        public DateTime? FechaVencimiento { get; set; }

        [Column("fecha_ingreso")]
        public DateTime? FechaIngreso { get; set; } = DateTime.Now;

        [Column("imagen_url")]
        [StringLength(255)]
        public string? ImagenUrl { get; set; }

        [Column("activo")]
        public bool Activo { get; set; } = true;
    }

    [Table("Pedidos")]
    public class Pedido
    {
        [Key]
        [Column("id_pedido")]
        public int IdPedido { get; set; }

        [Column("id_usuario")]
        public int? IdUsuario { get; set; }
        [ForeignKey("IdUsuario")]
        public Usuario? Usuario { get; set; }

        [Column("id_estado_pedido")]
        public int? IdEstadoPedido { get; set; }
        [ForeignKey("IdEstadoPedido")]
        public EstadoPedido? EstadoPedido { get; set; }

        [Column("fecha_pedido")]
        public DateTime? FechaPedido { get; set; } = DateTime.Now;

        [Required]
        [Column("total", TypeName = "decimal(18,2)")]
        public decimal Total { get; set; }

        public ICollection<DetallePedido>? DetallesPedido { get; set; }
    }

    [Table("Detalle_Pedido")]
    public class DetallePedido
    {
        [Key]
        [Column("id_detalle")]
        public int IdDetalle { get; set; }

        [Column("id_pedido")]
        public int? IdPedido { get; set; }
        [ForeignKey("IdPedido")]
        public Pedido? Pedido { get; set; }

        [Column("id_producto")]
        public int? IdProducto { get; set; }
        [ForeignKey("IdProducto")]
        public Producto? Producto { get; set; }

        [Required]
        [Column("cantidad")]
        public int Cantidad { get; set; }

        [Required]
        [Column("precio_unitario", TypeName = "decimal(18,2)")]
        public decimal PrecioUnitario { get; set; }
    }

    [Table("Noticias")]
    public class Noticia
    {
        [Key]
        [Column("id_noticia")]
        public int IdNoticia { get; set; }

        [Required]
        [Column("titulo")]
        [StringLength(200)]
        public string Titulo { get; set; } = string.Empty;

        [Required]
        [Column("texto_noticia")]
        public string TextoNoticia { get; set; } = string.Empty;

        [Column("fecha_publicacion")]
        public DateTime? FechaPublicacion { get; set; } = DateTime.Now;

        [Column("id_usuario_admin")]
        public int? IdUsuarioAdmin { get; set; }
        [ForeignKey("IdUsuarioAdmin")]
        public Usuario? UsuarioAdmin { get; set; }
    }
    [Table("Pagos_Paypal")]
    public class PagoPaypal
    {
        [Key]
        [Column("id_pago")]
        public int IdPago { get; set; }

        [Column("id_pedido")]
        public int IdPedido { get; set; }
        [ForeignKey("IdPedido")]
        public Pedido? Pedido { get; set; }

        [Required]
        [Column("id_transaccion_paypal")]
        [StringLength(100)]
        public string IdTransaccionPaypal { get; set; } = string.Empty;

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
