using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ProyectoFinalEmbutidosElTio.Models
{
    [Table("Roles")]
    public class Rol
    {
        [Key]
        [Column("id_rol")]
        public int IdRol { get; set; }

        [Required]
        [Column("nombre_rol")]
        [StringLength(50)]
        public string NombreRol { get; set; } = string.Empty;
    }

    [Table("Estados_Usuario")]
    public class EstadoUsuario
    {
        [Key]
        [Column("id_estado_usuario")]
        public int IdEstadoUsuario { get; set; }

        [Required]
        [Column("nombre_estado")]
        [StringLength(50)]
        public string NombreEstado { get; set; } = string.Empty;
    }

    [Table("Estados_Pedido")]
    public class EstadoPedido
    {
        [Key]
        [Column("id_estado_pedido")]
        public int IdEstadoPedido { get; set; }

        [Required]
        [Column("nombre_estado")]
        [StringLength(50)]
        public string NombreEstado { get; set; } = string.Empty;
    }

    [Table("Categorias")]
    public class Categoria
    {
        [Key]
        [Column("id_categoria")]
        public int IdCategoria { get; set; }

        [Required]
        [Column("nombre_tipo")]
        [StringLength(100)]
        public string NombreTipo { get; set; } = string.Empty;

        [Column("descripcion")]
        public string? Descripcion { get; set; }
    }
}
