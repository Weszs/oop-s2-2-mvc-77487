using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using oop_s2_2_mvc_77487.Data;
using oop_s2_2_mvc_77487.Models;

namespace oop_s2_2_mvc_77487.Controllers
{
    [Authorize]
    public class InspectionsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<InspectionsController> _logger;

        public InspectionsController(ApplicationDbContext context, ILogger<InspectionsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            _logger.LogInformation("Inspections list accessed by user {UserName}", User.Identity?.Name ?? "Unknown");
            var inspections = await _context.Inspections
                .Include(i => i.Premises)
                .OrderByDescending(i => i.InspectionDate)
                .ToListAsync();

            return View(inspections);
        }

        public async Task<IActionResult> Details(int id)
        {
            var inspection = await _context.Inspections
                .Include(i => i.Premises)
                .Include(i => i.FollowUps)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (inspection == null)
            {
                _logger.LogWarning("Inspection with ID {InspectionId} not found", id);
                return NotFound();
            }

            _logger.LogInformation("Inspection {InspectionId} details accessed", id);
            return View(inspection);
        }

        [Authorize(Roles = "Admin,Inspector")]
        public IActionResult Create()
        {
            ViewData["PremisesId"] = new SelectList(_context.Premises.OrderBy(p => p.Name), "Id", "Name");
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Inspector")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("PremisesId,InspectionDate,Score,Outcome,Notes")] Inspection inspection)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (inspection.Score < 0 || inspection.Score > 100)
                    {
                        _logger.LogWarning("Invalid score {Score} provided for inspection", inspection.Score);
                        ModelState.AddModelError(nameof(inspection.Score), "Score must be between 0 and 100");
                        ViewData["PremisesId"] = new SelectList(_context.Premises.OrderBy(p => p.Name), "Id", "Name", inspection.PremisesId);
                        return View(inspection);
                    }

                    _context.Add(inspection);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Inspection created: InspectionId={InspectionId}, PremisesId={PremisesId}, Score={Score}, Outcome={Outcome}",
                        inspection.Id, inspection.PremisesId, inspection.Score, inspection.Outcome);

                    return RedirectToAction(nameof(Details), new { id = inspection.Id });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating inspection");
                    ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists, see your system administrator.");
                }
            }

            ViewData["PremisesId"] = new SelectList(_context.Premises.OrderBy(p => p.Name), "Id", "Name", inspection.PremisesId);
            return View(inspection);
        }

        [Authorize(Roles = "Admin,Inspector")]
        public async Task<IActionResult> Edit(int id)
        {
            var inspection = await _context.Inspections.FindAsync(id);
            if (inspection == null)
            {
                return NotFound();
            }

            ViewData["PremisesId"] = new SelectList(_context.Premises.OrderBy(p => p.Name), "Id", "Name", inspection.PremisesId);
            return View(inspection);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Inspector")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,PremisesId,InspectionDate,Score,Outcome,Notes")] Inspection inspection)
        {
            if (id != inspection.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (inspection.Score < 0 || inspection.Score > 100)
                    {
                        _logger.LogWarning("Invalid score {Score} provided for inspection {InspectionId}", inspection.Score, id);
                        ModelState.AddModelError(nameof(inspection.Score), "Score must be between 0 and 100");
                        ViewData["PremisesId"] = new SelectList(_context.Premises.OrderBy(p => p.Name), "Id", "Name", inspection.PremisesId);
                        return View(inspection);
                    }

                    _context.Update(inspection);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Inspection updated: InspectionId={InspectionId}, PremisesId={PremisesId}, Score={Score}",
                        inspection.Id, inspection.PremisesId, inspection.Score);

                    return RedirectToAction(nameof(Details), new { id = inspection.Id });
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    _logger.LogError(ex, "Concurrency error updating inspection {InspectionId}", id);
                    if (!await InspectionExists(id))
                    {
                        return NotFound();
                    }

                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating inspection {InspectionId}", id);
                    ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists, see your system administrator.");
                }
            }

            ViewData["PremisesId"] = new SelectList(_context.Premises.OrderBy(p => p.Name), "Id", "Name", inspection.PremisesId);
            return View(inspection);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var inspection = await _context.Inspections
                .Include(i => i.Premises)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (inspection == null)
            {
                return NotFound();
            }

            return View(inspection);
        }

        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var inspection = await _context.Inspections.FindAsync(id);
            if (inspection == null)
            {
                return NotFound();
            }

            try
            {
                _context.Inspections.Remove(inspection);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Inspection deleted: InspectionId={InspectionId}, PremisesId={PremisesId}",
                    id, inspection.PremisesId);

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting inspection {InspectionId}", id);
                ModelState.AddModelError("", "Unable to delete. Try again, and if the problem persists, see your system administrator.");
                return View(inspection);
            }
        }

        private Task<bool> InspectionExists(int id)
        {
            return _context.Inspections.AnyAsync(e => e.Id == id);
        }
    }
}
