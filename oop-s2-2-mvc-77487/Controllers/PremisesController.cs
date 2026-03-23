using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using oop_s2_2_mvc_77487.Data;
using oop_s2_2_mvc_77487.Models;

namespace oop_s2_2_mvc_77487.Controllers
{
    [Authorize]
    public class PremisesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PremisesController> _logger;

        public PremisesController(ApplicationDbContext context, ILogger<PremisesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            _logger.LogInformation("Premises list accessed by user {UserName}", User.Identity?.Name ?? "Unknown");
            var premises = await _context.Premises.OrderBy(p => p.Name).ToListAsync();
            return View(premises);
        }

        public async Task<IActionResult> Details(int id)
        {
            var premises = await _context.Premises
                .Include(p => p.Inspections)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (premises == null)
            {
                _logger.LogWarning("Premises with ID {PremisesId} not found", id);
                return NotFound();
            }

            _logger.LogInformation("Premises {PremisesId} details accessed", id);
            return View(premises);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(Premises premises)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Premises.Add(premises);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Premises created: {PremisesId} - {PremisesName}", premises.Id, premises.Name);
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating premises");
                    ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists, see your system administrator.");
                }
            }

            return View(premises);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var premises = await _context.Premises.FindAsync(id);
            if (premises == null)
            {
                return NotFound();
            }

            return View(premises);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id, Premises premises)
        {
            if (id != premises.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(premises);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Premises updated: {PremisesId} - {PremisesName}", premises.Id, premises.Name);
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    _logger.LogError(ex, "Concurrency error updating premises {PremisesId}", id);
                    if (!await PremisesExists(id))
                    {
                        return NotFound();
                    }

                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating premises {PremisesId}", id);
                    ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists, see your system administrator.");
                }
            }

            return View(premises);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var premises = await _context.Premises.FindAsync(id);
            if (premises == null)
            {
                return NotFound();
            }

            return View(premises);
        }

        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var premises = await _context.Premises.FindAsync(id);
            if (premises == null)
            {
                return NotFound();
            }

            try
            {
                _context.Premises.Remove(premises);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Premises deleted: {PremisesId} - {PremisesName}", id, premises.Name);
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting premises {PremisesId}", id);
                ModelState.AddModelError("", "Unable to delete. Try again, and if the problem persists, see your system administrator.");
                return View(premises);
            }
        }

        private Task<bool> PremisesExists(int id)
        {
            return _context.Premises.AnyAsync(e => e.Id == id);
        }
    }
}
