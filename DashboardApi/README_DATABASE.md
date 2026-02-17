# DashboardApi – Database setup

The API uses **Entity Framework Core** with either **SQLite** (local/dev) or **PostgreSQL** (production/cloud).

## Connection

- **Default (SQLite):** `ConnectionStrings:DefaultConnection` = `Data Source=monitoring.db` — no server required.
- **PostgreSQL:** Set `ConnectionStrings:DefaultConnection` to a Postgres connection string, e.g.  
  `Host=localhost;Database=monitoringai;Username=postgres;Password=your_password;Include Error Detail=true`  
  Or set **DATABASE_URL** (e.g. from env/Secrets Manager):  
  `postgresql://user:password@host:5432/dbname`

The app detects PostgreSQL when the connection string contains `Host=` or `Database=` (or starts with `postgresql://`).

## Schema (ApplicationDbContext)

| Table         | Purpose |
|--------------|---------|
| Users        | Id (Guid), Username, Email, PasswordHash, CreatedAt, Role, RefreshToken, lockout |
| Dashboards   | Id (Guid), Name (50), UserId, Configuration (JSON), CreatedAt, UpdatedAt |
| Queries      | Id (Guid), Category, Key (200), Value (text), Tags (JSON), CreatedBy, CreatedAt |
| Activities   | Id (Guid), Type, Description, UserId (nullable), Timestamp, Metadata (JSON) |
| ChatHistory  | Id (Guid), UserId, ConversationId, Role, Content (text), Timestamp |
| LogMappings  | Existing: Category, Key, Value (log mapping for query assistant) |
| SavedQueries | Existing: Name, QueryText, Category, Tags, UsageCount |

## First-time / local (SQLite)

If you see `no such table` or want a clean DB:

```bash
rm -f MonitoringAi/DashboardApi/monitoring.db
cd MonitoringAi/DashboardApi
dotnet run
```

On first run, if there are **no pending migrations**, the app calls `EnsureCreatedAsync()` and creates all tables. Seed data (sample activities) and optional admin user are applied when the `Users` table is empty.

## Migrations (recommended for production)

Install the EF Core tools (once):

```bash
dotnet tool install --global dotnet-ef
```

Create and apply migrations:

```bash
cd MonitoringAi/DashboardApi
dotnet ef migrations add InitialCreate --context ApplicationDbContext
dotnet ef database update
```

After that, startup uses **MigrateAsync()** so each run applies any pending migrations.

## Optional: seed admin user

In `appsettings.Development.json` (or environment):

```json
"Seed": { "DefaultAdminPassword": "YourSecurePassword" }
```

If the `Users` table is empty on startup, an `admin` user is created with that password (Username: `admin`, Email: `admin@local`).

## Security (for @MonitoringAi/.cursor/rules/paul-security.mdc)

- Connection string: from **Configuration** (appsettings or env); use **Secrets Manager** in production (e.g. AWS Secrets Manager / GCP Secret Manager).
- Do **not** hardcode credentials.
- Prefer **SSL/TLS** for Postgres in production; disable **public access** to the DB (VPC/firewall).
- Use IAM roles (AWS) or service accounts (GCP) where possible; principle of least privilege for DB users.

## For @MonitoringAi/.cursor/rules/gary-backend-developer.mdc

Database is configured; connection string is in **appsettings.json** (and optional **Postgres** entry). Services use **ApplicationDbContext**. To apply migrations, run:

```bash
dotnet ef database update
```

(from the DashboardApi directory, with `dotnet-ef` tool installed).
