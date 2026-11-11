# RaceDay API (PROG6212 Part 2)

A RESTful API for managing road events: registration, event setup, enrolments and race results. Organisers run events; Participants enrol and track their results.

**Built with:** ASP.NET Core 11 · C# · Entity Framework Core (Code First) · SQL Server · session authentication · Swagger UI · xUnit · GitHub Actions

> **IDE note:** .NET 11 is only supported in **Visual Studio 2026**. Visual Studio 2022 cannot target it. If you prefer a terminal, the .NET 11 SDK builds the solution on its own.

---

## Contents

1. [Features](#1-features)
2. [Roles and permissions](#2-roles-and-permissions)
3. [Prerequisites](#3-prerequisites)
4. [Getting started](#4-getting-started)
5. [Database setup](#5-database-setup)
6. [Using the API through Swagger](#6-using-the-api-through-swagger)
7. [Endpoint reference](#7-endpoint-reference)
8. [Running the tests](#8-running-the-tests)
9. [Continuous integration](#9-continuous-integration)
10. [Commit plan](#10-commit-plan)
11. [Video presentation checklist](#11-video-presentation-checklist)
12. [Submission checklist](#12-submission-checklist)
13. [Scope and Part 3](#13-scope-and-part-3)

---

## 1. Features

- Registration and login for **Organiser** and **Participant** accounts
- Passwords hashed with ASP.NET Core `PasswordHasher<AppUser>`; plaintext passwords are never stored
- Server-side sessions holding the user's ID, role and email
- Role-based authorisation, plus ownership checks on protected endpoints
- Profile viewing and editing
- Event CRUD with filtering by event type
- Event categories (age ranges or distance ranges)
- Participant enrolments and Organiser enrolment views
- Result capture, result correction and Participant result history
- Swagger UI with XML endpoint descriptions
- EF Core Code First database creation
- Integration tests and a GitHub Actions build-and-test workflow

## 2. Roles and permissions

| Role | Can do |
|---|---|
| **Organiser** | Create, edit and delete their own events; manage categories; view enrolments for their events; record and correct results for their events |
| **Participant** | View events and categories; enrol once per event; view and cancel their own enrolments; view their own results; view and update their own profile |

All rules are enforced on the server. The role is saved at registration and read from the authenticated session, so changing a request body later cannot grant Organiser access.

## 3. Prerequisites

| Requirement | Notes |
|---|---|
| Visual Studio 2026 Community or later | Install the **ASP.NET and web development** workload |
| .NET 11 SDK | `dotnet --list-sdks` should show an `11.0.xxx` entry |
| SQL Server Database Engine | SQL Server Express LocalDB is enough for local development |
| SQL Server Management Studio (SSMS) | For inspecting the database |
| Git and a GitHub account | For version control and CI |

### Check that LocalDB is installed

In Windows PowerShell:

```powershell
sqllocaldb info
```

If `MSSQLLocalDB` is listed, inspect it:

```powershell
sqllocaldb info MSSQLLocalDB
```

If `sqllocaldb` is not recognised or no instance is listed, install SQL Server Express with LocalDB. Depending on your setup, LocalDB is also available through Visual Studio Installer under the *Data storage and processing* components.

**Downloads and docs**

- [.NET 11](https://dotnet.microsoft.com/download/dotnet/11.0)
- [Visual Studio](https://visualstudio.microsoft.com/vs/)
- [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads)
- [LocalDB documentation](https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb)

## 4. Getting started

1. Extract the project into your Part 2 repository branch or folder.
2. Open `RaceDay.sln` in Visual Studio 2026.
3. In Solution Explorer, confirm the two projects: `RaceDay.Api` and `RaceDay.Api.Tests`.
4. Wait for NuGet restore to finish.
5. Right-click the solution and choose **Build Solution**.

## 5. Database setup

### Connection string

The default is in `RaceDay.Api/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "RaceDay": "Server=(localdb)\\MSSQLLocalDB;Database=RaceDayApiDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
  }
}
```

This targets a separate database, `RaceDayApiDb`, so it won't clash with the `RaceDayDb` created by your Part 1 SQL script.

If you use a named SQL Server Express instance instead of LocalDB, change the server value:

```json
"RaceDay": "Server=localhost\\SQLEXPRESS;Database=RaceDayApiDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
```

Use the exact instance name shown in SSMS under **Connect > Database Engine > Server name**. Don't guess it.

### Creating the database (Code First)

On startup the API calls `EnsureCreatedAsync()`, which creates `RaceDayApiDb` and its tables from the EF Core model if the database doesn't exist. The `EventTypes` table is seeded with Run, Walk and Cycle.

1. Make sure SQL Server or LocalDB is running.
2. Set `RaceDay.Api` as the startup project.
3. Press **F5** or **Ctrl+F5**.
4. In SSMS, connect to `(localdb)\MSSQLLocalDB` (or your configured instance) and refresh **Databases**.
5. Confirm `RaceDayApiDb` exists and contains these six tables:
   - `dbo.AppUsers`
   - `dbo.EventTypes`
   - `dbo.Events`
   - `dbo.Categories`
   - `dbo.Enrolments`
   - `dbo.Results`

These match the six entities from Part 1.

Verification query:

```sql
USE RaceDayApiDb;
GO
SELECT name FROM sys.tables ORDER BY name;
SELECT * FROM dbo.EventTypes ORDER BY EventTypeId;
SELECT * FROM dbo.AppUsers;
SELECT * FROM dbo.Events;
SELECT * FROM dbo.Categories;
SELECT * FROM dbo.Enrolments;
SELECT * FROM dbo.Results;
GO
```

> **Schema changes:** `EnsureCreated` does not update an existing schema when the model changes. If you change the entities later, switch to EF Core migrations (replace `EnsureCreated` with `Migrate`). Never run the Part 1 SQL script against `RaceDayApiDb`; EF creates this database from the C# model.

## 6. Using the API through Swagger

1. Start `RaceDay.Api` with F5.
2. Browse to `https://localhost:7081/swagger` (or the URL shown in the Visual Studio output).
3. Use `POST /api/auth/register` to create one Organiser and one Participant, each with a different email address.
4. Call `POST /api/auth/login` with the account you want to test. The browser stores the `.RaceDay.Session` cookie automatically.
5. Call protected endpoints from the **same browser tab and origin**; the cookie is sent for you.
6. Call `POST /api/auth/logout` to end the session.

**Example registration request**

```json
{
  "firstName": "Ayesha",
  "lastName": "Khan",
  "email": "ayesha@example.com",
  "password": "StrongPass!2026",
  "phoneNumber": "0821234567",
  "role": "Participant"
}
```

**Example login request**

```json
{
  "email": "ayesha@example.com",
  "password": "StrongPass!2026"
}
```

### Suggested demo order

1. Register an Organiser and log in.
2. Create an event using `EventTypeId` `1` (Run), `2` (Walk) or `3` (Cycle).
3. Add an Age or Distance category to the event.
4. Log out, register a Participant and log in.
5. Browse events and categories.
6. Enrol using the event ID and category ID.
7. View `GET /api/enrolments/mine`.
8. Log out and log back in as the Organiser.
9. View the event's enrolments.
10. Mark the event as Completed with `PUT /api/events/{eventId}`, setting an event date in the past.
11. Record a result with `POST /api/enrolments/{enrolmentId}/result`.
12. Log in as the Participant and view `GET /api/results/mine`.

## 7. Endpoint reference

| Method | Route | Access |
|---|---|---|
| POST | `/api/auth/register` | Public |
| POST | `/api/auth/login` | Public |
| POST | `/api/auth/logout` | Authenticated |
| GET | `/api/profile` | Authenticated |
| PUT | `/api/profile` | Authenticated |
| GET | `/api/events` | Public |
| GET | `/api/events/{eventId}` | Public |
| POST | `/api/events` | Organiser |
| PUT | `/api/events/{eventId}` | Event-owning Organiser |
| DELETE | `/api/events/{eventId}` | Event-owning Organiser |
| GET | `/api/events/{eventId}/categories` | Public |
| POST | `/api/events/{eventId}/categories` | Event-owning Organiser |
| PUT | `/api/categories/{categoryId}` | Event-owning Organiser |
| DELETE | `/api/categories/{categoryId}` | Event-owning Organiser |
| POST | `/api/enrolments` | Participant |
| GET | `/api/enrolments/mine` | Participant |
| GET | `/api/events/{eventId}/enrolments` | Event-owning Organiser |
| GET | `/api/enrolments/{enrolmentId}` | Record owner or event Organiser |
| DELETE | `/api/enrolments/{enrolmentId}` | Enrolment-owning Participant |
| POST | `/api/enrolments/{enrolmentId}/result` | Event-owning Organiser |
| GET | `/api/results/mine` | Participant |
| GET | `/api/events/{eventId}/results` | Event-owning Organiser |
| GET | `/api/results/{resultId}` | Result owner or event Organiser |
| PUT | `/api/results/{resultId}` | Event-owning Organiser |

## 8. Running the tests

### Visual Studio

1. Open **Test > Test Explorer**.
2. Build the solution.
3. Click **Run All** and confirm every test passes.

### Command line

From the folder containing `RaceDay.sln`:

```powershell
dotnet restore RaceDay.sln
dotnet build RaceDay.sln --configuration Release
dotnet test RaceDay.sln --configuration Release
```

The integration tests use EF Core InMemory, so they don't need a SQL Server instance. The running API uses SQL Server via the connection string.

**Coverage:** registration, duplicate email rejection, invalid roles, login, incorrect passwords, unauthenticated access, role rejection, event creation, enrolment, duplicate enrolment, enrolment privacy, result capture and Participant result history.

## 9. Continuous integration

The workflow lives at `.github/workflows/part2-build-test.yml`. On every push and pull request it restores packages, builds the solution and runs all tests.

After pushing to GitHub:

1. Open the repository and select **Actions**.
2. Open **Part 2 - Build and Test RaceDay API**.
3. Wait for the run to finish and confirm the green check mark.
4. Screenshot the successful run and paste it below.

`[Add screenshot of successful green GitHub Actions build here]`

## 10. Commit plan

Commit after the described work is actually done. Don't create empty commits just to hit a number.

1. Create RaceDay ASP.NET Core API and test solution
2. Add EF Core SQL Server provider and database configuration
3. Create AppUser and EventType domain models
4. Create Event and Category domain models
5. Create Enrolment and Result domain models
6. Configure EF Core relationships and constraints
7. Add Code First database initialization and event type seed data
8. Implement session authentication handler
9. Implement secure registration and password hashing
10. Implement login, logout and session state
11. Add authenticated profile read and update endpoints
12. Implement public event browsing and filtering
13. Implement Organiser event create, update and delete endpoints
14. Implement event category management endpoints
15. Implement Participant enrolment and duplicate prevention
16. Implement Organiser enrolment views and ownership checks
17. Implement race result capture and Participant result history
18. Add Swagger documentation for API endpoints
19. Add API integration tests for authentication and authorisation
20. Add enrolment and results integration tests and GitHub Actions workflow

## 11. Video presentation checklist

Record your own voice and screen. Do not use an AI-generated voice.

- [ ] Introduce RaceDay and the API's purpose
- [ ] Walk through the Visual Studio solution and folder structure
- [ ] Explain the six EF Core entities and how they match Part 1
- [ ] Show registration, password hashing, login, the session cookie and logout
- [ ] Demonstrate Organiser and Participant permissions, including a forbidden request
- [ ] Demonstrate event and category creation, enrolment and results
- [ ] Show Swagger UI and explain request bodies and response codes
- [ ] Run all tests and show them passing
- [ ] Show the green GitHub Actions build

**YouTube video:** `[Add your unlisted Part 2 demonstration URL here]`

## 12. Submission checklist

- [ ] Solution opens and builds in Visual Studio 2026
- [ ] API starts and Swagger loads
- [ ] `RaceDayApiDb` is created and all six tables are verified in SSMS
- [ ] Registered and logged in as both roles
- [ ] Demonstrated every endpoint group
- [ ] Unauthenticated and wrong-role requests are rejected
- [ ] All tests pass locally
- [ ] All files pushed to the Part 2 branch/folder
- [ ] At least 20 meaningful commits from your own GitHub account
- [ ] GitHub Actions is green and the screenshot is added above
- [ ] Narrated video recorded and unlisted YouTube link added above
- [ ] GitHub repository link submitted on ARC

## 13. Scope and Part 3

This is the Part 2 starter. Part 3 extends it with Azure Blob Storage image uploads, an MVC frontend and a Docker container. Those features are intentionally not implemented here.
