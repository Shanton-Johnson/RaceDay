# RaceDay – PROG6212 Part 2 RESTful API

This solution implements the RaceDay REST API in ASP.NET Core 11, using C#, Entity Framework Core Code First, SQL Server, session authentication, role-based authorization, Swagger UI, xUnit integration tests, and GitHub Actions.

> **IDE compatibility:** .NET 11 requires Visual Studio 2026 for supported Visual Studio IDE development. Visual Studio 2022 does not officially support targeting .NET 11. Install Visual Studio 2026 Community (ASP.NET and web development workload) before opening this solution. The .NET 11 SDK can also build the solution from a terminal.

## 1. Features

- Registration and login for `Organiser` and `Participant` roles.
- Password hashing using ASP.NET Core `PasswordHasher<AppUser>`; plaintext passwords are never stored.
- Server-side session state stores the authenticated user ID, role, and email.
- Role-based authorization and event ownership checks on protected endpoints.
- User profile viewing and editing.
- Event CRUD and event-type filtering.
- Event categories for age ranges or distance ranges.
- Participant enrolments and Organiser enrolment views.
- Results capture, correction, and Participant result history.
- Swagger UI with XML endpoint descriptions.
- EF Core Code-First database creation.
- Automated API integration tests and GitHub Actions build/test workflow.

## 2. Roles

- **Organiser:** creates, edits and deletes their own events; manages categories; views enrolments for their events; records and corrects results for their events.
- **Participant:** views events and categories; enrols once per event; views and cancels their own enrolments; views their own results; views and updates their own profile.

The API enforces these rules on the server. A user cannot gain Organiser access by changing a request body after registration; the role is saved at registration and read from the authenticated session.

## 3. Software required

1. Visual Studio 2026 Community or later, with **ASP.NET and web development** workload.
2. .NET 11 SDK (`dotnet --list-sdks` should list an `11.0.xxx` SDK).
3. SQL Server Database Engine. For local development, SQL Server Express LocalDB is sufficient.
4. SQL Server Management Studio (SSMS), which you already have, to inspect the database.
5. Git and a GitHub account.

### Check whether LocalDB is installed

Open **Windows PowerShell** and run:

```powershell
sqllocaldb info
```

If the output lists `MSSQLLocalDB`, check that instance:

```powershell
sqllocaldb info MSSQLLocalDB
```

If PowerShell says `sqllocaldb` is not recognised or no instance is listed, install SQL Server Express with LocalDB. LocalDB can also be installed through Visual Studio Installer under the Data storage and processing components, depending on the installed SQL Server/Visual Studio components.

Official references:
- .NET downloads: https://dotnet.microsoft.com/download/dotnet/11.0
- Visual Studio: https://visualstudio.microsoft.com/vs/
- SQL Server downloads: https://www.microsoft.com/en-us/sql-server/sql-server-downloads
- LocalDB documentation: https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb

## 4. Open the solution in Visual Studio

1. Extract the project folder into your Part 2 repository branch/folder.
2. Open `RaceDay.sln` in Visual Studio 2026.
3. In Solution Explorer, confirm there are two projects: `RaceDay.Api` and `RaceDay.Api.Tests`.
4. Wait for NuGet package restore to finish.
5. Right-click the solution and choose **Build Solution**.

## 5. Configure SQL Server

The default connection string is in `RaceDay.Api/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "RaceDay": "Server=(localdb)\\MSSQLLocalDB;Database=RaceDayApiDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
  }
}
```

This uses a separate database named `RaceDayApiDb`. The table design and constraints correspond to the six Part 1 entities. Keeping this as a separate database avoids conflicts with the `RaceDayDb` created by your Part 1 SQL script.

If you have a named SQL Server Express instance instead of LocalDB, edit the server part. A common Express connection string is:

```json
"RaceDay": "Server=localhost\\SQLEXPRESS;Database=RaceDayApiDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
```

Use the exact instance name shown in SSMS under **Connect > Database Engine > Server name**. Do not guess the instance name.

## 6. Create the database (EF Core Code First)

The API calls `EnsureCreatedAsync()` when it starts. This creates `RaceDayApiDb` and the tables from the EF Core model if the database does not already exist. The EventTypes table is seeded with Run, Walk, and Cycle.

1. Start the SQL Server Database Engine / LocalDB.
2. In Visual Studio, set `RaceDay.Api` as the startup project.
3. Press **F5** or **Ctrl+F5**.
4. The API creates the database on startup.
5. Open SSMS and connect to `(localdb)\\MSSQLLocalDB` (or your configured instance).
6. Refresh Databases. You should see `RaceDayApiDb`.
7. Expand Tables and confirm these six tables exist:
   - `dbo.AppUsers`
   - `dbo.EventTypes`
   - `dbo.Events`
   - `dbo.Categories`
   - `dbo.Enrolments`
   - `dbo.Results`

Example SSMS verification query:

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

**Code-First note:** this coursework starter uses `EnsureCreated` to make the first run straightforward. `EnsureCreated` does not update an existing schema when the model changes. If you later change entities, use EF Core migrations (and replace `EnsureCreated` with `Migrate`) before relying on the changed schema. Do not run the Part 1 SQL script against `RaceDayApiDb`; EF creates this database from the C# model.

## 7. Run and use Swagger

1. Start `RaceDay.Api` with F5.
2. Browse to `https://localhost:7081/swagger` (or the URL shown in the Visual Studio output/browser).
3. Expand `POST /api/auth/register` and register a test user.
4. Register one Organiser and one Participant using different email addresses.
5. Call `POST /api/auth/login` with the account you want to test. The browser stores the `.RaceDay.Session` cookie automatically.
6. In the same Swagger browser tab and origin, call protected endpoints. The cookie is sent automatically.
7. Call `POST /api/auth/logout` to clear the current session.

Example registration request:

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

Example login request:

```json
{
  "email": "ayesha@example.com",
  "password": "StrongPass!2026"
}
```

### Suggested API demonstration order

1. Register Organiser.
2. Log in as Organiser.
3. Create an event using EventTypeId `1` (Run), `2` (Walk), or `3` (Cycle).
4. Add an Age or Distance category to that event.
5. Log out, register a Participant, and log in as Participant.
6. Browse events and categories.
7. Enrol in the event using its event ID and category ID.
8. View `/api/enrolments/mine`.
9. Log out and log in as the Organiser.
10. View that event's enrolments.
11. Mark the event as Completed using `PUT /api/events/{eventId}` and set an event date in the past.
12. Record a result using `POST /api/enrolments/{enrolmentId}/result`.
13. Log in as the Participant and view `/api/results/mine`.

## 8. Endpoint list

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
| GET | `/api/enrolments/{enrolmentId}` | Record owner / event Organiser |
| DELETE | `/api/enrolments/{enrolmentId}` | Enrolment-owning Participant |
| POST | `/api/enrolments/{enrolmentId}/result` | Event-owning Organiser |
| GET | `/api/results/mine` | Participant |
| GET | `/api/events/{eventId}/results` | Event-owning Organiser |
| GET | `/api/results/{resultId}` | Result owner / event Organiser |
| PUT | `/api/results/{resultId}` | Event-owning Organiser |

## 9. Run automated tests

### Visual Studio Test Explorer

1. Open **Test > Test Explorer**.
2. Build the solution.
3. Select **Run All**.
4. Confirm all tests pass.

### Command line

Open a terminal in the folder containing `RaceDay.sln`:

```powershell
dotnet restore RaceDay.sln
dotnet build RaceDay.sln --configuration Release
dotnet test RaceDay.sln --configuration Release
```

The integration tests use EF Core InMemory, so they do not require a SQL Server instance. The actual API uses SQL Server through the connection string.

Tests cover registration, duplicate email rejection, invalid roles, login, incorrect passwords, unauthenticated access, role rejection, event creation, enrolment, duplicate enrolment, enrolment privacy, result capture, and Participant result history.

## 10. GitHub Actions CI/CD

The workflow is `.github/workflows/part2-build-test.yml`. It restores packages, builds the solution, and runs all tests on every push and pull request.

After pushing to GitHub:

1. Open the repository on GitHub.
2. Select **Actions**.
3. Open **Part 2 - Build and Test RaceDay API**.
4. Wait for the workflow to finish.
5. Confirm the run has a green check mark.
6. Take a screenshot of the successful run and insert it below.

`[Add screenshot of successful green GitHub Actions build here]`

## 11. Suggested meaningful Part 2 commit sequence

Make each commit after the described work has actually been completed. Do not manufacture empty commits.

1. `Create RaceDay ASP.NET Core API and test solution`
2. `Add EF Core SQL Server provider and database configuration`
3. `Create AppUser and EventType domain models`
4. `Create Event and Category domain models`
5. `Create Enrolment and Result domain models`
6. `Configure EF Core relationships and constraints`
7. `Add Code First database initialization and event type seed data`
8. `Implement session authentication handler`
9. `Implement secure registration and password hashing`
10. `Implement login logout and session state`
11. `Add authenticated profile read and update endpoints`
12. `Implement public event browsing and filtering`
13. `Implement Organiser event create update and delete endpoints`
14. `Implement event category management endpoints`
15. `Implement Participant enrolment and duplicate prevention`
16. `Implement Organiser enrolment views and ownership checks`
17. `Implement race result capture and Participant result history`
18. `Add Swagger documentation for API endpoints`
19. `Add API integration tests for authentication and authorization`
20. `Add enrolment and results integration tests and GitHub Actions workflow`

## 12. Video presentation checklist

Record your own voice and screen. Do not use an AI-generated voice.

- Introduce RaceDay and explain the API's purpose.
- Show the Visual Studio solution and explain the folder structure.
- Explain the six EF Core entities and how they match Part 1.
- Show registration, password hashing, login, session cookie, and logout.
- Demonstrate Organiser and Participant permissions, including a forbidden request.
- Demonstrate event/category creation, enrolment, and results.
- Show Swagger UI and explain the request bodies and response codes.
- Run all unit tests and show the passing results.
- Show the GitHub Actions green build.

**YouTube video:** `[Add your unlisted Part 2 demonstration URL here]`

## 13. Before submitting

- [ ] Open and build the solution in Visual Studio 2026.
- [ ] Confirm the API starts and Swagger loads.
- [ ] Confirm `RaceDayApiDb` is created in SQL Server.
- [ ] Verify all six tables in SSMS.
- [ ] Register and log in as both roles.
- [ ] Demonstrate all endpoint groups.
- [ ] Confirm unauthenticated and wrong-role requests are rejected.
- [ ] Run all tests locally and confirm they pass.
- [ ] Push all files to your Part 2 branch/folder.
- [ ] Make at least 20 meaningful commits from your own GitHub account.
- [ ] Confirm GitHub Actions is green and add a screenshot here.
- [ ] Record your narrated video and add the unlisted YouTube link.
- [ ] Submit the GitHub repository link on ARC.

## 14. Scope boundary for Part 3

Part 3 extends this API with Azure Blob Storage image uploads and adds the MVC frontend and Docker container. Those features are intentionally not implemented in this Part 2 starter; they will be added when completing Part 3, as required by the POE.
#   R a c e D a y  
 