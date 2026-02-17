# PII Redaction (CARD 2)

User content is redacted before being sent to the LLM (Gemini) and before being stored in ChatHistory. This keeps PII out of third-party APIs and the database.

## Where redaction is applied

- **Chat** (`POST /api/chat`): message and history turns are redacted before `SendChatAsync`; same redacted content is stored in ChatHistory.
- **Match query** (`POST /api/chat/match-query`): `userInput` is redacted before calling Gemini.
- **Generate / optimize / explain query** (`POST /api/chat/generate-query`, `optimize-query`, `explain-query`): user input or query text is redacted before calling the AI.
- **Dashboard flow** (`POST /api/chat/dashboard-flow`): message and history item text are redacted before `ProcessAsync` and before logging.
- **Query assistant** (e.g. `POST /api/query` if it calls Gemini): message is redacted before `GetSumoQueryAsync`.

All ChatHistory rows (user and assistant) are stored only after redaction (see `TryLogChatExchangeAsync`).

## PII patterns redacted

| Pattern        | Example                    | Placeholder        |
|----------------|----------------------------|--------------------|
| Email          | `user@example.com`         | `[EMAIL_REDACTED]` |
| Phone (US-style)| `555-123-4567`, `(555) 123-4567` | `[PHONE_REDACTED]` |
| SSN            | `123-45-6789`              | `[SSN_REDACTED]`   |
| Credit card    | `1234 5678 9012 3456`      | `[CARD_REDACTED]`  |
| IPv4           | `192.168.1.1`              | `[IP_REDACTED]`    |
| Employee ID    | `E-12345`, `e-99999`       | `[ID_REDACTED]`    |
| User ID        | `user_abc123`, `user_xyz`  | `[ID_REDACTED]`    |

Implementation: `InputValidationService.StripPii(string?)` in `DashboardApi/Services/InputValidationService.cs`. Redaction runs **after** sanitization (e.g. `SanitizeChatMessage`). No PII is sent to Gemini; ChatHistory stores only redacted content.
