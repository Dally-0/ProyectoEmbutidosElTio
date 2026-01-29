using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoFinalEmbutidosElTio.Data;
using ProyectoFinalEmbutidosElTio.Models;

namespace ProyectoFinalEmbutidosElTio.Controllers
{
    public class TiendaController : Controller
    {
        private readonly AppDbContext _context;

        public TiendaController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(int? categoriaId)
        {
            var query = _context.Productos.AsQueryable();

            if (categoriaId.HasValue)
            {
                query = query.Where(p => p.IdCategoria == categoriaId);
            }

            ViewData["Categorias"] = await _context.Categorias.ToListAsync();
            ViewData["CurrentCategoria"] = categoriaId;

            return View(await query.ToListAsync());
        }

        public async Task<IActionResult> Details(int id)
        {
            var producto = await _context.Productos
                .Include(p => p.Categoria)
                .FirstOrDefaultAsync(m => m.IdProducto == id);

            if (producto == null)
            {
                return NotFound();
            }

            return View(producto);
        }
        // En TiendaController.cs

        // Acción para filtrar asíncronamente (AJAX)
        [HttpGet]
        public async Task<IActionResult> FiltrarProductos(string busqueda, int? categoriaId, decimal? precioMax, string orden)
        {
            var query = _context.Productos.Include(p => p.Categoria).AsQueryable();

            // 1. Filtro por Categoría
            if (categoriaId.HasValue)
            {
                query = query.Where(p => p.IdCategoria == categoriaId);
            }

            // 2. Filtro por Búsqueda (Nombre o Descripción)
            if (!string.IsNullOrEmpty(busqueda))
            {
                query = query.Where(p => p.Nombre.Contains(busqueda) || p.Descripcion.Contains(busqueda));
            }

            // 3. Filtro por Precio Máximo
            if (precioMax.HasValue && precioMax > 0)
            {
                query = query.Where(p => p.Precio_final <= precioMax);
            }

            // 4. Ordenamiento
            switch (orden)
            {
                case "precio_asc":
                    query = query.OrderBy(p => p.Precio_final);
                    break;
                case "precio_desc":
                    query = query.OrderByDescending(p => p.Precio_final);
                    break;
                case "nombre_asc":
                    query = query.OrderBy(p => p.Nombre);
                    break;
                default: // Relevancia (por defecto, o por ID)
                    query = query.OrderByDescending(p => p.IdProducto);
                    break;
            }

            var productos = await query.ToListAsync();

            // Retornamos una vista parcial con los resultados
            return PartialView("_ProductosGrid", productos);
        }

    }
}
