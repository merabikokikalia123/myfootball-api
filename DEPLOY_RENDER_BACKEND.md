# Deploy ASP.NET backend to Render (no Postgres)

This backend lives in `WebApplication6/WebApplication6/`.

## Important note about “no Postgres”

If you deploy **without Postgres** on Render Free, your app will use **SQLite inside the container**.
That means data can be lost on redeploy/restart (Render’s free instances don’t guarantee persistent disk).
Your app seeds demo data on startup, so it will still work, but treat it as **demo mode**.

### If you see: SQLite Error 1: 'near "max": syntax error'

That happens when EF tries to apply **SQL Server migrations** against SQLite (SQL Server uses column types like `nvarchar(max)` which SQLite doesn’t understand).
This project is configured to use **SQLite schema creation** (not SQL Server migrations) when `Database__Provider=Sqlite`.

## How to make data persistent

You need one of these:

1. **Persistent disk/volume** attached to the running service

- On Render this is the **Disk** feature (typically not available on the free plan).
- If you have a disk, mount it (example mount path: `/var/data`) and set:
  - `ConnectionStrings__SqliteConnection` = `Data Source=/var/data/webapplication6.db`

2. **External database** (managed DB)

- Easiest free option: **hosted Postgres** (Neon / Supabase have free tiers).

### Recommended (free + persistent): Neon/Supabase Postgres

1. Create a Postgres database (Neon or Supabase).
2. Copy the connection string.
3. In Render → your service → **Environment**, set:
   - `Database__Provider` = `Postgres`
   - `ConnectionStrings__PostgresConnection` = `<your full postgres connection string>`
   - `CORS_ALLOWED_ORIGINS` = `https://myfootball.pages.dev` (plus your custom domain if any)
   - `Jwt__Key` = long random secret (32+ bytes)

Then redeploy.

- If you truly want “no Postgres”, you’d need to switch the backend to a different DB provider (bigger code change).

## Render steps (Docker)

1. Go to Render Dashboard → **New** → **Web Service**.
2. Connect your GitHub repo.
3. Select the repo.
4. Configure:
   - **Runtime**: Docker
   - **Root Directory**: `WebApplication6/WebApplication6`
   - (Render will auto-detect `Dockerfile`)
5. Environment variables (Render → Environment):
   - `CORS_ALLOWED_ORIGINS` = `https://myfootball.pages.dev` (add your custom domain too if you have one)
   - `Jwt__Key` = a long random string (32+ bytes)
   - Optional (explicit): `Database__Provider` = `Sqlite`
   - Optional (SQLite file name): `ConnectionStrings__SqliteConnection` = `Data Source=webapplication6-dev.db`
6. Create service → Deploy.

After deploy, Render gives a URL like:

- `https://your-service.onrender.com`

## Connect Angular (Cloudflare Pages) to Render

Your API routes are prefixed with `/api` (example: `/api/auth/login`).

So set Angular production base URL to:

- `https://your-service.onrender.com/api`

Edit: `gadaketebulimyfootballaplikaica/src/environments/environment.prod.ts`
then redeploy Cloudflare Pages.
