using Microsoft.AspNetCore.Identity;
using oop_s2_2_mvc_77487.Models;

namespace oop_s2_2_mvc_77487.Data
{
    public class SeedData
    {
        public static async Task InitializeAsync(ApplicationDbContext context, UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager, ILogger<SeedData> logger)
        {
            try
            {
                // Create roles
                var roles = new[] { "Admin", "Inspector", "Viewer" };
                foreach (var role in roles)
                {
                    if (!await roleManager.RoleExistsAsync(role))
                    {
                        await roleManager.CreateAsync(new IdentityRole(role));
                        logger.LogInformation("Role {Role} created", role);
                    }
                }

                // Create seed users
                var admin = await userManager.FindByEmailAsync("admin@inspection.test");
                if (admin == null)
                {
                    admin = new IdentityUser { UserName = "admin", Email = "admin@inspection.test" };
                    await userManager.CreateAsync(admin, "Password123!");
                    await userManager.AddToRoleAsync(admin, "Admin");
                    logger.LogInformation("Admin user created");
                }

                var inspector = await userManager.FindByEmailAsync("inspector@inspection.test");
                if (inspector == null)
                {
                    inspector = new IdentityUser { UserName = "inspector", Email = "inspector@inspection.test" };
                    await userManager.CreateAsync(inspector, "Password123!");
                    await userManager.AddToRoleAsync(inspector, "Inspector");
                    logger.LogInformation("Inspector user created");
                }

                var viewer = await userManager.FindByEmailAsync("viewer@inspection.test");
                if (viewer == null)
                {
                    viewer = new IdentityUser { UserName = "viewer", Email = "viewer@inspection.test" };
                    await userManager.CreateAsync(viewer, "Password123!");
                    await userManager.AddToRoleAsync(viewer, "Viewer");
                    logger.LogInformation("Viewer user created");
                }

                // Seed premises if empty
                if (!context.Premises.Any())
                {
                    var premises = new[]
                    {
                        new Premises { Name = "The Italian Kitchen", Address = "12 Main St", Town = "Bristol", RiskRating = RiskRating.Low },
                        new Premises { Name = "Fast Food Express", Address = "45 High St", Town = "Bristol", RiskRating = RiskRating.Medium },
                        new Premises { Name = "Burger Palace", Address = "78 Park Ave", Town = "Bristol", RiskRating = RiskRating.High },
                        new Premises { Name = "Chinese Wok", Address = "23 Station Rd", Town = "Bath", RiskRating = RiskRating.Low },
                        new Premises { Name = "The Fish Place", Address = "56 Ocean Dr", Town = "Bath", RiskRating = RiskRating.Medium },
                        new Premises { Name = "Spice Route", Address = "89 Queens Rd", Town = "Bath", RiskRating = RiskRating.High },
                        new Premises { Name = "Veggie Corner", Address = "34 Market St", Town = "Wells", RiskRating = RiskRating.Low },
                        new Premises { Name = "Pizza Time", Address = "67 Chapel Ln", Town = "Wells", RiskRating = RiskRating.Medium },
                        new Premises { Name = "Meat & Grill", Address = "90 Broad St", Town = "Wells", RiskRating = RiskRating.High },
                        new Premises { Name = "The Tea House", Address = "11 Mill Ln", Town = "Bristol", RiskRating = RiskRating.Low },
                        new Premises { Name = "Kebab Master", Address = "22 New Rd", Town = "Bath", RiskRating = RiskRating.Medium },
                        new Premises { Name = "Deli Delights", Address = "33 King St", Town = "Wells", RiskRating = RiskRating.Low }
                    };

                    context.Premises.AddRange(premises);
                    await context.SaveChangesAsync();
                    logger.LogInformation("Seeded 12 premises");
                }

                // Seed inspections if empty
                if (!context.Inspections.Any())
                {
                    var premises = context.Premises.ToList();
                    var inspections = new List<Inspection>();
                    var today = DateTime.Now;

                    // Create 25 inspections across different dates
                    for (int i = 0; i < 25; i++)
                    {
                        var premisesIndex = i % premises.Count;
                        var daysAgo = (i / premises.Count) * 30 + (i % 10);
                        var inspectionDate = today.AddDays(-daysAgo);

                        inspections.Add(new Inspection
                        {
                            PremisesId = premises[premisesIndex].Id,
                            InspectionDate = inspectionDate,
                            Score = 70 + (i % 30),
                            Outcome = (i % 5 == 0) ? InspectionOutcome.Fail : InspectionOutcome.Pass,
                            Notes = $"Inspection {i + 1}: Standard checks completed"
                        });
                    }

                    context.Inspections.AddRange(inspections);
                    await context.SaveChangesAsync();
                    logger.LogInformation("Seeded 25 inspections");
                }

                // Seed follow-ups if empty
                if (!context.FollowUps.Any())
                {
                    var inspections = context.Inspections.ToList();
                    var followUps = new List<FollowUp>();
                    var today = DateTime.Now;

                    // Create 10 follow-ups from failed or medium-risk inspections
                    var failedInspections = inspections.Where(i => i.Outcome == InspectionOutcome.Fail).Take(5).ToList();

                    for (int i = 0; i < 5 && i < failedInspections.Count; i++)
                    {
                        var isOverdue = i < 2; // First 2 are overdue
                        var dueDate = isOverdue ? today.AddDays(-(10 + i * 5)) : today.AddDays(10 + i * 5);

                        followUps.Add(new FollowUp
                        {
                            InspectionId = failedInspections[i].Id,
                            DueDate = dueDate,
                            Status = isOverdue ? FollowUpStatus.Open : FollowUpStatus.Open,
                            ClosedDate = null
                        });
                    }

                    // Add some closed follow-ups
                    for (int i = 5; i < 10 && i - 5 < failedInspections.Count; i++)
                    {
                        var dueDate = today.AddDays(-30 - ((i - 5) * 5));
                        followUps.Add(new FollowUp
                        {
                            InspectionId = failedInspections[i - 5].Id,
                            DueDate = dueDate,
                            Status = FollowUpStatus.Closed,
                            ClosedDate = dueDate.AddDays(2)
                        });
                    }

                    context.FollowUps.AddRange(followUps);
                    await context.SaveChangesAsync();
                    logger.LogInformation("Seeded 10 follow-ups");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error seeding database");
                throw;
            }
        }
    }
}
