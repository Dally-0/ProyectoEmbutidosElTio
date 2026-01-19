using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoFinalEmbutidosElTio.Data;

namespace ProyectoFinalEmbutidosElTio.Controllers
{
    public class NoticiasController : Controller
    {
        private readonly AppDbContext _context;

        public NoticiasController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var noticias = await _context.Noticias
                .Include(n => n.UsuarioAdmin)
                .OrderByDescending(n => n.FechaPublicacion)
                .ToListAsync();
            return View(noticias);
        }
    }
}
