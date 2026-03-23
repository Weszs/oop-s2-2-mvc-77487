using oop_s2_2_mvc_77487.Data;
using oop_s2_2_mvc_77487.Models;
using Microsoft.EntityFrameworkCore;

namespace oop_s2_2_mvc_77487.Services;

public class PremisesService : IPremisesService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PremisesService> _logger;

    public PremisesService(ApplicationDbContext context, ILogger<PremisesService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<Premises>> GetAllPremisesAsync()
    {
        _logger.LogInformation("Fetching all premises");
        return await _context.Premises.Include(p => p.Inspections).ToListAsync();
    }

    public async Task<Premises?> GetPremisesByIdAsync(int id)
    {
        _logger.LogInformation("Fetching premises with ID: {PremisesId}", id);
        return await _context.Premises
            .Include(p => p.Inspections)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<IEnumerable<Premises>> GetPremisesByTownAsync(string town)
    {
        _logger.LogInformation("Fetching premises in town: {Town}", town);
        return await _context.Premises
            .Where(p => p.Town == town)
            .Include(p => p.Inspections)
            .ToListAsync();
    }

    public async Task<int> CreatePremisesAsync(Premises premises)
    {
        _logger.LogInformation("Creating premises: {Name}, Town: {Town}", premises.Name, premises.Town);
        _context.Premises.Add(premises);
        await _context.SaveChangesAsync();
        return premises.Id;
    }

    public async Task UpdatePremisesAsync(Premises premises)
    {
        _logger.LogInformation("Updating premises: {PremisesId}", premises.Id);
        _context.Premises.Update(premises);
        await _context.SaveChangesAsync();
    }

    public async Task DeletePremisesAsync(int id)
    {
        _logger.LogInformation("Deleting premises: {PremisesId}", id);
        var premises = await _context.Premises.FindAsync(id);
        if (premises != null)
        {
            _context.Premises.Remove(premises);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<string>> GetAllTownsAsync()
    {
        _logger.LogInformation("Fetching all towns");
        return await _context.Premises
            .Select(p => p.Town)
            .Distinct()
            .OrderBy(t => t)
            .ToListAsync();
    }
}
