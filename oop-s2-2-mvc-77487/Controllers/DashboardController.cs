using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using oop_s2_2_mvc_77487.Data;
using oop_s2_2_mvc_77487.Models;
using oop_s2_2_mvc_77487.Services;

namespace oop_s2_2_mvc_77487.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditTrailService _auditTrailService;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(
            ApplicationDbContext context,
            IAuditTrailService auditTrailService,
            ILogger<DashboardController> logger)
        {
            _context = context;
            _auditTrailService = auditTrailService;
            _logger = logger;
        }

        public async Task<IActionResult> Index(string? town = null, string? riskRating = null)
        {
            try
            {
                var now = DateTime.Now;
                var monthStart = new DateTime(now.Year, now.Month, 1);
                var userName = User.Identity?.Name ?? "Unknown";

                _logger.LogInformation("Dashboard accessed by user {UserName} with filters: Town={Town}, RiskRating={RiskRating}",
                    userName, town ?? "None", riskRating ?? "None");

                // Get all premises with filters
                var premisesQuery = _context.Premises.AsQueryable();

                if (!string.IsNullOrEmpty(town))
                {
                    premisesQuery = premisesQuery.Where(p => p.Town == town);
                }

                if (!string.IsNullOrEmpty(riskRating) && Enum.TryParse<RiskRating>(riskRating, out var rating))
                {
                    premisesQuery = premisesQuery.Where(p => p.RiskRating == rating);
                }

                var filteredPremises = await premisesQuery.ToListAsync();
                var premisesIds = filteredPremises.Select(p => p.Id).ToList();

                // Stats
                var totalPremises = filteredPremises.Count;

                var inspectionsThisMonth = await _context.Inspections
                    .Where(i => premisesIds.Contains(i.PremisesId) && i.InspectionDate >= monthStart)
                    .CountAsync();

                var failedInspectionsThisMonth = await _context.Inspections
                    .Where(i => premisesIds.Contains(i.PremisesId) &&
                               i.InspectionDate >= monthStart &&
                               i.Outcome == InspectionOutcome.Fail)
                    .CountAsync();

                var overdueFollowUps = await _context.FollowUps
                    .Where(f => f.Status == FollowUpStatus.Open &&
                               f.DueDate < now &&
                               premisesIds.Contains(f.Inspection!.PremisesId))
                    .CountAsync();

                var openFollowUps = await _context.FollowUps
                    .Where(f => f.Status == FollowUpStatus.Open &&
                               premisesIds.Contains(f.Inspection!.PremisesId))
                    .CountAsync();

                var highRiskCount = filteredPremises.Count(p => p.RiskRating == RiskRating.High);

                // Recent inspections (last 10)
                var recentInspections = await _context.Inspections
                    .Where(i => premisesIds.Contains(i.PremisesId))
                    .Include(i => i.Premises)
                    .OrderByDescending(i => i.InspectionDate)
                    .Take(10)
                    .ToListAsync();

                // Overdue follow-up list
                var overdueFollowUpList = await _context.FollowUps
                    .Where(f => f.Status == FollowUpStatus.Open &&
                               f.DueDate < now &&
                               premisesIds.Contains(f.Inspection!.PremisesId))
                    .Include(f => f.Inspection)
                    .ThenInclude(i => i!.Premises)
                    .OrderBy(f => f.DueDate)
                    .Take(10)
                    .ToListAsync();

                // High-risk premises
                var highRiskPremises = filteredPremises
                    .Where(p => p.RiskRating == RiskRating.High)
                    .OrderBy(p => p.Name)
                    .ToList();

                // Recent audit log (Admin only)
                IEnumerable<AuditLogEntry>? recentAuditEntries = null;
                if (User.IsInRole("Admin"))
                {
                    recentAuditEntries = await _auditTrailService.GetAuditLogAsync(20);
                }

                _logger.LogInformation("Dashboard stats - Premises: {Total}, Inspections this month: {InspCount}, Failed: {FailCount}, Overdue: {OverdueCount}",
                    totalPremises, inspectionsThisMonth, failedInspectionsThisMonth, overdueFollowUps);

                var viewModel = new DashboardViewModel
                {
                    TotalPremises = totalPremises,
                    InspectionsThisMonth = inspectionsThisMonth,
                    FailedInspectionsThisMonth = failedInspectionsThisMonth,
                    OverdueFollowUps = overdueFollowUps,
                    OpenFollowUps = openFollowUps,
                    HighRiskPremisesCount = highRiskCount,
                    TownFilter = town,
                    RiskRatingFilter = riskRating,
                    Towns = await _context.Premises.Select(p => p.Town).Distinct().OrderBy(t => t).ToListAsync(),
                    RiskRatings = Enum.GetNames(typeof(RiskRating)).ToList(),
                    RecentInspections = recentInspections,
                    OverdueFollowUpList = overdueFollowUpList,
                    HighRiskPremises = highRiskPremises,
                    RecentAuditEntries = recentAuditEntries
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accessing dashboard");
                throw;
            }
        }
    }

    public class DashboardViewModel
    {
        public int TotalPremises { get; set; }
        public int InspectionsThisMonth { get; set; }
        public int FailedInspectionsThisMonth { get; set; }
        public int OverdueFollowUps { get; set; }
        public int OpenFollowUps { get; set; }
        public int HighRiskPremisesCount { get; set; }
        public string? TownFilter { get; set; }
        public string? RiskRatingFilter { get; set; }
        public List<string> Towns { get; set; } = new();
        public List<string> RiskRatings { get; set; } = new();
        public List<Inspection> RecentInspections { get; set; } = new();
        public List<FollowUp> OverdueFollowUpList { get; set; } = new();
        public List<Premises> HighRiskPremises { get; set; } = new();
        public IEnumerable<AuditLogEntry>? RecentAuditEntries { get; set; }
    }
}
