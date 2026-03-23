using oop_s2_2_mvc_77487.Models;

namespace oop_s2_2_mvc_77487.Services;

public interface IPremisesService
{
    Task<IEnumerable<Premises>> GetAllPremisesAsync();
    Task<Premises?> GetPremisesByIdAsync(int id);
    Task<IEnumerable<Premises>> GetPremisesByTownAsync(string town);
    Task<int> CreatePremisesAsync(Premises premises);
    Task UpdatePremisesAsync(Premises premises);
    Task DeletePremisesAsync(int id);
    Task<IEnumerable<string>> GetAllTownsAsync();
}
