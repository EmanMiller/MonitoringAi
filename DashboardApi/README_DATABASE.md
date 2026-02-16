# DashboardApi – SQLite database (local)

The API uses **SQLite** (file-based). There is **no separate database server** to start.

## First-time / "no such table: Users" fix

If you see `SQLite Error 1: no such table: Users`, the database file exists but has no (or old) tables. Remove it and start the API again so it can create a fresh DB with the correct schema.

From the repo root:

```bash
# Remove the SQLite file (if it exists)
rm -f MonitoringAi/DashboardApi/monitoring.db

# Start the API (creates monitoring.db and all tables on first run)
cd MonitoringAi/DashboardApi
dotnet run
```

On Windows (PowerShell):

```powershell
Remove-Item -Force MonitoringAi\DashboardApi\monitoring.db -ErrorAction SilentlyContinue
cd MonitoringAi\DashboardApi
dotnet run
```

The first time the app runs after deleting the file, `EnsureCreated()` will create `monitoring.db` and the `Users`, `LogMappings`, `SavedQueries`, and `QueryLibraryItem` tables.

## Optional: seed admin user

To create a default admin user, set in `appsettings.Development.json` (or environment):

```json
"Seed": { "DefaultAdminPassword": "YourSecurePassword" }
```

Then restart the API. If the `Users` table is empty, it will create an `admin` user with that password.
