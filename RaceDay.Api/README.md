# RaceDay — PROG6212 Part 2 REST API

## Project Description

RaceDay is an API for organising community running, walking and cycling events. Part 2 builds on the Part 1 planning work by adding accounts, role-based access, event and category management, enrolments and race results. Data is stored in SQL Server with Entity Framework Core Code First.

## User Roles

### Organiser

Organiser's can manage their own events and categories, view enrolments for events they own, update enrolment status and record and view results for their events.

### Participant

Participants can browse events and categories, enrol by selecting a category, view their own enrolments and results and view or update their own profile. Registration accepts only Organiser or Participant. The API checks roles and ownership for restricted operations.

## Part 1

Part 1 contains the planning which includes the ERD, database design and API endpoint plan. They are found in the Docsocs folder. Part 2 implements the API and database using C# models and EF Core migrations.

## Part 2

Part 2 implements the RaceDay REST API. Its main entities are Role, AppUser, Event, Category, Enrolment and Result. Request and response DTOs are in RaceDay.Contracts. Organiser operations verify event ownership; participants can access only their own enrolments and results.

## Technologies Used

- C# and ASP.NET Core Web API (.NET 8)
- Entity Framework Core Code First and SQL Server
- xUnit and Microsoft.AspNetCore.Mvc.Testing for automated API integration tests
- Swagger / Swashbuckle
- GitHub Actions

## Project Structure

RaceDay.Api/                    Solution root
├── RaceDay.sln
├── RaceDay.Api/                 ASP.NET Core API
│   ├── Controllers/             API endpoints and access checks
│   ├── Data/                    DbContext and EF Core migrations
│   ├── Models/                  Database entities
│   └── Services/                Password hashing and related services
├── RaceDay.Contracts/           Request and response DTOs
├── RaceDay.Api.Tests/           Automated API integration tests
├── docs/                        Part 1 planning documents
└── .github/workflows/ci.yml     GitHub Actions workflow
~~~

## Database Setup

### Prerequisites

Install the .NET 8 SDK and SQL Server LocalDB (included with Visual Studio workloads), or use another SQL Server instance. Git is needed to clone the repository.

### Configure and create the database

1. Check ConnectionStrings:DefaultConnection in RaceDay.Api/appsettings.json. The local setup used for this project is SQL Server LocalDB with a database named RaceDayDb. A typical connection string is:

~~~json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=RaceDayDb;Trusted_Connection=True;TrustServerCertificate=True"
}
~~~

Update the connection string if your SQL Server instance differs. Do not commit passwords or other secrets.

2. From the solution root, apply the migrations:

~~~bash
dotnet tool install --global dotnet-ef --version 8.0.11
dotnet ef database update --project RaceDay.Api --startup-project RaceDay.Api
~~~

Skip the tool-install command if dotnet-ef is already installed. In Visual Studio Package Manager Console, use Update-Database -Project RaceDay.Api -StartupProject RaceDay.Api.

3. Confirm that RaceDayDb and its tables appear in SQL Server Object Explorer or SQL Server Management Studio. The migrations under RaceDay.Api/Data/Migrations create the schema from the C# models.

## How to Run the API

From the solution root:

~~~bash
dotnet restore RaceDay.sln
dotnet build RaceDay.sln
dotnet run --project RaceDay.Api
~~~

Open the HTTPS address printed by the application and add /swagger. The development profile used in testing was https://localhost:7043/swagger.

## Authentication

Register using POST /api/authentication/register. Supply a full name, email, password, phone number, date of birth, role and optional username. Passwords are stored as hashes, not plain text.

Log in using POST /api/authentication/login with the registered email and password. A successful login creates a session and returns a cookie; send that cookie with protected requests. POST /api/authentication/logout clears the session. Requests without a valid session are unauthorised; requests made with the wrong role are forbidden.

Example registration body:

~~~json
{
  "fullName": "Demo Organiser",
  "email": "organiser@example.com",
  "password": "Use-A-Strong-Password-123!",
  "phoneNumber": "0712345678",
  "dateOfBirth": "1990-01-01",
  "role": "Organiser",
  "username": "demoorganiser"
}
~~~

## API Endpoints

All operations are documented in Swagger. Replace {id}, {eventId} and {enrolmentId} with IDs returned by earlier requests.

| Method | Route | Access / purpose |
|---|---|---|
| POST | /api/authentication/register | Public: register |
| POST | /api/authentication/login | Public: start a session |
| POST | /api/authentication/logout | Logged-in user: end session |
| GET, PUT | /api/profile/me | Either role: view or update own profile |
| GET | /api/events | Browse events |
| GET | /api/events/{id} | View one event |
| GET | /api/events/mine | Organiser: view own events |
| POST | /api/events | Organiser: create an event |
| PUT, DELETE | /api/events/{id} | Organiser: update or delete own event |
| GET | /api/events/{eventId}/categories | View event categories |
| POST | /api/events/{eventId}/categories | Organiser: add category to own event |
| PUT, DELETE | /api/categories/{id} | Organiser: update or delete own category |
| POST | /api/events/{eventId}/enrolments | Participant: enrol using a category |
| GET | /api/users/enrolments | Participant: view own enrolments |
| GET | /api/events/{eventId}/enrolments | Organiser: view enrolments for own event |
| PUT | /api/enrolments/{id}/status | Organiser: change enrolment status |
| POST | /api/enrolments/{enrolmentId}/results | Organiser: record a result for own event |
| GET | /api/user/results | Participant: view own results |
| GET | /api/events/{eventId}/results | Organiser: view results for own event |

## Swagger

Swagger is available at /swagger while the API is running. Expand an operation, choose Try it out, enter request data and choose Execute. Register and log in first, then use the same browser session so the session cookie is sent with protected requests. Use test accounts and sample data.

## Unit Testing

RaceDay.Api.Tests contains automated API integration tests. They use WebApplicationFactory and an EF Core in-memory database to exercise API behavior such as registration/login, invalid credentials, unauthenticated access, role rejection, event creation and participant enrolment. These are integration tests rather than isolated unit tests.

Run from the solution root:

~~~bash
dotnet test RaceDay.sln --configuration Release
~~~

Check the result in the terminal or Visual Studio Test Explorer before submission.

## CI/CD

The workflow at .github/workflows/ci.yml is configured to restore, build and test the solution with .NET 8 on pushes and pull requests. Check the Actions tab in your GitHub repository for the actual result; claim a passing build only after confirming it there.

## GitHub Actions Screenshot

After a successful workflow run, add a genuine screenshot at docs/ci-green-build.png. The following Markdown displays it:

~~~markdown
![Successful GitHub Actions build](docs/ci-green-build.png)
~~~

This section does not claim a green build until you add your own screenshot.

## Video Presentation

**Unlisted YouTube video:** _Add your unlisted video link here after uploading the recording._

The video should introduce RaceDay and Part 2; show the repository structure, models, DbContext and SQL database; demonstrate registration and password hashing, login/session/logout, organiser and participant features, a rejected wrong-role request and Swagger; show passing tests and the GitHub Actions result; and briefly explain how Part 2 prepares the project for Part 3.

