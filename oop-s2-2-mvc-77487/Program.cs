using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using oop_s2_2_mvc_77487.Data;
using oop_s2_2_mvc_77487.Middleware;
using oop_s2_2_mvc_77487.Services;

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Configure Serilog
    Log.Logger = new LoggerConfiguration()
        .MinimumLevel.Information()
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "FoodSafetyInspectionTracker")
        .Enrich.WithProperty("Environment", builder.Environment.EnvironmentName)
        .WriteTo.Console()
        .WriteTo.File(
            path: "logs/inspection-tracker-.txt",
            rollingInterval: RollingInterval.Day,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties}{NewLine}{Exception}")
        .CreateLogger();

    builder.Host.UseSerilog();

    Log.Information("Starting Food Safety Inspection Tracker application");

    // Add services to the container
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=inspection.db"));

    builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
    {
        options.Password.RequireDigit = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

    builder.Services.ConfigureApplicationCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
    });

    // Register application services
    builder.Services.AddScoped<IInspectionService, InspectionService>();
    builder.Services.AddScoped<IPremisesService, PremisesService>();
    builder.Services.AddScoped<IFollowUpService, FollowUpService>();
    builder.Services.AddSingleton<IAuditTrailService, AuditTrailService>();

    builder.Services.AddControllersWithViews();

    var app = builder.Build();

    Log.Information("Configuring HTTP pipeline");

    // Apply migrations and seed data
    using (var scope = app.Services.CreateScope())
    {
        try
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<SeedData>>();

            Log.Information("Applying database migrations");
            dbContext.Database.Migrate();

            Log.Information("Seeding database");
            await SeedData.InitializeAsync(dbContext, userManager, roleManager, logger);
            Log.Information("Database initialized and seeded successfully");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error during database initialization");
            throw;
        }
    }

    // Configure the HTTP request pipeline
    app.UseMiddleware<GlobalExceptionHandlingMiddleware>();

    if (!app.Environment.IsDevelopment())
    {
        app.UseHsts();
    }
    else
    {
        app.UseDeveloperExceptionPage();
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseRouting();

    app.UseAuthentication();
    app.UseAuthorization();

    // Areas must be mapped BEFORE default routes
    app.MapControllerRoute(
        name: "areas",
        pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");

    // Default route for non-area controllers
    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");

    Log.Information("Application configured successfully");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
