using oop_s2_2_mvc_77487.Data;
using oop_s2_2_mvc_77487.Models;
using Microsoft.EntityFrameworkCore;

namespace oop_s2_2_mvc_77487.Services;

public class FollowUpService : IFollowUpService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<FollowUpService> _logger;

    public FollowUpService(ApplicationDbContext context, ILogger<FollowUpService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<FollowUp>> GetAllFollowUpsAsync()
    {
        _logger.LogInformation("Fetching all follow-ups");
        return await _context.FollowUps.Include(f => f.Inspection).ToListAsync();
    }

    public async Task<FollowUp?> GetFollowUpByIdAsync(int id)
    {
        _logger.LogInformation("Fetching follow-up with ID: {FollowUpId}", id);
        return await _context.FollowUps.Include(f => f.Inspection).FirstOrDefaultAsync(f => f.Id == id);
    }

    public async Task<IEnumerable<FollowUp>> GetFollowUpsByInspectionIdAsync(int inspectionId)
    {
        _logger.LogInformation("Fetching follow-ups for inspection: {InspectionId}", inspectionId);
        return await _context.FollowUps
            .Where(f => f.InspectionId == inspectionId)
            .OrderByDescending(f => f.DueDate)
            .ToListAsync();
    }

    public async Task<int> GetOverdueFollowUpsCountAsync()
    {
        var count = await _context.FollowUps
            .Where(f => f.DueDate < DateTime.Now && f.Status == FollowUpStatus.Open)
            .CountAsync();
        _logger.LogInformation("Overdue follow-ups count: {Count}", count);
        return count;
    }

    public async Task<int> CreateFollowUpAsync(FollowUp followUp)
    {
        _logger.LogInformation("Creating follow-up for inspection: {InspectionId}, DueDate: {DueDate}", followUp.InspectionId, followUp.DueDate);
        _context.FollowUps.Add(followUp);
        await _context.SaveChangesAsync();
        return followUp.Id;
    }

    public async Task UpdateFollowUpAsync(FollowUp followUp)
    {
        _logger.LogInformation("Updating follow-up: {FollowUpId}", followUp.Id);
        _context.FollowUps.Update(followUp);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteFollowUpAsync(int id)
    {
        _logger.LogInformation("Deleting follow-up: {FollowUpId}", id);
        var followUp = await _context.FollowUps.FindAsync(id);
        if (followUp != null)
        {
            _context.FollowUps.Remove(followUp);
            await _context.SaveChangesAsync();
        }
    }

    public async Task CloseFollowUpAsync(int id)
    {
        _logger.LogInformation("Closing follow-up: {FollowUpId}", id);
        var followUp = await _context.FollowUps.FindAsync(id);
        if (followUp != null)
        {
            followUp.Status = FollowUpStatus.Closed;
            followUp.ClosedDate = DateTime.Now;
            _context.FollowUps.Update(followUp);
            await _context.SaveChangesAsync();
        }
    }
}
