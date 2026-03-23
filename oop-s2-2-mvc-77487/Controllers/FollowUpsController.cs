using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using oop_s2_2_mvc_77487.Data;
using oop_s2_2_mvc_77487.Models;

namespace oop_s2_2_mvc_77487.Controllers
{
    [Authorize]
    public class FollowUpsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<FollowUpsController> _logger;

        public FollowUpsController(ApplicationDbContext context, ILogger<FollowUpsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            _logger.LogInformation("Follow-ups list accessed by user {UserName}", User.Identity?.Name ?? "Unknown");
            var followUps = await _context.FollowUps
                .Include(f => f.Inspection)
                .ThenInclude(i => i!.Premises)
                .OrderBy(f => f.Status)
                .ThenByDescending(f => f.DueDate)
                .ToListAsync();

            return View(followUps);
        }

        public async Task<IActionResult> Details(int id)
        {
            var followUp = await _context.FollowUps
                .Include(f => f.Inspection)
                .ThenInclude(i => i!.Premises)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (followUp == null)
            {
                _logger.LogWarning("Follow-up with ID {FollowUpId} not found", id);
                return NotFound();
            }

            _logger.LogInformation("Follow-up {FollowUpId} details accessed", id);
            return View(followUp);
        }

        [Authorize(Roles = "Admin,Inspector")]
        public IActionResult Create(int? inspectionId)
        {
            var inspections = _context.Inspections
                .Include(i => i.Premises)
                .OrderByDescending(i => i.InspectionDate)
                .Select(i => new SelectListItem
                {
                    Value = i.Id.ToString(),
                    Text = $"{i.Premises!.Name} - {i.InspectionDate:yyyy-MM-dd}"
                });

            ViewData["InspectionId"] = new SelectList(inspections, "Value", "Text", inspectionId);
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Inspector")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("InspectionId,DueDate")] FollowUp followUp)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var inspection = await _context.Inspections.FindAsync(followUp.InspectionId);
                    if (inspection == null)
                    {
                        _logger.LogWarning("Inspection {InspectionId} not found when creating follow-up", followUp.InspectionId);
                        ModelState.AddModelError(nameof(followUp.InspectionId), "Selected inspection not found");
                        return View(followUp);
                    }

                    if (followUp.DueDate < inspection.InspectionDate)
                    {
                        _logger.LogWarning("Follow-up DueDate {DueDate} is before InspectionDate {InspectionDate}",
                            followUp.DueDate, inspection.InspectionDate);
                        ModelState.AddModelError(nameof(followUp.DueDate), "Due date must be after the inspection date");
                        return View(followUp);
                    }

                    followUp.Status = FollowUpStatus.Open;
                    _context.Add(followUp);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Follow-up created: FollowUpId={FollowUpId}, InspectionId={InspectionId}, DueDate={DueDate}",
                        followUp.Id, followUp.InspectionId, followUp.DueDate);

                    return RedirectToAction(nameof(Details), new { id = followUp.Id });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating follow-up");
                    ModelState.AddModelError("", "Unable to save changes. Try again, and if the problem persists, see your system administrator.");
                }
            }

            var inspectionsForDropdown = _context.Inspections
                .Include(i => i.Premises)
                .OrderByDescending(i => i.InspectionDate)
                .Select(i => new SelectListItem
                {
                    Value = i.Id.ToString(),
                    Text = $"{i.Premises!.Name} - {i.InspectionDate:yyyy-MM-dd}"
                });

            ViewData["InspectionId"] = new SelectList(inspectionsForDropdown, "Value", "Text", followUp.InspectionId);
            return View(followUp);
        }

        [Authorize(Roles = "Admin,Inspector")]
        public async Task<IActionResult> Close(int id)
        {
            var followUp = await _context.FollowUps
                .Include(f => f.Inspection)
                .ThenInclude(i => i!.Premises)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (followUp == null)
            {
                return NotFound();
            }

            if (followUp.Status == FollowUpStatus.Closed)
            {
                _logger.LogWarning("Attempt to close already closed follow-up {FollowUpId}", id);
                return View("ClosedWarning", followUp);
            }

            return View(followUp);
        }

        [HttpPost, ActionName("Close")]
        [Authorize(Roles = "Admin,Inspector")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CloseConfirmed(int id)
        {
            var followUp = await _context.FollowUps.FindAsync(id);
            if (followUp == null)
            {
                return NotFound();
            }

            try
            {
                if (followUp.Status == FollowUpStatus.Closed)
                {
                    _logger.LogWarning("Attempt to close already closed follow-up {FollowUpId}", id);
                    return BadRequest("This follow-up is already closed");
                }

                followUp.Status = FollowUpStatus.Closed;
                followUp.ClosedDate = DateTime.Now;
                _context.Update(followUp);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Follow-up closed: FollowUpId={FollowUpId}, ClosedDate={ClosedDate}",
                    id, followUp.ClosedDate);

                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error closing follow-up {FollowUpId}", id);
                ModelState.AddModelError("", "Unable to close follow-up. Try again, and if the problem persists, see your system administrator.");
                return View(followUp);
            }
        }

        private Task<bool> FollowUpExists(int id)
        {
            return _context.FollowUps.AnyAsync(e => e.Id == id);
        }
    }
}
