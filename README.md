# MonitoringAI

A full-stack monitoring and observability dashboard with AI-powered chat, Confluence integration, and query assistance.

## Overview

MonitoringAI combines a React frontend (Vite) with a .NET 10 API to provide:

- **AI Chat** — Gemini-powered conversation for log queries, dashboards, and guidance
- **Common Q&A** — Search answers grounded in Confluence documentation (RAG planned)
- **Dashboard Wizard** — Create and publish dashboards to Confluence
- **Query Library** — Saved queries, templates, and log mappings (API used by Common Q&A, Query Builder)
- **Auth** — JWT-based login with role-based access (Admin, Viewer, etc.)

## Highlights

- **Stack:** React 19, Vite 7, .NET 10, Entity Framework Core, SQLite/PostgreSQL
- **AI:** Google Gemini for chat and query assistance; PII stripping for safe logging
- **Database:** SQLite (local) or PostgreSQL (production); migrations and seeding
- **Security:** JWT auth, rate limiting, input validation, HTTPS
- **Git:** Dan watcher for auto-commit; coordination with Becca (QA) and Paul (Security)

## Project Structure

```
MonitoringAi/
├── DashboardFrontend/     # React + Vite SPA
│   ├── src/
│   │   ├── components/    # ChatWindow, CommonQAPage, QueryBuilder, etc.
│   │   ├── context/       # AuthContext, DashboardFlowContext
│   │   ├── services/      # api, geminiService
│   │   └── styles/
│   └── package.json
├── DashboardApi/          # .NET Web API
│   ├── Controllers/       # Auth, Chat, Dashboard, Query, SavedQueries, LogMappings
│   ├── Services/          # GeminiChatService, DashboardService, QueryAssistantService
│   ├── Data/              # EF Core DbContext, entities
│   └── Configuration/     # Auth, CORS, Database, Security
├── Tests/                 # NUnit tests (Dashboard, QueryLibrary, Security, Activity)
├── docs/                  # Architecture, Becca checklists, PII redaction
├── task-cards/            # Task breakdowns and coordination (local only)
└── README.md
```

## Prerequisites

- **.NET 10** (or .NET 9)
- **Node.js 18+**
- **Google Gemini API key** (for chat)
- Optional: **PostgreSQL** for production

## Quick Start

### 1. Clone & install

```bash
git clone https://github.com/EmanMiller/MonitoringAi.git
cd MonitoringAi
npm install
cd DashboardFrontend && npm install && cd ..
```

### 2. Configure

Copy `.env.example` to `.env` at the repo root for the Dan watcher (`npm run watch:dan`, `npm run dev:all`). Fill in `GITHUB_USERNAME`, `GITHUB_EMAIL`, and `GITHUB_TOKEN`. Never commit `.env`.

Also copy `.env.example` (or create `.env`) in `DashboardApi/` and `DashboardFrontend/`:

- **GEMINI_API_KEY** — Your Google Gemini API key
- **ConnectionStrings:DefaultConnection** — SQLite (`Data Source=monitoring.db`) or PostgreSQL

### 3. Run API

```bash
cd DashboardApi
dotnet run
```

API runs at `https://localhost:7xxx` (or port in launchSettings).

### 4. Run frontend

```bash
cd DashboardFrontend
npm run dev
```

Frontend runs at `http://localhost:5173`. Set `VITE_API_URL` to your API base URL.

### 5. Build & test

```bash
dotnet build
dotnet test
cd DashboardFrontend && npm run build
```

## Configuration

| Variable | Where | Purpose |
|----------|-------|---------|
| GEMINI_API_KEY | API, Frontend | Google Gemini for chat |
| VITE_API_URL | Frontend .env | API base URL (e.g. `http://localhost:5290`) |
| ConnectionStrings:DefaultConnection | API appsettings | SQLite or PostgreSQL |
| JWT_SECRET or Jwt__Secret | Env (production) | JWT signing key — **required in prod**; appsettings value is dev-only placeholder |

See `DashboardApi/SETUP.md` for JWT secret configuration details.

See `DashboardApi/README_DATABASE.md` for database setup.

## Scripts (root)

| Command | Description |
|---------|-------------|
| `npm run dev` | Start frontend dev server |
| `npm run watch:dan` | Dan Git watcher (auto-commit) |
| `npm run dev:all` | Frontend + Dan concurrently |

## License

Private. See repository settings.

---

**Repository:** [github.com/EmanMiller/MonitoringAi](https://github.com/EmanMiller/MonitoringAi)
