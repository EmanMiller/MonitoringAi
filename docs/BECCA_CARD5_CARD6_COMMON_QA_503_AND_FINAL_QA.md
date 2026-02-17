# Becca: CARD 5 & CARD 6 — Common Q&A, Dashboard 503, Final QA

**Source:** TASK_CARDS_COMMON_QA_AND_503.md (lines 135–186)

---

## CARD 5: Test Common Q&A and Dashboard Flow

### 1. Common Q&A

| # | Check | Pass |
|---|--------|------|
| 1 | **All 6 sections appear in order** — Browse Product, Browse Path, Account, Checkout, Gift Registry, API (per `CommonQAPage` CATEGORY_ORDER) | [ ] |
| 2 | **Placeholders when section has no queries** — Selecting/expanding a section with no queries shows a helpful placeholder (e.g. "No queries in this category yet. Use **Request New Query**… or ask CrateBot in the chat.") | [ ] |
| 3 | **Minimal scrolling; compact layout** — Page feels compact; key content visible without excessive scroll; horizontal space used where appropriate | [ ] |
| 4 | **Search** — Search filters results as expected | [ ] |
| 5 | **Copy** — Copy Query copies to clipboard; toast/feedback | [ ] |
| 6 | **View full query** — View Full Query opens modal/detail with full query | [ ] |

### 2. Create Dashboard 503

| # | Scenario | Expected | Pass |
|---|----------|----------|------|
| 7 | **With API key set** | Create Dashboard flow works (step data returned; can complete flow or see expected steps) | [ ] |
| 8 | **Without API key** | Backend returns **503**; frontend shows **"CrateBot unavailable. Check API key."** (or equivalent helpful message) | [ ] |

### CARD 5 Acceptance

- [ ] Common Q&A redesign works as specified (sections, placeholders, layout, search, copy, view full)
- [ ] Dashboard flow: both 503 scenario and success scenario covered

---

## CARD 6: Final QA — Test, Sign-Off & Release Readiness

### 1. Full regression

| # | Flow | Pass |
|---|------|------|
| 1 | **Chat** — Send message; CrateBot reply; status Connected when configured | [ ] |
| 2 | **Create Dashboard** — Start from Sidebar or chat; complete or see steps; 503 when key missing | [ ] |
| 3 | **Common Q&A** — Navigate; 6 sections; search; copy; view full; placeholders; request modal | [ ] |
| 4 | **Crate&Barrel navigation** — Header/site navigation works | [ ] |
| 5 | **Sidebar** — Links/actions work (Dashboard, Create Dashboard, Quick Query, Query Builder, Common Q&A, etc.) | [ ] |
| 6 | **Login / Logout** — Auth flow works; protected routes redirect when not logged in | [ ] |

Confirm no regressions from: Common Q&A redesign, 503 fix, UX simplification.

### 2. Feature validation

| Area | Checks | Pass |
|------|--------|------|
| **Common Q&A** | 6 sections, placeholders, minimal scroll, search, copy, request modal | [ ] |
| **Create Dashboard** | Flow works with API key; clear error when key missing | [ ] |
| **Chat** | Intro messages, CrateBot responses, query matching (if applicable) | [ ] |

### 3. Cross-browser / responsive

| # | Check | Pass |
|---|--------|------|
| 1 | Quick smoke on target browsers (e.g. Chrome, Safari, Firefox) | [ ] |
| 2 | Key viewport sizes (desktop, tablet, mobile) — no broken layout | [ ] |

### 4. Backend tests

```bash
cd MonitoringAi/Tests
dotnet test
```

| # | Check | Pass |
|---|--------|------|
| 1 | All NUnit tests pass | [ ] |
| 2 | Any failing tests fixed or documented as known issues | [ ] |

### 5. Sign-off

- [ ] QA report or checklist updated (this doc or QA_REPORT.md)
- [ ] Ready for release or handoff confirmed — or blockers listed below
- [ ] Blockers for @Dan or team (if any): _________________________________

### CARD 6 Acceptance

- [ ] All critical flows tested and passing
- [ ] No regressions identified
- [ ] Backend tests pass
- [ ] QA sign-off complete (or blockers documented)

---

## Notes

- **CrateBot:** Chat assistant name used in UI (ChatWindow, Message). 503 from dashboard-flow or chat when Gemini key missing → show "CrateBot unavailable. Check API key."
- **Common Q&A sections:** Defined in `CommonQAPage.jsx` as `CATEGORY_ORDER`. Placeholders for empty categories should reference "Request New Query" and "ask CrateBot".
- **API key:** Backend reads `GEMINI_API_KEY` or `Gemini:ApiKey`; frontend never sends the key (backend proxy). Use `.env` or appsettings for local dev.
