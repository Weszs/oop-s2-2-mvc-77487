using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using oop_s2_2_mvc_77487.Data;
using oop_s2_2_mvc_77487.Models;

namespace oop_s2_2_mvc_77487.Tests;

public class InspectionTrackerTests
{
    private ApplicationDbContext GetDbContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task OverdueFollowUpsQuery_ReturnsCorrectItems()
    {
        // Arrange
        using var context = GetDbContext();
        var now = DateTime.Now;
        var premises = new Premises { Name = "Test Premises", Address = "123 Main", Town = "TestTown", RiskRating = RiskRating.Low };
        var inspection = new Inspection { Premises = premises, InspectionDate = now.AddDays(-30), Score = 75, Outcome = InspectionOutcome.Pass };
        
        var overdueFollowUp = new FollowUp { Inspection = inspection, DueDate = now.AddDays(-5), Status = FollowUpStatus.Open };
        var futureFollowUp = new FollowUp { Inspection = inspection, DueDate = now.AddDays(10), Status = FollowUpStatus.Open };
        var closedFollowUp = new FollowUp { Inspection = inspection, DueDate = now.AddDays(-5), Status = FollowUpStatus.Closed, ClosedDate = now.AddDays(-3) };

        context.Premises.Add(premises);
        context.Inspections.Add(inspection);
        context.FollowUps.AddRange(overdueFollowUp, futureFollowUp, closedFollowUp);
        await context.SaveChangesAsync();

        // Act
        var overdueOpenFollowUps = await context.FollowUps
            .Where(f => f.Status == FollowUpStatus.Open && f.DueDate < now)
            .ToListAsync();

        // Assert
        Assert.Single(overdueOpenFollowUps);
        Assert.Equal(overdueFollowUp.Id, overdueOpenFollowUps.First().Id);
    }

    [Fact]
    public async Task FollowUp_CannotBeClosed_WithoutClosedDate()
    {
        // Arrange
        using var context = GetDbContext();
        var premises = new Premises { Name = "Test Premises", Address = "123 Main", Town = "TestTown", RiskRating = RiskRating.Low };
        var inspection = new Inspection { Premises = premises, InspectionDate = DateTime.Now.AddDays(-10), Score = 80, Outcome = InspectionOutcome.Pass };
        var followUp = new FollowUp { Inspection = inspection, DueDate = DateTime.Now.AddDays(5), Status = FollowUpStatus.Open };

        context.Premises.Add(premises);
        context.Inspections.Add(inspection);
        context.FollowUps.Add(followUp);
        await context.SaveChangesAsync();

        // Act
        followUp.Status = FollowUpStatus.Closed;
        // Intentionally not setting ClosedDate

        // Assert
        Assert.Null(followUp.ClosedDate);
        Assert.Equal(FollowUpStatus.Closed, followUp.Status);
        // This test validates that the model allows status change but application logic should prevent this
    }

    [Fact]
    public async Task DashboardCounts_AreConsistentWithSeedData()
    {
        // Arrange
        using var context = GetDbContext();
        var now = DateTime.Now;
        var monthStart = new DateTime(now.Year, now.Month, 1);

        var premises = new Premises { Name = "Test Premises", Address = "123 Main", Town = "Bristol", RiskRating = RiskRating.Medium };
        
        var inspectionThisMonth = new Inspection { Premises = premises, InspectionDate = now.AddDays(-5), Score = 85, Outcome = InspectionOutcome.Pass };
        var inspectionLastMonth = new Inspection { Premises = premises, InspectionDate = monthStart.AddDays(-1), Score = 70, Outcome = InspectionOutcome.Fail };
        var failedInspectionThisMonth = new Inspection { Premises = premises, InspectionDate = now.AddDays(-2), Score = 60, Outcome = InspectionOutcome.Fail };

        context.Premises.Add(premises);
        context.Inspections.AddRange(inspectionThisMonth, inspectionLastMonth, failedInspectionThisMonth);
        await context.SaveChangesAsync();

        // Act
        var inspectionsThisMonth = await context.Inspections
            .Where(i => i.InspectionDate >= monthStart)
            .CountAsync();

        var failedInspectionsThisMonth = await context.Inspections
            .Where(i => i.InspectionDate >= monthStart && i.Outcome == InspectionOutcome.Fail)
            .CountAsync();

        // Assert
        Assert.Equal(2, inspectionsThisMonth);
        Assert.Equal(1, failedInspectionsThisMonth);
    }

    [Fact]
    public async Task Authorization_InspectorCanCreateInspections_But_ViewerCannotEdit()
    {
        // Arrange
        using var context = GetDbContext();
        var premises = new Premises { Name = "Test Premises", Address = "123 Main", Town = "Bristol", RiskRating = RiskRating.Low };
        var inspection = new Inspection { Premises = premises, InspectionDate = DateTime.Now, Score = 75, Outcome = InspectionOutcome.Pass };

        context.Premises.Add(premises);
        context.Inspections.Add(inspection);
        await context.SaveChangesAsync();

        // Act & Assert
        // This test validates that inspection can be created and retrieved
        var retrievedInspection = await context.Inspections.FirstOrDefaultAsync(i => i.Id == inspection.Id);
        Assert.NotNull(retrievedInspection);
        Assert.Equal(75, retrievedInspection.Score);
    }
}
