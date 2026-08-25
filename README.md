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
- Full CRUD management of problems, competitions, years, groups, and user accounts
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

## Project Structure (high level)

```
CodeXpert/
├── Models/              # EF Core entity classes (Problem, Submission, Competition, ...)
├── Data/                # DbContext, Identity context, migrations
├── Managers/            # Service-layer classes wrapping Context CRUD operations
├── Services/            # Judge0Service, EmailSender, CustomIdentityErrorDescriber
├── Controllers/         # MVC controllers (Problems, Submissions, Competitions, ...)
├── Views/               # Razor views + shared layout
├── wwwroot/             # Static assets (CSS, JS, images) + problem statements/test cases
└── Program.cs           # App configuration & DI setup
```

Test cases and problem statements are organized hierarchically on disk:
```
TestCases/{Competition}/{Year}/{ProblemNotation}/Input|Output
```

---

## Getting Started

### Prerequisites
- .NET 8 SDK
- Microsoft SQL Server (local or remote instance)
- A [Judge0 API](https://judge0.com/) key (the hosted/shared instance is used by default; self-hosting is also supported — see below)
- SMTP credentials for outgoing email (registration & password reset)

### Setup
1. Clone the repository
   ```bash
   git clone https://github.com/vivo2006/CodeXpert.git
   cd CodeXpert
   ```
2. Configure `appsettings.json` with your:
   - SQL Server connection string
   - Judge0 API key/host
   - SMTP email settings
3. Apply database migrations
   ```bash
   dotnet ef database update
   ```
4. Run the seed console project once to create the `Admin`/`User` roles and an initial admin account.
5. Start the application
   ```bash
   dotnet run
   ```

### Self-hosting Judge0 (optional)
The free/shared Judge0 instance limits submissions (e.g. ~50/day, 500MB memory cap, standard I/O only). To lift these limits, Judge0 can be self-hosted via Docker:
```bash
git clone https://github.com/judge0/judge0.git
cd judge0
# configure docker-compose.yml (port, DB, cache settings)
docker-compose up -d
```
See Judge0's own documentation for full deployment details.

---

## Known Limitations

- Relies on the shared Judge0 API tier by default: max 50 submissions/day, 500 MB memory limit, standard input/output problems only
- Only Bulgarian national competition and olympiad problems are in scope (not a general-purpose judge like Codeforces)
- UI is currently Bulgarian-only

---

## License

© Velislav Borisov, 2025. All rights reserved. This project and its contents are protected under applicable copyright law. Use, copying, or redistribution of any part of this project without prior permission from the author is prohibited.

This project uses the [Judge0 API](https://judge0.com/) for code execution and testing. All rights to Judge0 belong to its creators and are subject to their own terms of use.

---

## Author

**Velislav Borisov**
[GitHub](https://github.com/vivo2006) · [LinkedIn](https://linkedin.com/in/velislavborisov)
