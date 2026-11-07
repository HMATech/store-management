# store-management-be-aspnc
A backend for Store Management app using ASP.NET Core

## Run with Docker

1. Copy `.env.example` to `.env` and fill it in. At minimum, set `POSTGRES_PASSWORD`, `JWT_SECRET`, `INITIAL_USER_EMAIL` and `INITIAL_USER_PASSWORD`.
2. Run `docker compose up -d --build`.
3. Log in with `POST http://localhost:8080/users/login` using the initial user, and send the returned `jwt` as a bearer token on other requests.

The database is kept in the `db-data` volume, and migrations are applied when the API starts. Put a reverse proxy with TLS in front of port 8080 before exposing it to the internet.
