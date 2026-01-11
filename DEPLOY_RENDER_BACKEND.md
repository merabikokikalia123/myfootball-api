# Deploy ASP.NET backend to Render (no Postgres)

This backend lives in `WebApplication6/WebApplication6/`.

## Important note about “no Postgres”

If you deploy **without Postgres** on Render Free, your app will use **SQLite inside the container**.
That means data can be lost on redeploy/restart (Render’s free instances don’t guarantee persistent disk).
Your app seeds demo data on startup, so it will still work, but treat it as **demo mode**.

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
