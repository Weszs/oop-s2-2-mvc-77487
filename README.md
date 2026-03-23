# Food Safety Inspection Tracker

ASP.NET Core MVC application (.NET 9) for a local council to track food premises inspections, outcomes, and follow-ups.

## Tech Stack

- .NET 9 / ASP.NET Core MVC
- Entity Framework Core + SQLite
- ASP.NET Core Identity (role-based auth)
- Serilog (structured logging + file sink)
- xUnit + Moq (testing)
- Bootstrap 5

## Getting Started

```bash
dotnet restore
dotnet build
dotnet run --project oop-s2-2-mvc-77487
```

The database is created and seeded automatically on first run.

## Demo Credentials

| Role      | Username    | Password      |
|-----------|-------------|---------------|
| Admin     | admin       | Password123!  |
| Inspector | inspector   | Password123!  |
| Viewer    | viewer      | Password123!  |

## Entities

**Premises** - Id, Name, Address, Town, RiskRating (Low/Medium/High)

**Inspection** - Id, PremisesId, InspectionDate, Score (0-100), Outcome (Pass/Fail), Notes

**FollowUp** - Id, InspectionId, DueDate, Status (Open/Closed), ClosedDate

Relationships: Premises 1--* Inspection 1--* FollowUp

## Roles

- **Admin** - Full CRUD on all entities, audit trail access
- **Inspector** - Create/edit inspections and follow-ups
- **Viewer** - Read-only dashboard and listings

## Key Features

- Role-based authorization on all controllers
- Dashboard with KPI cards, filters by town/risk rating
- Overdue follow-up tracking
- Audit trail service (logged to Serilog + in-memory)
- Global exception handling middleware
- Seed data (12 premises, 25 inspections, 10 follow-ups)

## Project Structure

```
oop-s2-2-mvc-77487/
  Controllers/        HomeController, DashboardController, PremisesController,
                      InspectionsController, FollowUpsController, AccountController
  Models/             Premises, Inspection, FollowUp, ErrorViewModel
  Views/              Dashboard, Premises, Inspections, FollowUps, Account, Shared
  Services/           IPremisesService, IInspectionService, IFollowUpService, IAuditTrailService
  Data/               ApplicationDbContext, SeedData
  Middleware/         GlobalExceptionHandlingMiddleware
  Utilities/          AppConstants, ValidationExtensions
  Areas/Admin/        Admin panel with audit log
```

## Testing

```bash
dotnet test
```
