# API project notes

- Controllers are grouped by resource.
- DTOs define validated request and response shapes; EF entities are not returned directly.
- `SessionAuthenticationHandler` converts server-side session values into claims used by `[Authorize]` and `[Authorize(Roles = "...")]`.
- `AppDbContext` defines the Part 1 relational schema and constraints using EF Core Fluent API.
- `Program.cs` configures SQL Server for normal execution and EF Core InMemory only for integration tests.
- The API uses a demo-friendly in-memory distributed session cache. For a multi-instance production deployment, replace it with a distributed session store such as SQL Server or Redis.
