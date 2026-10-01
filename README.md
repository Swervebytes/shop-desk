# Shop Desk

Shop Desk is a job tracker for a small software shop. Sign in, keep a client list, add a job, move it from new to done, and find it again with search.

## Live

https://adam-variety-integrity-calculations.trycloudflare.com

Demo account:

- Email: `demo@shopdesk.dev`
- Password: `demo-shop-desk`

The React app and the API are the same site. That address is a Cloudflare tunnel to the Shop Desk process running with Postgres. It stays up while that process is running, and a new tunnel would get a new address.

## What this repo is

- React front end (Vite, TypeScript) in `web/`
- ASP.NET Core 9 API (C#) in `api/`
- PostgreSQL, started with Docker Compose
- One seeded user. The password is hashed with ASP.NET Core's PBKDF2 password hasher. The API returns a JWT after login, and the other routes require it.
- The API creates the schema on startup and seeds three clients and six jobs when the database is empty.

Job status values are `new`, `in_progress`, `review`, and `done`.

## Run it locally

You need the .NET 9 SDK, Node.js 22, and Docker.

Start Postgres:

```powershell
docker compose up -d db
```

Development, with the Vite dev server proxying `/api` to the API:

```powershell
dotnet run --project api/src/ShopDesk.Api
```

```powershell
cd web
npm ci
npm run dev
```

Open http://localhost:5173 and sign in with the demo account. The Development configuration uses a local JWT key from `appsettings.Development.json`.

One process, which is how the live site is served:

```powershell
cd web
npm ci
npm run build
cd ..
$env:ASPNETCORE_ENVIRONMENT = "Production"
$env:Jwt__Key = "replace-this-with-at-least-32-characters"
dotnet run --project api/src/ShopDesk.Api --urls http://localhost:8080 --no-launch-profile
```

Open http://localhost:8080. Outside Development the API refuses to start unless `Jwt__Key` is set to at least 32 characters.

`docker compose up --build` builds the front end into the API image and serves both on port 8080, with Postgres beside it. The compose file sets its own local JWT key.

## Tests

```powershell
dotnet test api/ShopDesk.sln
```

The API tests start PostgreSQL in Docker and cover login, the password hash, auth on the job routes, the seed data, adding a client, and creating a job, marking it done, and finding it with search.

## Data

Clients have an id, name, and contact. Jobs have an id, client id, title, status, due date, and notes.
