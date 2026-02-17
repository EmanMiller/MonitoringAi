# CARD 5: E2E Testing for Simulate Chatbox Experience

**Owner:** @Becca (Testing)  
**Source:** TASK_BREAKDOWN_SIMULATE_CHATBOX_EXPERIENCE.md (lines 180–205)

---

## Overview

User types natural language in chat → system matches against mock query library (login, checkout, email, slow page loads) → returns matched query + explanation in chat, or a friendly no-match message.

**Backend:** `POST /api/Chat/match-query` with `{ userInput }`.  
**Frontend:** Chat tries match-query first for normal messages (≥3 chars); if matched, shows query + explanation; else falls back to general chat.

---

## 1. Happy path

| # | User types | Expected | Pass |
|---|------------|----------|------|
| 1 | "I need a query to see logins for the past 3 hours" | **Login tracking** query + explanation; query in code block | [ ] |
| 2 | "show me checkout failures" | **Checkout failures** query + explanation | [ ] |
| 3 | "email delivery issues" | **Email delivery** query + explanation | [ ] |
| 4 | "slow page loads" | **Slow page loads** query + explanation | [ ] |

**Check:** Reply shows explanation text and a code block with the Sumo Logic query. Category should match the mock library (Login tracking, Checkout failures, Email delivery, Slow page loads).

---

## 2. Edge cases

| # | User types | Expected | Pass |
|---|------------|----------|------|
| 5 | "help me find auth events" (paraphrasing) | **Login** query (auth/logins) | [ ] |
| 6 | "I need a query for UFO sightings" | Friendly **no-match** message (e.g. "No matching query found...") | [ ] |
| 7 | *(empty send)* | No API call; input required / button disabled | [ ] |
| 8 | "ab" (very short) | Either validation error or general chat reply (no crash) | [ ] |

---

## 3. Regression

| # | Check | Pass |
|---|--------|------|
| 9 | **Normal chat** (non-query): e.g. "What is Sumo Logic?" → Gemini reply (no match-query result) | [ ] |
| 10 | **Dashboard flow:** "I want to create a dashboard" → step-by-step flow unchanged | [ ] |
| 11 | **Query Builder:** Navigate to Query Builder → page works, unchanged | [ ] |

---

## Acceptance criteria (CARD 5)

- [ ] All 4 query categories match for appropriate natural language (happy path 1–4)
- [ ] No match handled gracefully (edge case 6)
- [ ] Empty / very short input handled (edge cases 7–8)
- [ ] No regressions in existing chat, dashboard flow, or Query Builder (regression 9–11)

---

## Notes

- Mock library: `DashboardApi/Data/MockQueryLibrary.cs` (login-tracking, checkout-failures, email-delivery, slow-page-loads).
- Chat flow: `ChatWindow` calls `matchQuery(input)` first for non-dashboard messages; if `matched: true`, displays query + explanation; if `matched: false`, displays `message`; otherwise falls back to `postChat`.
- Backend validates `userInput` (required, max 500 chars, no script tags) and rate-limits via `ChatRateLimitService`.
