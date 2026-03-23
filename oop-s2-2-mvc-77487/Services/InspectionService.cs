using oop_s2_2_mvc_77487.Data;
using oop_s2_2_mvc_77487.Models;
using Microsoft.EntityFrameworkCore;

namespace oop_s2_2_mvc_77487.Services;

public class InspectionService : IInspectionService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<InspectionService> _logger;

    public InspectionService(ApplicationDbContext context, ILogger<InspectionService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<Inspection>> GetAllInspectionsAsync()
    {
        _logger.LogInformation("Fetching all inspections");
        return await _context.Inspections.Include(i => i.Premises).ToListAsync();
    }

    public async Task<Inspection?> GetInspectionByIdAsync(int id)
    {
        _logger.LogInformation("Fetching inspection with ID: {InspectionId}", id);
        return await _context.Inspections.Include(i => i.Premises).FirstOrDefaultAsync(i => i.Id == id);
    }

    public async Task<IEnumerable<Inspection>> GetInspectionsByPremisesIdAsync(int premisesId)
    {
        _logger.LogInformation("Fetching inspections for premises: {PremisesId}", premisesId);
        return await _context.Inspections
            .Where(i => i.PremisesId == premisesId)
            .OrderByDescending(i => i.InspectionDate)
            .ToListAsync();
    }

    public async Task<int> CreateInspectionAsync(Inspection inspection)
    {
        _logger.LogInformation("Creating inspection for premises: {PremisesId}, Score: {Score}", inspection.PremisesId, inspection.Score);
        _context.Inspections.Add(inspection);
        await _context.SaveChangesAsync();
        return inspection.Id;
    }

    public async Task UpdateInspectionAsync(Inspection inspection)
    {
        _logger.LogInformation("Updating inspection: {InspectionId}", inspection.Id);
        _context.Inspections.Update(inspection);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteInspectionAsync(int id)
    {
        _logger.LogInformation("Deleting inspection: {InspectionId}", id);
        var inspection = await _context.Inspections.FindAsync(id);
        if (inspection != null)
        {
            _context.Inspections.Remove(inspection);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<int> GetInspectionsThisMonthAsync()
    {
        var thisMonth = DateTime.Now;
        var count = await _context.Inspections
            .Where(i => i.InspectionDate.Year == thisMonth.Year && i.InspectionDate.Month == thisMonth.Month)
            .CountAsync();
        _logger.LogInformation("Inspections this month: {Count}", count);
        return count;
    }

    public async Task<int> GetFailedInspectionsThisMonthAsync()
    {
        var thisMonth = DateTime.Now;
        var count = await _context.Inspections
            .Where(i => i.InspectionDate.Year == thisMonth.Year && 
                        i.InspectionDate.Month == thisMonth.Month &&
                        i.Outcome == InspectionOutcome.Fail)
            .CountAsync();
        _logger.LogInformation("Failed inspections this month: {Count}", count);
        return count;
    }
}
