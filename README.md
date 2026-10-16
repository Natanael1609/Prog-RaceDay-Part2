# RaceDay — Part 2: RESTful API Development

ASP.NET Core 8 Web API, C#, SQL Server, EF Core Code-First, Swagger, xUnit, and GitHub Actions.

## Based on Part 1

The `/docs` folder preserves the provided Part 1 SQL script, ERD, and 26-endpoint plan. Part 2 implements all **26 planned endpoints**, plus `POST /api/auth/logout`.

**Part 2 amendment:** The Part 1 ERD did not have `Events.EventType`. Part 2 includes a required `EventType` property (Run / Walk / Cycle), as explicitly demanded by the new brief. This is a documented schema change. The Part 1 SQL script includes sample passwords with placeholder hashes, which are **not** valid password hashes; do not import those accounts for login. New accounts use ASP.NET Core `PasswordHasher<User>`.

**Authentication:** Browser Swagger uses an HttpOnly login cookie, which retains role and user ID across calls without copying tokens. JWT bearer is also returned from login and accepted for compatibility with the Part 1 endpoint plan. The `logout` route clears the cookie, but does not revoke previously issued JWTs. HTTPS is essential for deployment; never use the development JWT secret in production.

**Note on registration:** Both Organiser and Participant can self-select their role because the assignment requires this. For a real public service, Organiser access should instead require an invitation or approval. Browser cookie authentication requires additional anti-CSRF measures before using it for a production website.

## Requirements

- Visual Studio 2022 with .NET 8 SDK and ASP.NET/Web development workload
- SQL Server Express or SQL Server LocalDB (Visual Studio installer) 
- SQL Server Management Studio is optional

## Getting started

1. Extract ZIP and open `RaceDay.sln` in Visual Studio 2022.
2. Check `RaceDay.Api/appsettings.json`. Default connection points to `(localdb)\MSSQLLocalDB` and creates database `RaceDayDB_Part2`. Use your own SQL Server instance if LocalDB isn't installed.
3. Open **Tools > NuGet Package Manager > Package Manager Console**. Choose **RaceDay.Api** as Default Project.
4. Run `Add-Migration InitialCreate` then `Update-Database`. Alternatively in a terminal:
   ```bash
   dotnet tool install --global dotnet-ef --version 8.0.11
   dotnet ef migrations add InitialCreate --project RaceDay.Api
   dotnet ef database update --project RaceDay.Api
   ```
   The first command may be skipped if `dotnet-ef` is already installed.
5. Select **RaceDay.Api** as startup project and run HTTPS profile using F5. Open `https://localhost:7162/swagger` (port may vary).
6. In Swagger, first run `POST /api/auth/register` with role `Organiser`, then `POST /api/auth/login`. Swagger will carry the HttpOnly session cookie in the same browser; you can now create events. Register a `Participant` separately to test participant enrolments. If switching users, call `POST /api/auth/logout` and log in again.
7. Optional JWT: copy the token from login, open Swagger **Authorize**, enter `Bearer <token>`.
8. Run tests from **Test > Run All Tests**, or `dotnet test RaceDay.sln`.

## Examples

`POST /api/auth/register`
```json
{"firstName":"Sipho","lastName":"Dlamini","email":"sipho@example.com","password":"Password123!","phone":"0710000003","role":"Participant"}
```
`POST /api/auth/login`
```json
{"email":"sipho@example.com","password":"Password123!"}
```
`POST /api/events` (Organiser only)
```json
{"name":"Gqeberha Coastal Run","eventDate":"2027-10-18","location":"Gqeberha","distanceKm":10,"description":"Community coastal run","status":"Open","eventType":"Run"}
```
`POST /api/events/1/categories` (event-owning Organiser)
```json
{"name":"10 km Open","maxParticipants":500,"entryFee":150}
```
`POST /api/categories/1/enrolments` (Participant only)
```json
{"raceNumber":105}
```

## API routes

| Method | Path | Access |
|---|---|---|
| POST | /api/auth/register | Public |
| POST | /api/auth/login | Public |
| POST | /api/auth/logout | Session user (cookie clearance) |
| GET, PUT | /api/users/me | Signed-in user |
| GET | /api/events, /api/events/{eventId} | Public |
| POST | /api/events | Organiser |
| PUT, DELETE | /api/events/{eventId} | Owning Organiser |
| GET | /api/events/{eventId}/categories | Public |
| POST | /api/events/{eventId}/categories | Owning Organiser |
| PUT, DELETE | /api/categories/{categoryId} | Owning Organiser |
| POST | /api/categories/{categoryId}/enrolments | Participant |
| GET | /api/users/me/enrolments | Participant |
| GET | /api/events/{eventId}/enrolments | Owning Organiser |
| PUT, DELETE | /api/enrolments/{enrolmentId} | Participant owner or event organiser |
| POST | /api/events/{eventId}/results | Owning Organiser |
| GET | /api/events/{eventId}/results, /api/results/{resultId} | Public |
| GET | /api/users/me/results | Participant |
| PUT, DELETE | /api/results/{resultId} | Owning Organiser |
| GET | /api/events/{eventId}/route | Public |
| PUT | /api/events/{eventId}/route | Owning Organiser |
| GET | /api/events/{eventId}/weather | Public; external provider required |

Expected HTTP statuses: 200, 201, 204, 400, 401, 403, 404, 409; 502 for external weather errors. Model binding returns 400 for invalid required fields. XML comments on controllers appear as descriptions in Swagger.

## Weather endpoint

The weather service needs a real OpenWeather API key. Configure it locally using .NET Secret Manager or environment variable `Weather__ApiKey`. It returns 502 when the provider is not configured or fails; no fake weather is returned. The OpenWeather endpoint used is current weather rather than a race-day forecast. This is a deliberate limitation; change to the provider's forecast API if the rubric expects future forecasts.

## CI/CD

`.github/workflows/raceday-api.yml` restores, builds and executes xUnit tests on every source-code push or pull request. Push to **your own** GitHub repository, under a Part 2 folder or branch. If storing this in a Part 2 subfolder of a larger repository, update the workflow paths and `dotnet` working directory accordingly.

**The required 20 meaningful commits must actually be made from your account.** The provided ZIP has no fabricated Git history. Example authentic commit milestones while implementing/testing and documenting your own work:

1. Create Part 2 solution and projects
2. Add database entity classes
3. Configure EF Core relationships
4. Add model constraints and indexes
5. Add SQL Server configuration
6. Add registration DTO and validation
7. Implement hashed user registration
8. Add login and password verification
9. Add session-cookie authentication
10. Add JWT compatibility
11. Implement user profile endpoints
12. Implement event endpoints
13. Enforce organiser ownership rules
14. Implement category endpoints
15. Implement enrolment endpoints
16. Add enrolment duplicate/capacity validation
17. Implement results endpoints
18. Implement route and weather endpoints
19. Add Swagger descriptions and authentication
20. Add tests, GitHub Actions and README

These are suggested logical development stages, **not** 20 commits already present. Make truthful commits as you work and verify `git log --oneline`.

### GitHub Actions proof

After pushing, open **GitHub > Actions > RaceDay Part 2 CI**, select the latest successful run (green check), take a screenshot and save to `docs/github-actions-green-build.png`. Then add this line to the README and commit the screenshot:

`![Successful GitHub Actions build](docs/github-actions-green-build.png)`

The ZIP intentionally does **not** contain an invented green-build screenshot. GitHub Actions can only be confirmed after the workflow runs in your repository.

## Test coverage

`RaceDay.Tests/ApiTests.cs` contains xUnit integration-style API tests (using an EF Core InMemory test database) covering registration, duplicate email, bad login, unauthenticated event requests, organiser event creation/update, participant role denial, organiser enrolment denial, successful participant enrolment, duplicate enrolments, organiser-only enrolment lists, invalid roles, and missing events. SQL Server-specific migration behaviour and concurrency must additionally be verified on SQL Server.

## Testing status / limitations

**Source code has been prepared but not compiled or executed in this sandbox**, because the .NET SDK and SQL Server are not installed here. Do not claim that tests passed or CI is green until you run them locally/GitHub. The supplied EF Core Code-First models match the six main entities of Part 1, with the added EventType column. Code-First migrations are generated locally using the instructions above; do not run the original Part 1 SQL script over the newly created database.

This is a student-friendly coursework project, not a production-hardened service. Extra security measures (anti-CSRF for cookie API, production secrets management, organiser approval, rate limiting and JWT revocation) would be needed before public deployment.
