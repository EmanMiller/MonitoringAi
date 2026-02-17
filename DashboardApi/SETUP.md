# DashboardApi Setup

## JWT Secret (required for production)

The JWT signing secret is used for authentication tokens. **Never deploy to production with the placeholder value.**

### Development

`appsettings.json` and `appsettings.Development.json` contain placeholder values (dev-only). These are acceptable for local development.

### Production

**You must set the JWT secret via environment variable or Secrets Manager.** The app will refuse to start in Production if the secret is missing or is a known placeholder.

**Environment variables** (either works):

- `JWT_SECRET` — recommended
- `Jwt__Secret` — alternative (ASP.NET config binding)

**Requirements:**

- Minimum 32 characters
- Use a cryptographically random value (e.g. `openssl rand -base64 48`)
- Store in AWS Secrets Manager, GCP Secret Manager, or equivalent — never commit real secrets

**Example (Docker):**

```bash
docker run -e JWT_SECRET="your-strong-secret-min-32-chars" ...
```

**Example (Kubernetes secret):**

```yaml
env:
  - name: JWT_SECRET
    valueFrom:
      secretKeyRef:
        name: monitoringai-secrets
        key: jwt-secret
```

---

## Gemini API Key (required for chat features)

The following features require a valid **Google Gemini API key**:

- Chat (`POST /api/Chat`)
- Generate Query (`POST /api/Chat/generate-query`)
- Optimize Query (`POST /api/Chat/optimize-query`)
- Explain Query (`POST /api/Chat/explain-query`)
- **Dashboard Flow** (`POST /api/Chat/dashboard-flow`) — Create Dashboard in-chat flow
- Match Query (`POST /api/Chat/match-query`)

Without a valid key, these endpoints return **503 Service Unavailable** with a message instructing you to set the key.

### How to configure

1. Get an API key at [Google AI Studio](https://aistudio.google.com/app/apikey).
2. Set it in one of these ways:
   - **Environment:** Copy `DashboardApi/.env.example` to `.env` and replace `EMAN_GOOGLE_API_KEY_HERE` with your key.
   - **Config:** In `appsettings.json` or `appsettings.Development.json`, set `Gemini:ApiKey` to your key.

The placeholder value `EMAN_GOOGLE_API_KEY_HERE` is treated as "not configured" — you must replace it with your actual key for chat features to work.

## Chat History Logging

Chat messages (user + assistant) are persisted to the `ChatHistory` table after each successful exchange for:

- `POST /api/Chat` (regular chat)
- `POST /api/Chat/dashboard-flow`
- `POST /api/Chat/match-query`

**Requirements:**

- **Authenticated users only** — Logging is skipped for anonymous/unauthenticated requests (no valid UserId).
- **Content** — Stored content is sanitized, PII stripped, and truncated to 4096 characters per message.
- **ConversationId** — Optional. Requests may include `conversationId` (Guid) to group messages; if omitted, the backend generates a new ID per exchange.
