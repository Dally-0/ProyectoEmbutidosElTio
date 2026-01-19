using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProyectoFinalEmbutidosElTio.Data;
using ProyectoFinalEmbutidosElTio.Models;
using System.Security.Claims;

namespace ProyectoFinalEmbutidosElTio.Controllers
{
    [Authorize(Roles = "Administrador")]
    public class AdminNoticiasController : Controller
    {
        private readonly AppDbContext _context;

        public AdminNoticiasController(AppDbContext context)
        {
            _context = context;
        }

        // GET: AdminNoticias
        public async Task<IActionResult> Index()
        {
            var noticias = await _context.Noticias
                .Include(n => n.UsuarioAdmin)
                .OrderByDescending(n => n.FechaPublicacion)
                .ToListAsync();
            return View(noticias);
        }

        // GET: AdminNoticias/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: AdminNoticias/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Titulo,TextoNoticia")] Noticia noticia)
        {
            if (ModelState.IsValid)
            {
                // Set default values
                noticia.FechaPublicacion = DateTime.Now;
                
                // Get Current User ID (Admin)
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (int.TryParse(userId, out int id))
                {
                    noticia.IdUsuarioAdmin = id;
                }

                _context.Add(noticia);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(noticia);
        }

        // GET: AdminNoticias/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var noticia = await _context.Noticias.FindAsync(id);
            if (noticia == null)
            {
                return NotFound();
            }
            return View(noticia);
        }

        // POST: AdminNoticias/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("IdNoticia,Titulo,TextoNoticia,FechaPublicacion,IdUsuarioAdmin")] Noticia noticia)
        {
            if (id != noticia.IdNoticia)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(noticia);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!NoticiaExists(noticia.IdNoticia))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(noticia);
        }

        // GET: AdminNoticias/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var noticia = await _context.Noticias
                .Include(n => n.UsuarioAdmin)
                .FirstOrDefaultAsync(m => m.IdNoticia == id);
            if (noticia == null)
            {
                return NotFound();
            }

            return View(noticia);
        }

        // POST: AdminNoticias/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var noticia = await _context.Noticias.FindAsync(id);
            if (noticia != null)
            {
                _context.Noticias.Remove(noticia);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool NoticiaExists(int id)
        {
            return _context.Noticias.Any(e => e.IdNoticia == id);
        }
    }
}
