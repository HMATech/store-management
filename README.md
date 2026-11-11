# Store Management

A store management app. The ASP.NET Core API is in `backend/` and the React app in `frontend/`.

## Run with Docker

From the repo root:

1. Copy `.env.example` to `.env` and fill it in. At minimum, set `POSTGRES_PASSWORD`, `JWT_SECRET`, `INITIAL_USER_EMAIL` and `INITIAL_USER_PASSWORD`.
2. Run `docker compose up -d --build`.
3. Log in with `POST http://localhost:8080/users/login` using the initial user, and send the returned `jwt` as a bearer token on other requests.

The database is kept in the `db-data` volume, and migrations are applied when the API starts. Put a reverse proxy with TLS in front of port 8080 before exposing it to the internet.

## Develop the backend

From `backend/`, run `dotnet build Backend.sln` and `dotnet test Backend.sln`. See [AGENTS.md](AGENTS.md) for the full commands and conventions.

## Develop the frontend

From `frontend/`, run `npm install`, then `npm run dev` (http://localhost:3000) and `npm test`. The backend must be running for the app to sign in.
