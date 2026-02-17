# Becca: CARD 4 & CARD 5 — Chat Logging, PII Stripping, Final QA

**Source:** task-cards/TASK_CARDS_CHAT_LOGGING_PII_VECTOR.md (lines 238–296)

---

## CARD 4: Test Chat Logging and PII Stripping

### 1. Chat logging

| # | Check | How to verify | Pass |
|---|--------|----------------|------|
| 1 | **Rows in ChatHistory** | Send a chat message (POST /api/Chat). Query DB: `SELECT * FROM ChatHistory ORDER BY Timestamp DESC LIMIT 2` — expect user + assistant rows with correct UserId, Role, Content, Timestamp | [ ] |
| 2 | **ConversationId links messages** | Same ConversationId on both user and assistant rows for that exchange | [ ] |
| 3 | **Authenticated user** | Log in, send message — UserId in ChatHistory = authenticated user's ID | [ ] |
| 4 | **Anonymous** | Document: if unauthenticated, backend may skip logging or use placeholder UserId (see TryLogChatExchangeAsync) | [ ] |

**Backend:** `ChatController` calls `TryLogChatExchangeAsync` after Post, DashboardFlow, and MatchQuery. Content stored is truncated to 4096 chars (`TruncateForChatHistory`).

### 2. PII stripping

| # | Check | How to verify | Pass |
|---|--------|----------------|------|
| 5 | **Email redacted** | Send message containing e.g. `user@example.com` — LLM receives redacted text; ChatHistory Content has `[EMAIL_REDACTED]`, not raw email | [ ] |
| 6 | **Phone redacted** | Send e.g. `555-123-4567` — stored/LLM sees `[PHONE_REDACTED]` | [ ] |
| 7 | **IDs redacted** | Send e.g. `E-12345` or `user_abc123` — stored/LLM sees `[ID_REDACTED]` | [ ] |
| 8 | **ChatHistory has no raw PII** | Inspect ChatHistory.Content for recent rows — must not contain email, phone, SSN, card, IP, employee ID, user_xxx | [ ] |
| 9 | **Regression** | Send non-PII message — chat still works; response normal | [ ] |

**Backend:** `InputValidationService.StripPii` runs before all Gemini calls and before logging. Patterns: email, phone, SSN, credit card, IP, E-xxxx, user_xxx.

### 3. Vector DB

- Defer until implementation exists (Card 3).

### CARD 4 Acceptance

- [ ] Chat logging verified (rows, ConversationId, UserId, Content, Timestamp)
- [ ] PII stripping verified (unit tests + manual; ChatHistory stores redacted content)
- [ ] No regressions in chat, dashboard-flow, match-query

---

## CARD 5: Final QA — Test All Changes & Sign-Off

**When:** After Cards 1 and 2 (logging + PII) are complete. Re-run when Vector DB (Card 3) is implemented.

### 1. Full regression

| # | Flow | Pass |
|---|------|------|
| 1 | **Chat** — Send messages; verify logging; verify PII redaction in DB | [ ] |
| 2 | **Dashboard flow** — Create Dashboard end-to-end (with API key) | [ ] |
| 3 | **Match-query** — Natural language → matched query + explanation | [ ] |
| 4 | **Common Q&A** — Search, copy, categories | [ ] |

### 2. Chat logging

| # | Check | Pass |
|---|--------|------|
| 1 | ChatHistory rows created with correct UserId, Role, Content, Timestamp, ConversationId | [ ] |
| 2 | No PII in stored Content (redacted only) | [ ] |

### 3. PII stripping

| # | Check | Pass |
|---|--------|------|
| 1 | Messages with email, phone, IDs → redacted in storage and before LLM | [ ] |
| 2 | Chat still works (responses, no errors) | [ ] |

### 4. Backend tests

```bash
cd MonitoringAi/Tests
dotnet test
```

| # | Check | Pass |
|---|--------|------|
| 1 | All tests pass | [ ] |

### 5. Sign-off

- [ ] Update QA report (this doc or main QA_REPORT.md)
- [ ] Confirm ready for @Dan to commit/push — or list blockers below
- [ ] **Blockers:** _________________________________

### CARD 5 Acceptance

- [ ] All flows tested and passing
- [ ] Chat logging and PII stripping verified
- [ ] Backend tests pass
- [ ] QA sign-off complete

---

## Reference (backend)

- **ChatHistory:** `DashboardApi/Data/ChatHistory.cs` — Id, UserId, ConversationId, Role, Content, Timestamp.
- **Logging:** `ChatController.TryLogChatExchangeAsync` — inserts user + assistant rows; skips if userId is default/empty (anonymous).
- **PII:** `InputValidationService.StripPii` — email, phone, SSN, credit card, IP, E-xxxx, user_xxx → placeholders. Used in Post, DashboardFlow, MatchQuery, GenerateQuery, OptimizeQuery, ExplainQuery.
- **Storage:** Content is sanitized/redacted and truncated to 4096 chars before insert.
