# RACHEL'S REVIEW — Feb 16, 2025

═══════════════════════════════════════
RACHEL'S REVIEW - Feb 16, 2025
═══════════════════════════════════════

**STATUS:** ⚠️ NEEDS WORK

---

## CRITICAL (must fix)

### 1. AuthController: No password verification for existing users
**@Gary:** In `AuthController.Login`, when a user already exists in the database, the code returns `Ok` without ever verifying the password. Anyone can log in as any existing user with any password.

**Fix:** Use `AuthService.ValidateLoginAsync` (or `BCrypt.Net.BCrypt.Verify`) to validate the password for existing users before returning success.

### 2. Auth flow disconnected: JWT/cookies never issued
**@Gary:** The `AuthController` returns `{ id, userName, role, expiresInMinutes }` but never:
- Calls `IAuthService.IssueTokensAsync` to generate JWT + refresh token
- Sets `access_token` or `refresh_token` cookies

`AuthService` has full JWT/refresh token logic but is never used by the controller. `JwtCookieMiddleware` exists to read cookies but:
1. It may not be registered in `Program.cs` (middleware pipeline)
2. Even if it were, no cookies are ever set on login

**Fix:** Wire `AuthController.Login` to `IAuthService.ValidateLoginAsync` + `IssueTokensAsync`, then set httpOnly cookies for `access_token` and `refresh_token` in the response.

### 3. AuthController /me returns first user, not logged-in user
**@Gary:** `GET /api/auth/me` returns the first user in the database regardless of who is “logged in,” because there is no session or token to identify the current user.

**Fix:** Protect the `/me` endpoint with `[Authorize]`, read the user ID from `User.FindFirst(ClaimTypes.NameIdentifier)`, and return that user. Ensure `JwtCookieMiddleware` is registered before `UseAuthentication()`.

---

## HIGH PRIORITY (should fix)

### 4. Admin Panel has no route
**@Niklaus:** `AdminPanel` (Log Mappings + Query Library) exists but is not reachable. There is no `/admin` route in `App.jsx`, and the Sidebar has no link to Admin. QA report expects Admin Panel to be accessible.

**Fix:** Add `<Route path="/admin" element={<ProtectedRoute><AdminPanel /></ProtectedRoute>} />` to `App.jsx`, and add an Admin link in `Sidebar.jsx` (e.g. for users with `admin` role).

### 5. Dashboard Wizard success link rendered as plain text
**@Niklaus:** After wizard success, `statusMessage` contains HTML:  
`✅ Dashboard Live! <a href="${dashboardUrl}">View Dashboard</a>`.  
`ProcessingView` renders it with `<p>{status}</p>`, so the link appears as raw HTML text, not clickable.

**Fix:** Separate the message and URL: render the link as `<a href={dashboardUrl} target="_blank" rel="noopener noreferrer">View Dashboard</a>` and keep the rest as plain text.

### 6. Query run endpoint missing
**@Gary:** `api.js` calls `POST /api/Query/run`, but `QueryController` only has `POST /api/Query/ask`. The frontend `runQuery` catches errors and returns mock data. Query Builder “Run” does not hit a real backend.

**Fix:** Add `[HttpPost("run")]` to `QueryController` that executes the Sumo Logic query (or returns a clear “not yet implemented” response) and update the API contract.

### 7. CORS may exclude frontend on alternate port
**@Gary:** `appsettings.json` and `appsettings.Development.json` set `Cors:Origins` to `http://localhost:5173`. QA notes the frontend can run on 5174. Vite may use a different port.

**Fix:** Add `http://localhost:5174` to CORS origins, or use a configurable list like `http://localhost:5173,http://localhost:5174`.

### 8. Jwt:Secret in appsettings.json
**@Paul:** `appsettings.json` has `"Secret":"REPLACE_WITH_STRONG_SECRET_MIN_32_CHARS"`. If this is deployed as-is, JWT signing is weak.

**Fix:** Ensure production reads the secret from env vars or a secrets store; keep a placeholder in the repo.

---

## WORKING WELL

✅ **Design system:** Flock Ramp colors, gradients, and corner brackets defined in `_variables.css` and `_corner-brackets.css`  
✅ **Input validation:** `InputValidationService` sanitizes chat, dashboard names; XSS mitigations in place  
✅ **Rate limiting:** `ChatRateLimitService` applied to Chat and Query endpoints  
✅ **ProcessingView:** Uses plain `{status}` (no `dangerouslySetInnerHTML`), addressing prior XSS concern  
✅ **Core flows:** Chat, Query Library CRUD, Common Q&A, Query Builder visual/code modes, wizard steps  
✅ **NUnit tests:** 15/15 pass per QA report  
✅ **Build:** Frontend and backend build successfully  

---

## DESIGN CONSISTENCY CHECK

| Element              | Expected                          | Status                          |
|----------------------|-----------------------------------|---------------------------------|
| Flock Ramp colors    | Deep indigo, purple gradients     | ✅ Variables defined             |
| Corner brackets      | Purple accent on cards            | ✅ Styles present                |
| Glassmorphism modals | Semi-transparent, blur            | ✅ `--glass-bg` etc.             |
| Header branding      | SumoLogic / MonitoringAI          | ⚠️ Shows "Crate&Barrel"          |

**@Niklaus:** Header displays "Crate&Barrel" — consider updating to "MonitoringAI" or "SumoLogic" for consistency.

---

## NEXT STEPS

1. **@Gary** — Fix AuthController: password check, JWT issuance, cookie setting; fix `/me`; add `Query/run` if applicable; register `JwtCookieMiddleware` if needed.
2. **@Niklaus** — Add `/admin` route and Sidebar link; fix wizard success link rendering; update header branding.
3. **@Paul** — Confirm JWT secret is never committed; review CORS and security headers.
4. **@Becca** — Retest auth, admin panel, and query run flows after fixes.
5. **@Dan** — Push only after critical items are resolved and QA signs off.

---

## SIGN-OFF

- [ ] **NOT READY** — Critical auth and admin gaps must be addressed first  
- [ ] READY  
- [ ] APPROVED 🚀  

═══════════════════════════════════════
