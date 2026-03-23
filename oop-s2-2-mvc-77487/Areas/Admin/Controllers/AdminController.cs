using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using oop_s2_2_mvc_77487.Data;
using oop_s2_2_mvc_77487.Models;
using oop_s2_2_mvc_77487.Services;

namespace oop_s2_2_mvc_77487.Areas.Admin.Controllers;

[Area("Admin")]
[Route("Admin/[controller]/[action]")]
[Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditTrailService _auditTrailService;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        ApplicationDbContext context,
        IAuditTrailService auditTrailService,
        UserManager<IdentityUser> userManager,
        ILogger<AdminController> logger)
    {
        _context = context;
        _auditTrailService = auditTrailService;
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        _logger.LogInformation("Admin panel accessed by {UserName}", User.Identity?.Name);

        var now = DateTime.Now;
        var monthStart = new DateTime(now.Year, now.Month, 1);

        var viewModel = new AdminDashboardViewModel
        {
            TotalPremises = await _context.Premises.CountAsync(),
            TotalInspections = await _context.Inspections.CountAsync(),
            TotalFollowUps = await _context.FollowUps.CountAsync(),
            OpenFollowUps = await _context.FollowUps.CountAsync(f => f.Status == FollowUpStatus.Open),
            OverdueFollowUps = await _context.FollowUps.CountAsync(f => f.Status == FollowUpStatus.Open && f.DueDate < now),
            InspectionsThisMonth = await _context.Inspections.CountAsync(i => i.InspectionDate >= monthStart),
            FailedThisMonth = await _context.Inspections.CountAsync(i => i.InspectionDate >= monthStart && i.Outcome == InspectionOutcome.Fail),
            TotalUsers = _userManager.Users.Count(),
            RecentAuditEntries = await _auditTrailService.GetAuditLogAsync(20)
        };

        return View(viewModel);
    }

    public async Task<IActionResult> AuditLog()
    {
        _logger.LogInformation("Full audit log accessed by {UserName}", User.Identity?.Name);

        await _auditTrailService.LogActionAsync(
            _userManager.GetUserId(User) ?? "unknown",
            "ViewAuditLog", "Admin", null,
            $"Admin {User.Identity?.Name} viewed the full audit log");

        var entries = await _auditTrailService.GetAuditLogAsync(500);
        return View(entries);
    }
}

public class AdminDashboardViewModel
{
    public int TotalPremises { get; set; }
    public int TotalInspections { get; set; }
    public int TotalFollowUps { get; set; }
    public int OpenFollowUps { get; set; }
    public int OverdueFollowUps { get; set; }
    public int InspectionsThisMonth { get; set; }
    public int FailedThisMonth { get; set; }
    public int TotalUsers { get; set; }
    public IEnumerable<AuditLogEntry> RecentAuditEntries { get; set; } = [];
}
