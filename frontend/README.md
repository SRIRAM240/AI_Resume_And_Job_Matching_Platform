# FitCheck frontend

React + TypeScript frontend for the Resume & Job Matching Platform. This version includes a simplified dashboard and an optional Gemini analysis action for each job match.

## Stack

- React 19, TypeScript, Vite
- Material UI
- React Router
- Axios
- Chart.js

## Run locally

1. Extract this folder into your existing repository as `frontend/`.
2. Open a terminal in `frontend/`.
3. Copy `.env.example` to `.env` and confirm the API URL.
4. Run:

   ```powershell
   npm install
   npm run dev
   ```

Set `VITE_API_BASE_URL` to the exact URL printed by `dotnet run`, for example `http://localhost:5175`. Restart Vite after changing `.env`.

## API requirements

The frontend expects the existing API routes for authentication, profiles, resumes, jobs, and matching. Gemini analysis calls:

- `POST /api/ai-analysis`
- JSON body: `{ "resumeId": 1, "jobId": 1 }`
- Requires the logged-in user's JWT, added automatically by the Axios client.

The Gemini branch must have `Gemini:ApiKey` configured through .NET User Secrets. The frontend does not contain or need the Gemini API key.

If the browser reports a CORS error, add a development CORS policy to the API that allows the actual Vite origin, usually `http://localhost:5173`, and call `app.UseCors(...)` before authentication/authorization. The Gemini branch's `Program.cs` should be configured for this if it is not already.

## Design changes

- Dashboard uses compact metrics and one main comparison action.
- Resume and job previews are lists rather than repeated promotional cards.
- Regular matching scores remain distinct from Gemini's written analysis.
- Gemini analysis is requested per job match and shown inline with clear loading/error states.
- The frontend sends only resume/job IDs to the backend; API keys remain server-side.

## Notes

This archive is a frontend source starter, not a deployed application. Run `npm install` and `npm run build` locally to validate against your installed Node.js version and backend configuration.
