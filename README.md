# Resume and Job Matching API

A RESTful backend for a Resume and Job Matching Platform built with C#, ASP.NET Core, and .NET 10.

## Features

- User registration and login
- JWT bearer authentication
- User profile management
- Resume upload, retrieval, download, and deletion
- Job creation, retrieval, updating, and deletion
- Resume-to-job matching
- SQL Server database integration
- Entity Framework Core migrations
- Automated tests using xUnit

## Technology Stack

- C#
- ASP.NET Core Web API
- .NET 10
- Microsoft SQL Server
- Entity Framework Core
- JWT Bearer Authentication
- PdfPig
- DocumentFormat.OpenXml
- xUnit
- Git and GitHub

## Project Structure

    backend/
    ├── MatchingDebug/
    ├── ResumeJobMatching.Api/
    │   ├── Controllers/
    │   ├── Data/
    │   ├── Migrations/
    │   ├── Models/
    │   ├── Program.cs
    │   └── ResumeJobMatching.Api.csproj
    ├── ResumeJobMatching.Tests/
    └── README.md

## Prerequisites

- .NET 10 SDK
- Microsoft SQL Server
- Git

## Getting Started

### 1. Clone the repository

    git clone https://github.com/SRIRAM240/AI_Resume_And_Job_Matching_Platform-.git

### 2. Navigate to the backend

    cd AI_Resume_And_Job_Matching_Platform-/backend

### 3. Configure application settings

Configure the SQL Server connection string and JWT settings using .NET User Secrets or environment variables.

Do not commit database passwords, JWT signing keys, access tokens, or other sensitive information.

Example commands for local development:

    dotnet user-secrets set "ConnectionStrings:DefaultConnection" "YOUR_CONNECTION_STRING" --project ResumeJobMatching.Api

    dotnet user-secrets set "Jwt:Key" "YOUR_SECRET_SIGNING_KEY" --project ResumeJobMatching.Api

    dotnet user-secrets set "Jwt:Issuer" "YOUR_ISSUER" --project ResumeJobMatching.Api

    dotnet user-secrets set "Jwt:Audience" "YOUR_AUDIENCE" --project ResumeJobMatching.Api

    dotnet user-secrets set "Jwt:ExpiresMinutes" "60" --project ResumeJobMatching.Api

Replace the placeholders with appropriate local values. Ensure the configuration keys match those expected by the application.

### 4. Restore dependencies

    dotnet restore ResumeJobMatching.Api/ResumeJobMatching.Api.csproj

### 5. Apply database migrations

Ensure SQL Server is running and the connection string is configured correctly.

    dotnet ef database update --project ResumeJobMatching.Api

If the Entity Framework CLI is not installed:

    dotnet tool install --global dotnet-ef

### 6. Run the API

    dotnet run --project ResumeJobMatching.Api

Use the local URL displayed in the terminal to send requests to the API.

## API Controllers

| Controller | Responsibility |
|---|---|
| AuthController | User registration and login |
| JobsController | Create, retrieve, update, and delete jobs |
| MatchingController | Resume-to-job matching |
| ProfileController | Retrieve and update the current user's profile |
| ResumesController | Upload, retrieve, download, and delete resumes |

Protected endpoints require a valid JWT bearer token.

Include the token in authenticated requests:

    Authorization: Bearer YOUR_ACCESS_TOKEN

## Running Tests

Run the automated test suite:

    dotnet test ResumeJobMatching.Tests/ResumeJobMatching.Tests.csproj

The test project uses xUnit, ASP.NET Core MVC Testing, and Entity Framework Core InMemory.

## Security Notes

- Keep secrets out of source control.
- Use strong JWT signing keys.
- Never commit private resumes or real personal information.
- Review local test files before publishing them to a public repository.
- Configure appropriate HTTPS and security settings before production deployment.

## Future Improvements

- Expand automated test coverage.
- Improve API documentation.
- Add production logging and monitoring.
- Implement deployment automation.

---

Built with C# and ASP.NET Core.
