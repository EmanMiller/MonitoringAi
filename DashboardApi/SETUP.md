# DashboardApi Setup

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
- **Content** — Stored content is sanitized and truncated to 4096 characters per message.
- **ConversationId** — Optional. Requests may include `conversationId` (Guid) to group messages; if omitted, the backend generates a new ID per exchange.
