using oop_s2_2_mvc_77487.Data;

namespace oop_s2_2_mvc_77487.Services;

public interface IAuditTrailService
{
    Task LogActionAsync(string userId, string action, string entityType, int? entityId, string details);
    Task<IEnumerable<AuditLogEntry>> GetAuditLogAsync(int limit = 100);
    Task<IEnumerable<AuditLogEntry>> GetUserAuditLogAsync(string userId, int limit = 50);
}

public class AuditLogEntry
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public int? EntityId { get; set; }
    public string Details { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class AuditTrailService : IAuditTrailService
{
    private readonly ILogger<AuditTrailService> _logger;
    private readonly List<AuditLogEntry> _auditLogs = new();

    public AuditTrailService(ILogger<AuditTrailService> logger)
    {
        _logger = logger;
    }

    public async Task LogActionAsync(string userId, string action, string entityType, int? entityId, string details)
    {
        var entry = new AuditLogEntry
        {
            Id = _auditLogs.Count + 1,
            UserId = userId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Details = details,
            Timestamp = DateTime.UtcNow
        };

        _auditLogs.Add(entry);

        _logger.LogInformation(
            "AUDIT: User {UserId} performed {Action} on {EntityType} (ID: {EntityId}). Details: {Details}",
            userId, action, entityType, entityId ?? 0, details);

        await Task.CompletedTask;
    }

    public async Task<IEnumerable<AuditLogEntry>> GetAuditLogAsync(int limit = 100)
    {
        _logger.LogInformation("Retrieving last {Limit} audit log entries", limit);
        return await Task.FromResult(_auditLogs.OrderByDescending(x => x.Timestamp).Take(limit));
    }

    public async Task<IEnumerable<AuditLogEntry>> GetUserAuditLogAsync(string userId, int limit = 50)
    {
        _logger.LogInformation("Retrieving audit log for user {UserId} (last {Limit} entries)", userId, limit);
        return await Task.FromResult(
            _auditLogs
                .Where(x => x.UserId == userId)
                .OrderByDescending(x => x.Timestamp)
                .Take(limit));
    }
}
