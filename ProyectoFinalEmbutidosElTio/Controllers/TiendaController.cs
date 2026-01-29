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

        public async Task<IActionResult> Index(string searchString, int? categoriaId)
        {
            var query = _context.Productos.AsQueryable();

            if (categoriaId.HasValue)
            {
                query = query.Where(p => p.IdCategoria == categoriaId);
            }

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(p => p.Nombre.Contains(searchString));
            }

            ViewData["Categorias"] = await _context.Categorias.ToListAsync();
            ViewData["CurrentCategoria"] = categoriaId;
            ViewData["CurrentFilter"] = searchString;

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
    }
}
