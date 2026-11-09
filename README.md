# RaceDay

## Project Overview

RaceDay is a web application for organising running, walking, and cycling events. Part 1 contains the planning and database design, Part 2 provides the REST API, and Part 3 adds an ASP.NET Core MVC website. The MVC website gets application data from the API; it does not access the database directly.

## User Roles

### Organiser

Organisers create and manage their own events, add event categories, review participant enrolments, update enrolment status, and record race results.

### Participant

Participants browse upcoming events, view event details and categories, enrol in events, and view their own enrolments, results, and profile.

## Solution Structure

```text
RaceDay.sln
RaceDay.Api/                 ASP.NET Core REST API, controllers, models, data and migrations
RaceDay.Contracts/            Shared API request and response DTOs
RaceDay.Mvc/                  ASP.NET Core MVC website, views, client and Dockerfile
RaceDay.Api.Tests/            Automated API integration tests
.github/workflows/            GitHub Actions CI workflow
Docs/                         Part 1 planning and database documents, if included in this checkout
```

### Part 1 - Planning and Database

Part 1 contains the initial planning, ERD, database design, and endpoint plan. Include the submitted Part 1 documents in the repository's planning documents folder.

### Part 2 - RESTful API

The API provides authentication, profile, event, category, enrolment, and result endpoints. Entity Framework Core Code First and SQL Server store application data. Request and response DTOs are in `RaceDay.Contracts`.

### Part 3 - MVC Web Application

The MVC project provides role-aware organiser and participant pages. `RaceDayApiClient` forwards API requests and the API session cookie. The MVC project does not contain a DbContext or connect directly to SQL Server.

## Prerequisites

- .NET 8 SDK and Visual Studio with the ASP.NET and web development workload
- SQL Server LocalDB or another SQL Server instance
- Docker Desktop with its engine running for the container steps
- An Azure Storage account for real banner uploads

## Database Setup

The default local database configuration is in `RaceDay.Api/appsettings.json`. Change `ConnectionStrings:DefaultConnection` if your SQL Server instance differs. From the solution root, apply the EF Core migrations:

```powershell
dotnet tool install --global dotnet-ef --version 8.0.11
dotnet ef database update --project RaceDay.Api --startup-project RaceDay.Api
```

Skip the tool installation if `dotnet-ef` is already installed. In Visual Studio Package Manager Console, set the default project to `RaceDay.Api` and run `Update-Database`.

## How to Run RaceDay.Api

From the solution root:

```powershell
dotnet restore RaceDay.sln
dotnet run --project RaceDay.Api --launch-profile RaceDay.Api
```

The development launch profile uses `https://localhost:7043` and `http://localhost:5043`. Swagger is at `https://localhost:7043/swagger` while the API is running.

## How to Run RaceDay.Mvc Locally

Start the API first and leave it running. In a second terminal, from the solution root, run:

```powershell
dotnet run --project RaceDay.Mvc
```

Open the MVC address printed in the terminal. Use the website's Register and Login pages. `RaceDay.Mvc/appsettings.json` contains the `RaceDayApi:BaseUrl` setting used by the MVC API client; it must point to the running API address.

## Azure Blob Storage Setup

Event banners are sent from the MVC website to the API as multipart form uploads. The API validates JPG, PNG, or WebP images up to 5 MB, uploads them to the `event-banners` path in the `raceday-images` blob container, and stores the resulting URL on the event. The MVC project does not connect to Azure directly.

1. Create an Azure Storage account in Azure Portal.
2. Create a private Blob container named `raceday-images` (or let the API create it if the storage credentials permit).
3. In the storage account's **Security + networking → Access keys**, copy a connection string. Keep it secret.
4. In Visual Studio, right-click **RaceDay.Api** and choose **Manage User Secrets**. Add the Azure settings to the existing JSON, preserving any other settings:

```json
{
  "AzureBlobStorage": {
    "ConnectionString": "YOUR_STORAGE_ACCOUNT_CONNECTION_STRING",
    "ContainerName": "raceday-images"
  }
}
```

5. Restart the API and upload a banner while signed in as the organiser who owns the event.
6. Verify the upload by refreshing the event page and checking the `raceday-images` container for a blob under `event-banners/`.

Never commit the real connection string, add it to `appsettings.json`, or show it in screenshots/video. User Secrets are local to the API project and are not included in the MVC Docker image. For a deployed API, provide the setting through a protected environment variable or managed identity.

## Docker Setup

Docker runs the MVC website. The API and database still need to be available separately. For this project, the container is configured to reach the API running on the Windows host at port 5000.

### Build Image

Run these commands from the solution root, the folder containing `RaceDay.sln`:

```powershell
docker build -f RaceDay.Mvc/Dockerfile -t raceday-mvc:part3 .
```

### Run Container

Start the API on port 5000 and leave it running. If another API process already uses port 5000, use that process rather than starting a second one. Then run the MVC container:

```powershell
docker run -d --name raceday-mvc -p 8080:8080 -e "RaceDayApi__BaseUrl=http://host.docker.internal:5000/" raceday-mvc:part3
```

Open `http://localhost:8080`. To check the container and its logs:

```powershell
docker ps
docker logs --tail 50 raceday-mvc
```

Stop and restart it with `docker stop raceday-mvc` and `docker start raceday-mvc`. To remove it before recreating a container with the same name, run `docker rm -f raceday-mvc`. After code changes, rebuild the image and recreate the container.

## Authentication and Session Flow

Register and sign in through the MVC website. The API validates credentials, stores passwords as PBKDF2 hashes, and creates the server-side session. The MVC application keeps the signed-in user's ID and role in its own session and forwards the API session cookie with protected requests. Navigation and controller filters restrict pages by role. Signing out clears the sessions and returns the user to the public pages.

## Organiser Features

- Dashboard showing the organiser's events, enrolment totals, and upcoming dates
- Create, edit, and delete owned events
- Upload event banners through the API to Azure Blob Storage
- Create and manage event categories
- View enrolments and their selected categories/statuses
- Record participant finish times and positions

## Participant Features

- Browse upcoming events and filter the list
- View event details and available categories
- Enrol in an event using a category
- View personal enrolments, statuses, and results
- View and update profile information

## CI/CD

GitHub Actions is configured in `.github/workflows`. It restores, builds, and runs the automated tests. Check the repository's **Actions** tab for the actual workflow result; only describe a build as passing after verifying a successful run.

## GitHub Actions Screenshot

Add a genuine screenshot of a green Actions run to `docs/ci-green-build.png` if that is the path used in this repository. Then this image reference can display it:

```markdown
![Successful GitHub Actions build](docs/ci-green-build.png)
```

## Video Presentation

**Unlisted YouTube video:** _https://youtu.be/8QI9WuN8_fI?si=UaoqYKZUBkPjhYlT_

The video should demonstrate the running MVC application and API, explain the project structure and role-based workflows, show banner upload and Azure Blob Storage, and demonstrate the Docker image and running container. Also show the verified GitHub Actions result. Do not show passwords, Azure connection strings, or other secrets.
