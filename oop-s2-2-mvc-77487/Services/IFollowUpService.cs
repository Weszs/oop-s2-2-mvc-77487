using oop_s2_2_mvc_77487.Models;

namespace oop_s2_2_mvc_77487.Services;

public interface IFollowUpService
{
    Task<IEnumerable<FollowUp>> GetAllFollowUpsAsync();
    Task<FollowUp?> GetFollowUpByIdAsync(int id);
    Task<IEnumerable<FollowUp>> GetFollowUpsByInspectionIdAsync(int inspectionId);
    Task<int> GetOverdueFollowUpsCountAsync();
    Task<int> CreateFollowUpAsync(FollowUp followUp);
    Task UpdateFollowUpAsync(FollowUp followUp);
    Task DeleteFollowUpAsync(int id);
    Task CloseFollowUpAsync(int id);
}
