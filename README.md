# CodeXpert

CodeXpert is a full-stack **online judge for competitive programming**, built with C#, ASP.NET Core MVC and SQL Server. Users browse a competition archive, submit solutions in five languages, inspect judging results and track their progress on leaderboards.

Originally developed as a diploma project at the High School of Mathematics "Akad. Kiril Popov", Plovdiv (2025), and subsequently improved with English content, safer submission handling and automated regression tests.

[![Build and test](https://github.com/vivo2006/OnlineJudgeSystemMVC/actions/workflows/build.yml/badge.svg)](https://github.com/vivo2006/OnlineJudgeSystemMVC/actions/workflows/build.yml)

## Demo

A 2-minute walkthrough of problem browsing, Python and C++ submissions, judging results, rankings and submission history:

https://github.com/user-attachments/assets/30323b3a-2289-43c3-bb87-880060c31388

## Features

- **Competition archive:** browse tournaments by year and age group without signing in. The interface and bundled archive statements are in English, with links to the original Bulgarian PDFs.
- **Five submission languages:** C++, Java, Python, JavaScript and C#.
- **Background judging:** Hangfire queues submissions; Judge0 evaluates them against private test cases with time and memory limits.
- **Saved results:** inspect per-test verdicts, runtime and memory, and reopen submission history without another judging request.
- **Leaderboards:** global and age-group rankings use each user's best score per problem.
- **Daily challenge:** earn a 10-point bonus once per UTC day by improving the best accepted runtime or memory usage on the selected problem.
- **Accounts and administration:** Identity authentication, profile management, email flows, and role-protected management of competitions, problems, statements, test files and users.

## Technology and structure

| Area | Technologies |
| --- | --- |
| Web application | C#, ASP.NET Core 8 MVC, Razor Pages, ASP.NET Core Identity |
| Data | Entity Framework Core, Microsoft SQL Server |
| Background work and judging | Hangfire, Judge0 CE through RapidAPI |
| Frontend | Bootstrap, JavaScript, jQuery and AJAX |
| Email | .NET SMTP client (`System.Net.Mail`) |
| Verification | NUnit, Moq, SQLite/InMemory test databases, GitHub Actions |

```text
OnlineJudgeSystemMVC/   Controllers, Razor views/Identity pages, background processor and static statements
BusinessLayer/          Domain entities, Judge0 DTOs and configuration models
DataLayer/              EF Core contexts, database access and SQL Server migrations
ServiceLayer/           Managers, Judge0 client, scoring, email and file-storage helpers
SeedingLayer/           Console application for roles, an optional admin and sample data
TestingLayer/           Regression tests for data access, judging, authorization and English content
ValidationLayer/        Custom validation attributes
```

Problem statements are served from `OnlineJudgeSystemMVC/wwwroot`. Judge input/output files live outside the public web root:

```text
OnlineJudgeSystemMVC/TestCases/{Competition}/{Year}/{ProblemNotation}/Input|Output
```

## Technical decisions

- **Keep judging out of web requests.** A submission is saved and queued before the worker contacts Judge0. Its lifecycle is persisted as Queued, Running, Completed, Incomplete or Failed.
- **Conserve the free API allowance.** Each job makes at most one batch POST and one batch GET, with a configurable delay (10 seconds by default). There is no automatic polling or API retry. Refreshing or reopening a result page only reads saved data.
- **Make the tradeoff explicit.** If Judge0 has not finished at the single check, the submission is marked Incomplete and receives no score. The delay starts after the batch POST; queueing time can make the total wait longer than 10 seconds.
- **Protect submissions and scoring.** Users can read their own source code and detailed results. Repeated accepted submissions do not inflate the best-per-problem score. Regression tests cover ownership, upload paths, API failures, quota behavior and scoring.

### Default application limits

These are configurable under `Judging` in `appsettings.json` or through environment variables:

| Setting | Default |
| --- | --- |
| Result check delay | 10 seconds |
| Maximum test cases per submission | 20 |
| Submission attempts per user per UTC day | 10 |
| Submission attempts across all users per UTC day | 10 |
| Cooldown between a user's attempts | 60 seconds |

These limits count application submission attempts. The provider may count individual test executions differently; use limits appropriate to your actual RapidAPI subscription. Provider quotas and resource limits depend on that subscription.

## Run locally

The following commands use **PowerShell from the repository root**. They describe a fresh local installation; back up an existing database before applying migrations or running the seeder.

### 1. Prerequisites and clone

- .NET 8 SDK.
- SQL Server, including Express or LocalDB on Windows, with permission to create/update the application database.
- A Judge0 CE RapidAPI subscription/key for live submissions.
- SMTP credentials for registration confirmation and password-reset email. Browsing and signing in as the seeded, confirmed admin do not require SMTP.

```powershell
git clone https://github.com/vivo2006/OnlineJudgeSystemMVC.git
cd OnlineJudgeSystemMVC
dotnet restore OnlineJudgeSystemMVC.sln
```

### 2. Create local configuration

Create `OnlineJudgeSystemMVC/appsettings.Local.json`. It is ignored by Git and excluded from publish output. The web app loads it in Development; the seeder also reads it. Environment variables override its values.

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=CodeXpert;Trusted_Connection=True;TrustServerCertificate=True;"
  },
  "APIdata": {
    "Key": "",
    "Host": "judge0-ce.p.rapidapi.com"
  },
  "EmailSettings": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": 587,
    "SmtpUsername": "",
    "SmtpPassword": "",
    "EnableSsl": true
  },
  "SeedAdmin": {
    "Username": "admin",
    "Email": "admin@example.com",
    "Password": ""
  },
  "SeedDemoData": true
}
```

Replace the connection string if you use SQL Server Express or another instance. To create a local admin, supply an email and a unique password of at least 12 characters containing uppercase, lowercase, a digit and a non-alphanumeric character. The seeded admin is email-confirmed. Never commit the local configuration or publish admin credentials.

Fill the API and SMTP settings when you need those integrations. Empty API credentials allow browsing, but live judging requires a valid key. New user registration requires working email delivery to confirm the account.

### 3. Apply migrations

Install the EF Core 8 CLI once if it is not already available (`dotnet ef --version`):

```powershell
dotnet tool install --global dotnet-ef --version 8.0.11
```

Set the connection string for the design-time tools so migrations and the application use the same database, then apply migrations:

```powershell
$localSettings = Get-Content ./OnlineJudgeSystemMVC/appsettings.Local.json -Raw | ConvertFrom-Json
$env:ConnectionStrings__DefaultConnection = $localSettings.ConnectionStrings.DefaultConnection
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet ef database update --project DataLayer --startup-project OnlineJudgeSystemMVC --context OnlineJudgeDbContext
```

Wait for the migration command to finish successfully before proceeding.

### 4. Seed roles, admin and sample problem

```powershell
dotnet run --project SeedingLayer
```

The seeder creates the Admin/User roles, optionally creates the configured admin, and rebuilds leaderboard totals from stored best scores. It does not reset an existing admin's password or elevate an existing non-admin account.

With `SeedDemoData` enabled, it adds **Sum two integers** to group E of the competition archive, preferring the Autumn Informatics Tournament in 2011. The sample is identified as a demonstration problem. A fresh database gets this sample; bundled archive PDFs do not automatically populate the rest of the competition database. Administrators can add further problems through the UI.

### 5. Start over HTTPS

```powershell
dotnet dev-certs https --trust
dotnet run --project OnlineJudgeSystemMVC --launch-profile https
```

Open [https://localhost:7197](https://localhost:7197). Sign in with the admin username and password configured in step 2. The Hangfire dashboard at `/hangfire` is restricted to administrators.

For deployment, provide secrets through environment variables or a secret store. For example, `APIdata__Key` and `ConnectionStrings__DefaultConnection` map to the corresponding configuration values.

## Build and test

From the repository root:

```powershell
dotnet build OnlineJudgeSystemMVC.sln --configuration Release
dotnet test TestingLayer/TestingLayer.csproj --no-build --configuration Release
```

GitHub Actions runs restore, build and tests on pushes and pull requests. Judging tests use fake responses and temporary test databases; they do not consume the live Judge0 allowance or require the configured application database.

## Scope and limitations

- The archive focuses on Bulgarian national programming competitions; new uploaded statements retain their supplied language.
- The current Judge0 client targets the hosted RapidAPI endpoint. Self-hosting Judge0 requires adapting the client URL and authentication.
- Only standard input/output problems are supported. There is no interactive judging.
- A single delayed result check deliberately favors predictable API usage over continually updated results. Incomplete submissions are not automatically checked again.

## Author and rights

**Velislav Borisov** - [GitHub](https://github.com/vivo2006) - [LinkedIn](https://linkedin.com/in/velislavborisov)

© Velislav Borisov, 2025. All rights reserved. This project and its contents are protected under applicable copyright law. Use, copying, or redistribution of any part of this project without prior permission from the author is prohibited.

Third-party components and archived problem statements remain subject to their respective authors' rights and terms. Original problem PDFs are linked from the English archive statements, with source credits retained.

