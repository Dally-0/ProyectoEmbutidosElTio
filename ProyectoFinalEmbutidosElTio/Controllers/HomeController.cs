using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // Importante para Include y ToListAsync
using ProyectoFinalEmbutidosElTio.Data; // Importante para AppDbContext
using ProyectoFinalEmbutidosElTio.Models;

namespace ProyectoFinalEmbutidosElTio.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AppDbContext _context; // 1. Variable para la BD

        // 2. Inyectamos el Contexto en el constructor
        public HomeController(ILogger<HomeController> logger, AppDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        // 3. Método Index actualizado para enviar datos a la vista
        public async Task<IActionResult> Index()
        {
            // Obtenemos los últimos 3 productos que estén activos (Activo == true)
            // Ordenados por fecha de ingreso descendente (los más nuevos primero)
            var ultimosProductos = await _context.Productos
                .Include(p => p.Categoria) // Incluimos la categoría para mostrar el nombre (ej: "Res", "Cerdo")
                .Where(p => p.Activo == true)
                .OrderByDescending(p => p.IdProducto)
                .Take(3)
                .ToListAsync();

            // Enviamos la lista a la Vista para evitar el NullReferenceException
            return View(ultimosProductos);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult About()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}