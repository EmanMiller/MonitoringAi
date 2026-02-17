# TASK BREAKDOWN — Landing Page Chat Experience

═══════════════════════════════════════
TASK BREAKDOWN - Landing Page Chat Experience
═══════════════════════════════════════

## OVERVIEW

Improve the landing page chat experience with three goals:
1. Polish chat box formatting and styling
2. Replace the "Create Dashboard" popup modal with an interactive chat flow
3. Build AI backend logic so the AI can guide dashboard creation conversations, collect user inputs via chat, and return the data needed to complete dashboard setup

---

## IMPACT ANALYSIS

| Area | Impact | Details |
|------|--------|---------|
| **Frontend** | High | Chat layout, Message component, ChatInput, new structured message types, quick actions, Sidebar behavior, removal of wizard modal trigger |
| **Backend** | Medium | New or extended Chat endpoint for dashboard-creation context, optional session/flow state, wiring to existing DashboardService |
| **AI** | High | New system prompts, structured output format, multi-turn flow logic for dashboard creation |
| **Database** | Low | Optional: conversation/flow state table if stateless approach insufficient |
| **Security** | Medium | Validate all user inputs in dashboard flow, rate limit, auth checks |

---

## TASK CARDS

---

### @Niklaus (Frontend)

#### CARD 1: Fix Chat Box Formatting on Landing Page

**Components affected:**
- `ChatWindow.jsx` — container, header, message list
- `ChatInput.jsx` / `Chat/ChatInput.jsx` — input area
- `Message.jsx` — message bubbles
- `_chat.css`, `_chat-input.css`

**Tasks:**
1. **Layout & polish**
   - Ensure chat container, message list, and input area are correctly laid out (no overlap, proper spacing)
   - Fix any `position: fixed` vs flex layout issues (e.g., input overlapping messages on scroll)
   - Apply Flock Ramp design: `--gradient-card`, `--color-border-accent`, `--glow-purple`, corner brackets on chat container if appropriate

2. **Message bubbles**
   - Consistent max-width, padding, border-radius for user vs assistant messages
   - Clear distinction between user (right, `--color-user-bubble`) and assistant (left, `--color-assistant-bubble`)
   - Typography: `--font-size-base`, `--line-height-relaxed`, proper line breaks for multi-line text

3. **Input area**
   - Align with design system: input background `--color-bg-input`, focus `--shadow-focus`, send button `--gradient-purple` + `--glow-purple` on hover
   - Ensure placeholder and disabled states are readable
   - Responsive: on narrow viewports (<900px), input should span full width (sidebar collapses)

4. **Header & status**
   - Chat header title and status indicator (Connected/Not Connected) styled consistently
   - Settings button accessible and on-brand

**Acceptance criteria:**
- [ ] Chat looks polished and matches Flock Ramp design
- [ ] No layout bugs (input overlap, truncation, scroll issues)
- [ ] Works on desktop and tablet/mobile breakpoints

---

#### CARD 2: Replace Create Dashboard Popup with Interactive Chat Flow

**Components affected:**
- `Sidebar.jsx` — change "Create Dashboard" from opening modal to initiating chat flow
- `ChatWindow.jsx` — handle "Create Dashboard" intent, render structured messages
- `Message.jsx` — extend to support structured content (options, choices, inline forms)
- `DashboardCreatorWizard.jsx` — repurpose or extract logic; modal no longer primary entry point
- `api.js` — add/use endpoint for dashboard-creation chat context
- New: `ChatQuickActions.jsx` or similar for in-chat quick actions

**Tasks:**

1. **Entry point**
   - When user clicks "Create Dashboard" in Sidebar: inject a system message or user message (e.g., "I want to create a dashboard") and trigger the AI flow, OR show a chat quick action like "📊 Create Dashboard" that the user can click
   - Do **not** open `DashboardCreatorWizard` modal

2. **Structured message types**
   - Extend `Message` component to handle:
     - `type: 'text'` — plain text (current behavior)
     - `type: 'dashboard_step'` — AI response with options/choices (e.g., "What title?", "Choose metrics: A, B, C")
   - Add `ChatMessageOptions` component: render clickable option buttons that send a message when clicked
   - Add `ChatMessageInput` component: render inline text input or short form for step-specific data (e.g., dashboard title)

3. **Chat flow state**
   - Track `dashboardCreationState`: `{ active: boolean, step: number, collected: { title?, panels?, variables? } }`
   - When AI returns a structured step (e.g., "ask_title", "ask_metrics"), render the appropriate UI (input or options)
   - When user submits (text or option click), send to backend and append assistant reply with next step

4. **Backend integration**
   - `postChat` (or new `postDashboardCreationChat`) sends: `{ message, history, flowContext?: { flow: 'dashboard_creation', step, collected } }`
   - AI returns structured response; frontend parses and renders options/inputs
   - When flow completes, call `createDashboardFromWizard` with collected payload

5. **Remove/deprecate wizard modal**
   - Sidebar "Create Dashboard" no longer calls `onStartWizard`
   - Optionally keep `DashboardCreatorWizard` for a "Classic" mode link, or remove entirely

**Acceptance criteria:**
- [ ] User can start "Create Dashboard" from Sidebar or via typing in chat
- [ ] AI guides user through title, metrics, variables step-by-step in chat
- [ ] Options appear as clickable buttons; free-text inputs appear inline
- [ ] Flow completes and dashboard is created via existing API
- [ ] No popup modal for Create Dashboard

---

### @Gary (Backend)

#### CARD 3: Chat Endpoint for Dashboard Creation Flow

**Components affected:**
- `ChatController.cs` — extend `Post` or add `PostDashboardCreation`
- `GeminiChatService.cs` — possibly add method for structured dashboard flow (or George handles via existing `SendChatAsync` with context)
- `DashboardService` / `OnboardingService` — already used for `createDashboardFromWizard`; ensure Chat can invoke it

**Tasks:**

1. **API contract**
   - Option A: Extend `POST /api/Chat` body to accept optional `flowContext`:
     ```json
     {
       "message": "Production API Metrics",
       "history": [...],
       "flowContext": {
         "flow": "dashboard_creation",
         "step": 1,
         "collected": { "title": "...", "panels": {...}, "variables": {...} }
       }
     }
     ```
   - Option B: Add `POST /api/Chat/dashboard-flow` with same structure
   - Response: `{ response: string, structured?: { step, options?, prompt?, payload? } }` when George returns structured output

2. **Orchestration**
   - If George returns structured JSON in the reply, parse it and return `structured` to frontend
   - When George signals "complete" with full payload, call `DashboardService.CreateDashboardFromWizardAsync` (or equivalent) and include dashboard URL in response

3. **Validation**
   - Reuse `InputValidationService.ValidateDashboardName`, `SanitizeDashboardName` for title
   - Validate `panels`, `variables` before passing to DashboardService
   - Rate limit this flow (reuse `ChatRateLimitService`)

4. **Auth**
   - Require authenticated user for dashboard creation; pass userId to DashboardService if needed

**Acceptance criteria:**
- [ ] Chat endpoint accepts flowContext and returns structured step data when applicable
- [ ] Dashboard creation succeeds when AI returns complete payload
- [ ] All inputs validated; errors returned clearly to frontend

---

### @George (AI / Gemini)

#### CARD 4: Dashboard Creation Conversation Logic

**Components affected:**
- `GeminiChatService.cs` — possibly new method or system prompt for dashboard flow
- Prompts / system instructions — new "dashboard creation guide" persona
- Response format — structured output (e.g., JSON block) for steps

**Tasks:**

1. **System prompt**
   - Create a system instruction for the "dashboard creation assistant":
     - Knows the steps: (1) Dashboard title, (2) Metric selection (defaults vs custom), (3) Template variables (timeslice, domain, etc.)
     - Knows the schema: `DashboardWizardRequest` — `DashboardTitle`, `UseDefaults`, `Variables`, `Panels`
     - Metric options: Success Rate %, Error Rate %, Slow Queries, Past 7 day trend (with custom alternatives)

2. **Conversation flow**
   - Step 1: Ask for dashboard title. Validate (uppercase start, 3–50 chars). On invalid, ask again.
   - Step 2: Present metric options (use defaults or choose custom). Collect choices.
   - Step 3: Ask about variables (use defaults or customize: timeslice, domain, domainPrefix, environment).
   - Step 4: Confirm and return structured payload.

3. **Structured output**
   - When returning a "step" response, use a consistent format so backend/frontend can parse:
     ```
     [DASHBOARD_STEP]
     {"step":1,"prompt":"What would you like to name your dashboard?","type":"text_input"}
     [DASHBOARD_STEP]
     ```
   - Or:
     ```
     [DASHBOARD_STEP]
     {"step":2,"prompt":"Choose metrics","type":"options","options":["Success Rate %","Error Rate %",...]}
     [DASHBOARD_STEP]
     ```
   - When complete:
     ```
     [DASHBOARD_COMPLETE]
     {"dashboardTitle":"...","useDefaults":true,"variables":{...},"panels":{...}}
     [DASHBOARD_COMPLETE]
     ```

4. **Context handling**
   - Accept `flowContext` (step, collected) so the model knows where the user is in the flow
   - Include conversation history so the model can reference previous answers

**Acceptance criteria:**
- [ ] AI guides user through title → metrics → variables conversationally
- [ ] AI returns structured step data (type, options, prompt) for frontend to render
- [ ] AI returns complete `DashboardWizardRequest` payload when done
- [ ] Handles invalid input (e.g., bad title) by re-asking

---

### @Paul (Security)

#### CARD 5: Security Review for Chat Dashboard Flow

**Tasks:**
1. Ensure all user inputs (title, variables, panel choices) are validated and sanitized before use
2. Verify dashboard creation requires authentication; no unauthenticated creation
3. Rate limit the dashboard-creation flow (no abuse)
4. Check that structured AI output is not blindly trusted — validate payload server-side before calling DashboardService
5. No XSS: structured messages rendered in chat must escape/sanitize any user-controlled content

**Acceptance criteria:**
- [ ] No security regressions; inputs validated; auth enforced

---

### @Marcus (Database)

**No schema changes required** for the initial implementation. Flow state can be derived from conversation history. If persistence of in-progress flows is needed later (e.g., "resume dashboard creation"), a `FlowState` or `ConversationContext` table can be added — defer unless requested.

---

### @Becca (Testing)

#### CARD 6: E2E and Regression Testing

**Tasks:**
1. **Chat formatting**
   - Verify chat layout on desktop and mobile; no overlap, proper scroll
   - Check message bubbles, input, send button styling

2. **Create Dashboard chat flow**
   - Start "Create Dashboard" from Sidebar
   - Complete full flow: enter title, select metrics, set variables
   - Confirm dashboard is created and link/confirmation appears in chat
   - Test invalid title → AI asks again
   - Test cancel/abandon mid-flow (user sends unrelated message) — graceful behavior

3. **Regression**
   - Existing Chat (general Q&A) still works
   - Quick Query, Query Builder, Common Q&A unchanged
   - Existing dashboard wizard (if kept for "Classic" mode) still works

**Acceptance criteria:**
- [ ] Full Create Dashboard flow passes end-to-end
- [ ] No regressions in other features

---

## DEPENDENCIES

```
George (AI) ─────────────────────────┐
                                     ├──► Gary (Backend) ──► Integration
Niklaus (Formatting) ──► Can start   │
Niklaus (Chat Flow UI) ──────────────┘
         │
         └──► Paul (Security) ──► Before merge
         └──► Becca (Testing) ──► After Gary + Niklaus integration
```

- **Niklaus** Card 1 (formatting) can be done first, in parallel with George
- **George** and **Gary** should align on structured format (e.g., `[DASHBOARD_STEP]` / `[DASHBOARD_COMPLETE]`)
- **Niklaus** Card 2 depends on Gary/George API contract
- **Becca** tests after integration

---

## NOTES

- `DashboardWizardRequest` and `createDashboardFromWizard` already exist; reuse them
- `RECOMMENDED_DEFAULTS` in `DashboardCreatorWizard.jsx` should be shared with George (or defined in a shared config) so options match
- Consider adding a "Create Dashboard" quick action chip in the chat input area (e.g., next to placeholder) for discoverability

═══════════════════════════════════════
