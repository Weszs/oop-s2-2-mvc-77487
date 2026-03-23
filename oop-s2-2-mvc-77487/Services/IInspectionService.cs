using oop_s2_2_mvc_77487.Models;

namespace oop_s2_2_mvc_77487.Services;

public interface IInspectionService
{
    Task<IEnumerable<Inspection>> GetAllInspectionsAsync();
    Task<Inspection?> GetInspectionByIdAsync(int id);
    Task<IEnumerable<Inspection>> GetInspectionsByPremisesIdAsync(int premisesId);
    Task<int> CreateInspectionAsync(Inspection inspection);
    Task UpdateInspectionAsync(Inspection inspection);
    Task DeleteInspectionAsync(int id);
    Task<int> GetInspectionsThisMonthAsync();
    Task<int> GetFailedInspectionsThisMonthAsync();
}
