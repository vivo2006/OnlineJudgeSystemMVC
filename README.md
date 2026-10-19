# CodeXpert

CodeXpert is a full-stack **online judge system** for hosting competitive programming problems and automatically evaluating submitted solutions. It was built to serve Bulgarian competitive programmers preparing for national olympiads and tournaments, offering a more modern, feature-rich alternative to existing local platforms.

> 🎓 Originally developed as a diploma project at the High School of Mathematics "Akad. Kiril Popov" — Plovdiv (2025).

---

## Features

**For all visitors**
- Browse the archive of problems from Bulgarian national competitions and olympiads (read-only, no account required)

**For registered users**
- Submit solutions in **C++, Java, JavaScript, Python, and C#**
- Automatic compilation and execution against hidden test cases with time/memory limits
- View submission history, both overall and per-problem
- Global and group-based leaderboards
- **Challenge Me** — a daily feature that lets users retry an old accepted solution for bonus points by improving its runtime or memory usage
- Manage account/profile settings, including data export and account deletion
- Email notifications for registration and password changes

**For administrators**
- Manage problems, competitions, years, groups, and user accounts
- Upload problem statements (PDF) and test case folders directly through the UI

---

## Tech Stack

**Backend**
- C# / ASP.NET Core 8 (MVC)
- Entity Framework Core (Code-First) + Microsoft SQL Server
- ASP.NET Core Identity (authentication, roles, account management)
- [Judge0 API](https://judge0.com/) — sandboxed code compilation & execution
- Hangfire — background job processing for submission handling
- MailKit — transactional email delivery

**Frontend**
- Razor Views (CSHTML)
- Bootstrap 5
- JavaScript + jQuery + AJAX for dynamic, no-reload interactions

**Architecture**
- N-Tier layered architecture: **Presentation → Service → Business → Data Access**
- Also follows the MVC pattern within the presentation layer

---

## Architecture Overview

```
┌─────────────────────┐
│  Presentation Layer  │  ASP.NET MVC (Controllers + Views)
├─────────────────────┤
│    Service Layer      │  Manager classes, EmailSender, Judge0Service
├─────────────────────┤
│    Business Layer      │  Domain models, API DTOs, config classes
├─────────────────────┤
│  Data Access Layer    │  DbContext, Identity, migrations
└─────────────────────┘
              │
              ▼
      Microsoft SQL Server
```

Submitted code is queued as a background job (via Hangfire) rather than processed synchronously, keeping the app responsive under load. Each submission batch is sent to the Judge0 API, which compiles and runs the code inside isolated Docker containers before returning per-test-case results (status, runtime, memory usage).

---

## Project structure

```text
OnlineJudgeSystemMVC/     MVC controllers, Razor/Identity pages, background processor, static statements
BusinessLayer/            Entities and Judge0 DTOs/options
DataLayer/                EF Core contexts and SQL Server migrations
ServiceLayer/             Managers, Judge0 client, scoring and safe storage helpers
SeedingLayer/             Explicit role/admin/demo seeding and leaderboard rebuild
TestingLayer/             NUnit regression tests with fake judge responses and SQLite
```

Private test cases live at `OnlineJudgeSystemMVC/TestCases/{Competition}/{Year}/{ProblemNotation}/Input|Output`, outside `wwwroot`. Input and output stems must match. The configured cap defaults to 20 tests; oversized or mismatched sets are rejected rather than partly judged. Historical data may need correcting before a problem can be judged.

## Getting started

Requirements: .NET 8 SDK, SQL Server (Windows LocalDB is suitable for local development), a Judge0 RapidAPI subscription for real submissions, and SMTP for registration/password-reset email. Tests do not need SQL Server, SMTP or Judge0 credentials.

```powershell
git clone https://github.com/vivo2006/OnlineJudgeSystemMVC.git
cd OnlineJudgeSystemMVC
dotnet restore
dotnet build
dotnet test
```

Configure `OnlineJudgeSystemMVC/appsettings.Local.json` for development. This file is ignored by Git and loaded only in Development by the web host. Copy the structure from `appsettings.json`, then set `ConnectionStrings:DefaultConnection`, `APIdata:Key`, and the SMTP settings. Never put real credentials into tracked configuration. Production uses environment variables or a deployment secret store; for example `ConnectionStrings__DefaultConnection`, `APIdata__Key`, and `EmailSettings__SmtpPassword`. Environment variables override the local file.

Install the EF CLI if needed, and apply migrations explicitly:

```powershell
dotnet tool install --global dotnet-ef --version 8.0.11
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet ef database update --project DataLayer --startup-project OnlineJudgeSystemMVC
```

For an existing database, back it up first. The `SecurityAndJudging` migration adds submission state and quota/scoring metadata. It does not send code to Judge0. Previously queued Hangfire jobs target the old controller method and must be reviewed/removed before starting the upgraded worker; they are not automatically resubmitted.

The seed console reads the same local configuration plus environment variables. It creates roles, optionally creates an administrator, and rebuilds leaderboard totals from best completed per-problem scores. Existing non-admin accounts are never silently promoted. Set these values privately in your local configuration or shell:

```text
SeedAdmin__Username
SeedAdmin__Email
SeedAdmin__Password
```

Use a unique password with at least 12 characters, a digit, uppercase/lowercase letters and a symbol. Do not commit it. Set `SeedDemoData=true` to add the original English sample problem.

```powershell
$env:SeedDemoData = "true"
dotnet run --project SeedingLayer
dotnet run --project OnlineJudgeSystemMVC --launch-profile https
```

Use the HTTPS address printed by the application. Sign in with the locally configured administrator or register/confirm a normal account. Open the DEMO competition, select “Sum two integers”, submit a solution, and refresh its results page. Page refreshes read your database and do not call Judge0.

## API budget and scoring

- A valid submission makes at most one Judge0 batch POST and one batch GET, after a **10-second delay** by default. There is no automatic polling or API resubmission retry.
- If Judge0 still reports queued/processing at that check, the submission becomes `Incomplete`, without a score. This deliberately trades automatic recovery for a fixed request budget. Increasing `Judging:ResultDelaySeconds` changes the wait without increasing the request count. Refreshing the page cannot recover provider results.
- Default application limits are 10 submissions per UTC day globally and per user, with a 60-second per-user cooldown. Configure `MaxGlobalSubmissionsPerDay`, `MaxSubmissionsPerDay`, `SubmissionCooldownSeconds`, and `MaxTestCases` under `Judging` for your subscription. These limits count attempts; the provider may account for batch test executions separately, so check your own subscription's allowance.
- The leaderboard counts the best score per user/problem. Repeating an accepted solution does not repeatedly add 100 points. A fully accepted daily challenge can earn a single 10-point bonus per UTC day by improving the best prior accepted runtime or memory usage.
- Job redelivery does not resubmit a row that has already left `Queued`. A worker interruption can leave a row `Running`; automatic recovery is intentionally disabled to avoid an ambiguous duplicate API submission.

## Security and validation

Administrator pages and the Hangfire dashboard require the Admin role. Mutating MVC requests require antiforgery tokens. Submission identity and storage paths are derived on the server, and private submission code/results are visible only to their owner. Test uploads validate filenames, size, pairs and storage identifiers before replacing an existing test set.

The committed configuration contains no real API or SMTP credentials. Credentials previously published in Git history must still be revoked/rotated through their providers; removing them from the latest files is insufficient. See [SECURITY.md](SECURITY.md).

## Known limitations

- The main navigation, sign-in/register screens and submission journey are in English; some administration/account pages and the historical contest PDFs remain Bulgarian.
- Problem notation is unique within each competition year, so the same notation can be reused in another year. The migration checks conflicting legacy data before creating the matching database constraint.
- The Judge0 client currently targets the RapidAPI endpoint. Running your own Judge0 instance requires adapting endpoint/authentication configuration.
- SQL Server upgrade scripts are generated in tests, but applying them to your existing database is an explicit operator step. Integration tests use a disposable SQLite database and fake judge responses.
- There are existing compiler/nullability warnings elsewhere in the school project. No throughput or production-security certification is claimed.

---

## License

© Velislav Borisov, 2025. All rights reserved. This project and its contents are protected under applicable copyright law. Use, copying, or redistribution of any part of this project without prior permission from the author is prohibited.

This project uses the [Judge0 API](https://judge0.com/) for code execution and testing. All rights to Judge0 belong to its creators and are subject to their own terms of use.

---

## Author

**Velislav Borisov**
[GitHub](https://github.com/vivo2006) · [LinkedIn](https://linkedin.com/in/velislavborisov)
