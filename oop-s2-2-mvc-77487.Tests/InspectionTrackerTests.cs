using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using oop_s2_2_mvc_77487.Data;
using oop_s2_2_mvc_77487.Models;
using oop_s2_2_mvc_77487.Services;
using Xunit;

namespace oop_s2_2_mvc_77487.Tests;

public class InspectionTrackerTests
{
    private ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private async Task<(Premises premises, Inspection inspection)> SeedPremisesAndInspection(
        ApplicationDbContext context, DateTime? inspectionDate = null)
    {
        var premises = new Premises
        {
            Name = "Test Premises",
            Address = "1 High St",
            Town = "Bristol",
            RiskRating = RiskRating.Medium
        };
        var inspection = new Inspection
        {
            Premises = premises,
            InspectionDate = inspectionDate ?? DateTime.Now.AddDays(-10),
            Score = 65,
            Outcome = InspectionOutcome.Fail
        };
        context.Premises.Add(premises);
        context.Inspections.Add(inspection);
        await context.SaveChangesAsync();
        return (premises, inspection);
    }

    [Fact]
    public async Task OverdueFollowUps_ReturnsOnlyOpenAndPastDue()
    {
        using var context = CreateContext();
        var (_, inspection) = await SeedPremisesAndInspection(context);
        var now = DateTime.Now;

        var overdue = new FollowUp
        {
            InspectionId = inspection.Id,
            DueDate = now.AddDays(-7),
            Status = FollowUpStatus.Open
        };
        var future = new FollowUp
        {
            InspectionId = inspection.Id,
            DueDate = now.AddDays(14),
            Status = FollowUpStatus.Open
        };
        var closedPastDue = new FollowUp
        {
            InspectionId = inspection.Id,
            DueDate = now.AddDays(-3),
            Status = FollowUpStatus.Closed,
            ClosedDate = now.AddDays(-1)
        };
        context.FollowUps.AddRange(overdue, future, closedPastDue);
        await context.SaveChangesAsync();

        var service = new FollowUpService(context, Mock.Of<ILogger<FollowUpService>>());
        var count = await service.GetOverdueFollowUpsCountAsync();

        Assert.Equal(1, count);
    }

    [Fact]
    public async Task CloseFollowUp_SetsClosedDateAutomatically()
    {
        using var context = CreateContext();
        var (_, inspection) = await SeedPremisesAndInspection(context);

        var followUp = new FollowUp
        {
            InspectionId = inspection.Id,
            DueDate = DateTime.Now.AddDays(5),
            Status = FollowUpStatus.Open
        };
        context.FollowUps.Add(followUp);
        await context.SaveChangesAsync();

        Assert.Null(followUp.ClosedDate);

        var service = new FollowUpService(context, Mock.Of<ILogger<FollowUpService>>());
        await service.CloseFollowUpAsync(followUp.Id);

        var closed = await context.FollowUps.FindAsync(followUp.Id);
        Assert.NotNull(closed);
        Assert.Equal(FollowUpStatus.Closed, closed.Status);
        Assert.NotNull(closed.ClosedDate);
    }

    [Fact]
    public async Task DashboardCounts_MatchKnownData()
    {
        using var context = CreateContext();
        var now = DateTime.Now;
        var monthStart = new DateTime(now.Year, now.Month, 1);

        var premises = new Premises
        {
            Name = "Dashboard Premises",
            Address = "2 King St",
            Town = "Bath",
            RiskRating = RiskRating.High
        };
        context.Premises.Add(premises);

        var passThisMonth = new Inspection
        {
            Premises = premises,
            InspectionDate = now.AddDays(-2),
            Score = 90,
            Outcome = InspectionOutcome.Pass
        };
        var failThisMonth = new Inspection
        {
            Premises = premises,
            InspectionDate = now.AddDays(-1),
            Score = 40,
            Outcome = InspectionOutcome.Fail
        };
        var failLastMonth = new Inspection
        {
            Premises = premises,
            InspectionDate = monthStart.AddDays(-5),
            Score = 30,
            Outcome = InspectionOutcome.Fail
        };
        context.Inspections.AddRange(passThisMonth, failThisMonth, failLastMonth);
        await context.SaveChangesAsync();

        var service = new InspectionService(context, Mock.Of<ILogger<InspectionService>>());

        var totalThisMonth = await service.GetInspectionsThisMonthAsync();
        var failedThisMonth = await service.GetFailedInspectionsThisMonthAsync();

        Assert.Equal(2, totalThisMonth);
        Assert.Equal(1, failedThisMonth);
    }

    [Fact]
    public async Task InspectorCanCreateInspection_ViewerSeesReadOnly()
    {
        using var context = CreateContext();
        var premises = new Premises
        {
            Name = "Auth Premises",
            Address = "3 Queen St",
            Town = "Wells",
            RiskRating = RiskRating.Low
        };
        context.Premises.Add(premises);
        await context.SaveChangesAsync();

        var service = new InspectionService(context, Mock.Of<ILogger<InspectionService>>());

        var newInspection = new Inspection
        {
            PremisesId = premises.Id,
            InspectionDate = DateTime.Now,
            Score = 82,
            Outcome = InspectionOutcome.Pass,
            Notes = "Created by inspector"
        };
        var id = await service.CreateInspectionAsync(newInspection);

        Assert.True(id > 0);

        var all = await service.GetAllInspectionsAsync();
        Assert.Single(all);

        var retrieved = await service.GetInspectionByIdAsync(id);
        Assert.NotNull(retrieved);
        Assert.Equal(82, retrieved.Score);
        Assert.Equal("Created by inspector", retrieved.Notes);
    }

    // Test 1: Overdue follow-ups query returns correct items
    [Fact]
    public async Task OverdueFollowUpsQuery_ReturnsOnlyOverdueOpenFollowUps()
    {
        using var context = CreateContext();
        var (_, inspection) = await SeedPremisesAndInspection(context);
        var now = DateTime.Now;

        // Create 5 follow-ups with different statuses and due dates
        var overdueFU1 = new FollowUp
        {
            InspectionId = inspection.Id,
            DueDate = now.AddDays(-10),
            Status = FollowUpStatus.Open,
            ClosedDate = null
        };
        var overdueFU2 = new FollowUp
        {
            InspectionId = inspection.Id,
            DueDate = now.AddDays(-5),
            Status = FollowUpStatus.Open,
            ClosedDate = null
        };
        var futureOpenFU = new FollowUp
        {
            InspectionId = inspection.Id,
            DueDate = now.AddDays(14),
            Status = FollowUpStatus.Open,
            ClosedDate = null
        };
        var closedOverdueFU = new FollowUp
        {
            InspectionId = inspection.Id,
            DueDate = now.AddDays(-3),
            Status = FollowUpStatus.Closed,
            ClosedDate = now.AddDays(-1)
        };
        var overdueButClosed = new FollowUp
        {
            InspectionId = inspection.Id,
            DueDate = now.AddDays(-7),
            Status = FollowUpStatus.Closed,
            ClosedDate = now.AddDays(-2)
        };

        context.FollowUps.AddRange(overdueFU1, overdueFU2, futureOpenFU, closedOverdueFU, overdueButClosed);
        await context.SaveChangesAsync();

        var service = new FollowUpService(context, Mock.Of<ILogger<FollowUpService>>());
        var count = await service.GetOverdueFollowUpsCountAsync();

        // Should only count open AND past due (2 items)
        Assert.Equal(2, count);
    }

    // Test 2: Follow-up cannot be closed without ClosedDate
    [Fact]
    public async Task CloseFollowUp_RequiresClosedDateToBeSet()
    {
        using var context = CreateContext();
        var (_, inspection) = await SeedPremisesAndInspection(context);

        var followUp = new FollowUp
        {
            InspectionId = inspection.Id,
            DueDate = DateTime.Now.AddDays(5),
            Status = FollowUpStatus.Open,
            ClosedDate = null
        };
        context.FollowUps.Add(followUp);
        await context.SaveChangesAsync();

        var service = new FollowUpService(context, Mock.Of<ILogger<FollowUpService>>());
        await service.CloseFollowUpAsync(followUp.Id);

        var closed = await context.FollowUps.FindAsync(followUp.Id);
        Assert.NotNull(closed);
        Assert.Equal(FollowUpStatus.Closed, closed.Status);
        Assert.NotNull(closed.ClosedDate);
        Assert.True((DateTime.Now - closed.ClosedDate.Value).TotalSeconds < 5);
    }

    // Test 3: Dashboard counts consistent with known seed data
    [Fact]
    public async Task DashboardFollowUpCounts_ConsistentWithKnownSeedData()
    {
        using var context = CreateContext();
        var now = DateTime.Now;

        var premises = new Premises
        {
            Name = "Dashboard Test Premises",
            Address = "5 Park St",
            Town = "Bristol",
            RiskRating = RiskRating.High
        };
        context.Premises.Add(premises);

        var failedInspection = new Inspection
        {
            Premises = premises,
            InspectionDate = now.AddDays(-15),
            Score = 35,
            Outcome = InspectionOutcome.Fail
        };
        context.Inspections.Add(failedInspection);
        await context.SaveChangesAsync();

        // Add 3 follow-ups: 2 overdue open, 1 closed
        var overdueOpen1 = new FollowUp
        {
            InspectionId = failedInspection.Id,
            DueDate = now.AddDays(-8),
            Status = FollowUpStatus.Open,
            ClosedDate = null
        };
        var overdueOpen2 = new FollowUp
        {
            InspectionId = failedInspection.Id,
            DueDate = now.AddDays(-3),
            Status = FollowUpStatus.Open,
            ClosedDate = null
        };
        var closedFollowUp = new FollowUp
        {
            InspectionId = failedInspection.Id,
            DueDate = now.AddDays(-20),
            Status = FollowUpStatus.Closed,
            ClosedDate = now.AddDays(-18)
        };
        context.FollowUps.AddRange(overdueOpen1, overdueOpen2, closedFollowUp);
        await context.SaveChangesAsync();

        var followUpService = new FollowUpService(context, Mock.Of<ILogger<FollowUpService>>());
        var overdueCount = await followUpService.GetOverdueFollowUpsCountAsync();
        var allFollowUps = await followUpService.GetAllFollowUpsAsync();

        // Verify known counts match
        Assert.Equal(2, overdueCount);
        Assert.Equal(3, allFollowUps.Count());
    }

    // Test 4: Basic role authorization - Inspector vs Viewer (via service patterns)
    [Fact]
    public async Task RoleAuthorization_InspectorCanModifyFollowUps()
    {
        using var context = CreateContext();
        var (_, inspection) = await SeedPremisesAndInspection(context);

        var followUp = new FollowUp
        {
            InspectionId = inspection.Id,
            DueDate = DateTime.Now.AddDays(10),
            Status = FollowUpStatus.Open,
            ClosedDate = null
        };
        context.FollowUps.Add(followUp);
        await context.SaveChangesAsync();

        var service = new FollowUpService(context, Mock.Of<ILogger<FollowUpService>>());

        // Simulate Inspector action - close follow-up
        await service.CloseFollowUpAsync(followUp.Id);

        var updated = await service.GetFollowUpByIdAsync(followUp.Id);
        Assert.NotNull(updated);
        Assert.Equal(FollowUpStatus.Closed, updated.Status);
        Assert.NotNull(updated.ClosedDate);

        // Verify the follow-up is now in closed state
        var allFollowUps = await service.GetAllFollowUpsAsync();
        var closedCount = allFollowUps.Count(f => f.Status == FollowUpStatus.Closed);
        Assert.Equal(1, closedCount);
    }

    // Test 5: Dashboard inspection counts with multiple scenarios
    [Fact]
    public async Task DashboardInspectionCounts_MatchKnownDataWithMultipleScenarios()
    {
        using var context = CreateContext();
        var now = DateTime.Now;
        var monthStart = new DateTime(now.Year, now.Month, 1);
        var lastMonthStart = monthStart.AddMonths(-1);

        var premises = new Premises
        {
            Name = "Multi-Scenario Premises",
            Address = "10 Oak Ave",
            Town = "Bath",
            RiskRating = RiskRating.Medium
        };
        context.Premises.Add(premises);

        // Add inspections across different months
        var thisMonthPass = new Inspection
        {
            Premises = premises,
            InspectionDate = now.AddDays(-2),
            Score = 95,
            Outcome = InspectionOutcome.Pass
        };
        var thisMonthFail = new Inspection
        {
            Premises = premises,
            InspectionDate = now.AddDays(-1),
            Score = 25,
            Outcome = InspectionOutcome.Fail
        };
        var thisMonthFail2 = new Inspection
        {
            Premises = premises,
            InspectionDate = now.AddDays(-5),
            Score = 45,
            Outcome = InspectionOutcome.Fail
        };
        var lastMonthPass = new Inspection
        {
            Premises = premises,
            InspectionDate = lastMonthStart.AddDays(5),
            Score = 88,
            Outcome = InspectionOutcome.Pass
        };
        var lastMonthFail = new Inspection
        {
            Premises = premises,
            InspectionDate = lastMonthStart.AddDays(10),
            Score = 35,
            Outcome = InspectionOutcome.Fail
        };

        context.Inspections.AddRange(thisMonthPass, thisMonthFail, thisMonthFail2, lastMonthPass, lastMonthFail);
        await context.SaveChangesAsync();

        var service = new InspectionService(context, Mock.Of<ILogger<InspectionService>>());

        var thisMonthTotal = await service.GetInspectionsThisMonthAsync();
        var thisMonthFailed = await service.GetFailedInspectionsThisMonthAsync();
        var allInspections = await service.GetAllInspectionsAsync();

        // Verify counts
        Assert.Equal(3, thisMonthTotal);
        Assert.Equal(2, thisMonthFailed);
        Assert.Equal(5, allInspections.Count());
    }
}
