# Resume and Job Matching API

A RESTful backend for a Resume and Job Matching Platform built with C#, ASP.NET Core Web API, and .NET 10.

## Features

- User registration and login
- JWT bearer authentication
- User profile management
- Resume upload, retrieval, download, and deletion
- Job creation, retrieval, updating, and deletion
- Resume-to-job matching
- Matching scores, matching skills, and missing skills
- SQL Server database integration
- Entity Framework Core migrations
- Swagger API documentation in Development mode
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
- Swagger / OpenAPI
- xUnit
- Git and GitHub

## Project Structure

    .
    ├── MatchingDebug/
    ├── ResumeJobMatching.Api/
    │   ├── Controllers/
    │   ├── Data/
    │   ├── DTOs/
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

    git clone https://github.com/SRIRAM240/AI_Resume_And_Job_Matching_Platform.git
    cd AI_Resume_And_Job_Matching_Platform

### 2. Configure application settings

The API requires a SQL Server connection string and JWT settings.

Initialize User Secrets if they have not already been initialized:

    dotnet user-secrets init --project ResumeJobMatching.Api

Configure the settings for local development:

    dotnet user-secrets set "ConnectionStrings:DefaultConnection" "YOUR_CONNECTION_STRING" --project ResumeJobMatching.Api
    dotnet user-secrets set "Jwt:Key" "YOUR_SECRET_SIGNING_KEY" --project ResumeJobMatching.Api
    dotnet user-secrets set "Jwt:Issuer" "YOUR_ISSUER" --project ResumeJobMatching.Api
    dotnet user-secrets set "Jwt:Audience" "YOUR_AUDIENCE" --project ResumeJobMatching.Api
    dotnet user-secrets set "Jwt:ExpiresMinutes" "60" --project ResumeJobMatching.Api

Replace all placeholders with valid local values. Use a strong JWT signing key of at least 32 bytes. The issuer and audience must match the application's JWT configuration.

Never commit database passwords, JWT signing keys, access tokens, or other secrets to source control.

### 3. Restore dependencies

    dotnet restore ResumeJobMatching.Api/ResumeJobMatching.Api.csproj

### 4. Apply database migrations

Ensure SQL Server is running and the connection string is configured correctly.

If the Entity Framework Core CLI is not installed:

    dotnet tool install --global dotnet-ef

Apply the migrations:

    dotnet ef database update --project ResumeJobMatching.Api

### 5. Run the API

    dotnet run --project ResumeJobMatching.Api

Use the local URL displayed in the terminal.

### 6. Open Swagger

Swagger UI is enabled in the Development environment. Open `/swagger` on the local API address displayed by the application.

## Authentication

### Register

**POST** `/api/auth/register`

Request body:

    {
      "fullName": "Alex Johnson",
      "email": "alex@example.com",
      "password": "YOUR_PASSWORD"
    }

A successful registration returns `201 Created`.

### Login

**POST** `/api/auth/login`

Request body:

    {
      "email": "alex@example.com",
      "password": "YOUR_PASSWORD"
    }

A successful login returns the user ID, name, email, JWT token, and token expiration time.

For protected endpoints, include the token in the request header:

    Authorization: Bearer YOUR_ACCESS_TOKEN

Registration and login are public. Jobs, resumes, matching, and profile endpoints require authentication.

## API Endpoints

Use the local API address as the base URL.

### Authentication

| Method | Endpoint | Description | Authentication |
|---|---|---|---|
| POST | `/api/auth/register` | Register a user | Public |
| POST | `/api/auth/login` | Log in and receive a JWT | Public |

### Jobs

All job endpoints require a valid JWT.

| Method | Endpoint | Description |
|---|---|---|
| POST | `/api/jobs` | Create a job |
| GET | `/api/jobs` | Retrieve jobs |
| GET | `/api/jobs/{id}` | Retrieve a job by ID |
| PUT | `/api/jobs/{id}` | Update a job |
| DELETE | `/api/jobs/{id}` | Delete a job |

Example create-job request:

    {
      "title": "Software Engineer",
      "companyName": "Example Company",
      "description": "Develop and maintain software applications.",
      "requiredSkills": "C#, ASP.NET Core, SQL Server",
      "location": "Hyderabad",
      "employmentType": "Full-time"
    }

The update endpoint uses the same request fields.

### Resumes

All resume endpoints require a valid JWT.

| Method | Endpoint | Description |
|---|---|---|
| POST | `/api/resumes/upload` | Upload a resume |
| GET | `/api/resumes` | Retrieve the current user's resumes |
| GET | `/api/resumes/{id}` | Retrieve resume details |
| GET | `/api/resumes/{id}/download` | Download a resume |
| DELETE | `/api/resumes/{id}` | Delete a resume |

#### Upload a resume

Send a `multipart/form-data` request to `/api/resumes/upload` with a form field named `File` containing the resume file.

The endpoint has a 6 MB request-size limit.

Example:

    curl -X POST "http://localhost:5175/api/resumes/upload" \
      -H "Authorization: Bearer YOUR_ACCESS_TOKEN" \
      -F "File=@/path/to/your-resume.pdf"

Replace the URL, token, and file path with your local values. Check the application's validation rules for supported file types.

### Resume Matching

All matching endpoints require a valid JWT.

| Method | Endpoint | Description |
|---|---|---|
| POST | `/api/matching` | Match a resume against a specific job |
| GET | `/api/matching/resume/{resumeId}/jobs` | Match a resume against jobs |

Example request to match a resume with a job:

**POST** `/api/matching`

    {
      "resumeId": 1,
      "jobId": 1
    }

The matching response can include:

- Overall match score
- Skill match score
- Description relevance score
- Matching skills
- Missing skills
- Individual skill-match details
- A matching summary

Use valid resume and job IDs accessible to the authenticated user.

### User Profile

All profile endpoints require a valid JWT.

| Method | Endpoint | Description |
|---|---|---|
| GET | `/api/profile/me` | Retrieve the current user's profile |
| PUT | `/api/profile/me` | Update the current user's profile |

Example update request:

    {
      "fullName": "Alex Johnson"
    }

## Running Tests

Run the automated tests from the repository root:

    dotnet test ResumeJobMatching.Tests/ResumeJobMatching.Tests.csproj

The test project uses xUnit, ASP.NET Core MVC Testing, and Entity Framework Core InMemory.

## Common Issues

- **Database connection failure:** Check that SQL Server is running and the connection string is correct.
- **JWT configuration error:** Check the JWT key, issuer, and audience. The signing key must contain at least 32 bytes.
- **401 Unauthorized:** Log in and include the returned JWT in the `Authorization` header.
- **404 Not Found:** Verify that the requested resource exists and is accessible to the authenticated user.
- **Swagger unavailable:** Check that the API is running in the Development environment and use the correct local address.

## Security Notes

- Keep secrets out of source control.
- Use a strong JWT signing key.
- Do not commit private resumes or real personal information.
- Use HTTPS and appropriate security settings in production.
- Review application configuration before deployment.

## Future Improvements

- Expand automated test coverage.
- Improve API documentation.
- Add production logging and monitoring.
- Implement deployment automation.

---

Built with C# and ASP.NET Core.
