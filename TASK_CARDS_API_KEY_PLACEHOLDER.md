# Task Cards — API Key Placeholder Revert & Setup

═══════════════════════════════════════
TASK CARDS - API Key Placeholder
═══════════════════════════════════════

## CONTEXT

Revert recent API key configuration changes and standardize on a single placeholder: **`EMAN_GOOGLE_API_KEY_HERE`**.

Anyone who needs the Google Gemini API key should replace this placeholder with their actual key.

---

## PLACEHOLDER VALUE

```
EMAN_GOOGLE_API_KEY_HERE
```

Use this exact string as the placeholder in all config files. Developers replace it with their real Google Gemini API key to enable chat, query matching, and other Gemini features.

**Get a key:** https://aistudio.google.com/app/apikey

---

## TASK CARDS

---

### @Gary (Backend)

#### Revert API key config and use placeholder

**Files to update:**

1. **`DashboardApi/.env.example`**
   - Set: `GEMINI_API_KEY=EMAN_GOOGLE_API_KEY_HERE`
   - Comment: "Replace with your Google Gemini API key"

2. **`DashboardApi/appsettings.json`**
   - Set: `"Gemini": { "ApiKey": "EMAN_GOOGLE_API_KEY_HERE", ... }`

3. **`DashboardApi/appsettings.Example.json`**
   - Set: `"Gemini": { "ApiKey": "EMAN_GOOGLE_API_KEY_HERE", ... }`

4. **`DashboardApi/appsettings.Development.json`**
   - Set: `"Gemini": { "ApiKey": "EMAN_GOOGLE_API_KEY_HERE", ... }`

5. **`DashboardApi/Services/GeminiChatService.cs`**
   - In `GetApiKey()`: treat `EMAN_GOOGLE_API_KEY_HERE` as invalid/placeholder (return null) so the app does not treat it as a real key.

6. **`DashboardApi/Services/QueryAssistantService.cs`**
   - Same: ignore `EMAN_GOOGLE_API_KEY_HERE` when checking if the key is configured.

**Acceptance criteria:**
- [ ] All config files use `EMAN_GOOGLE_API_KEY_HERE` as the placeholder
- [ ] Backend treats this placeholder as "not configured"
- [ ] Error messages tell users to replace the placeholder with their actual key

---

### @Niklaus (Frontend)

#### Update Chat UI messages for placeholder

**Files to update:**

1. **`DashboardFrontend/src/components/ChatWindow.jsx`**
   - `NO_KEY_MESSAGE`: tell users to replace `EMAN_GOOGLE_API_KEY_HERE` with their Gemini API key in `.env` or appsettings.

2. **`DashboardFrontend/src/components/ChatSettingsModal.jsx`**
   - Hint text: "Replace EMAN_GOOGLE_API_KEY_HERE with your Google Gemini API key in .env or appsettings (Gemini:ApiKey)."
   - Error message when connection fails: same instruction.

**Acceptance criteria:**
- [ ] Chat shows clear instructions to replace the placeholder
- [ ] Settings modal explains where and how to set the key

---

### @Paul (Security) — Optional review

- Ensure the placeholder is never sent to any external API.
- Confirm no real keys are committed; `.env` and local overrides remain gitignored.

**Review (done):**
- **Placeholder not sent to APIs:** `GeminiChatService.GetApiKey()` returns `null` when the key is `EMAN_GOOGLE_API_KEY_HERE` or `placeholder`. All Gemini calls (`SendChatAsync`, `GenerateWithSystemAsync`) check `string.IsNullOrEmpty(apiKey)` and throw before building the request URL, so the placeholder is never sent to `generativelanguage.googleapis.com`. `QueryAssistantService` throws if the key is the placeholder before using it. `QueryAssistantAiService` and `QueryMatchService` use `GeminiChatService`, so they inherit this behavior.
- **Secrets not committed:** `DashboardApi/.gitignore` and `DashboardFrontend/.gitignore` include `.env`, `.env.local`, `.env.*.local`; `DashboardApi` also has `appsettings.*.local`. Root `.gitignore` includes `.env` and `.env.*.local`. Real keys in those files are not committed.

---

## FOR THE DEV WHO NEEDS THE KEY

**Placeholder to replace:**

```
EMAN_GOOGLE_API_KEY_HERE
```

**Where to put your key:**

- **Option A (recommended):** Create or edit `DashboardApi/.env` and set:
  ```
  GEMINI_API_KEY=your_actual_key_here
  ```
- **Option B:** Set `Gemini:ApiKey` in `appsettings.json` or `appsettings.Development.json`

**Get a key:** https://aistudio.google.com/app/apikey

**Important:** Never commit your real API key. Use `.env` (gitignored) or local config overrides.

═══════════════════════════════════════
