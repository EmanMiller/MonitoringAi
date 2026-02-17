# Task Cards

---

## @Niklaus — Chat Box Formatting

**Priority:** High  
**Area:** Frontend / Design

### Task
Fix the formatting on the chat box on the landing page.

### Acceptance Criteria
- [ ] Chat input/display is visually consistent with Flock Ramp design system
- [ ] Text, spacing, and layout match the rest of the UI
- [ ] No overflow, clipping, or misaligned elements
- [ ] Responsive and readable across viewport sizes

### Notes
Landing page = main page with ChatWindow component.

---

## @Niklaus — Create Dashboard via Chat (Interactive)

**Priority:** High  
**Area:** Frontend / Chat UX

### Task
Change the "Create Dashboard" flow from a popup modal to an interactive chat experience.

### Current Behavior
- User clicks "Create Dashboard" in Sidebar → separate modal (DashboardCreatorWizard) opens with steps.

### Desired Behavior
- User selects "Create Dashboard" in/from the chat
- Chat responds with the dashboard creation options (title, metrics, variables, etc.)
- User interacts back-and-forth with the chat to complete the wizard steps
- No separate popup; the flow is conversational within the chat interface

### Acceptance Criteria
- [ ] "Create Dashboard" can be initiated from within the chat (e.g., menu, quick action, or typed intent)
- [ ] Chat presents options (e.g., dashboard title, defaults, custom selections) as conversational turns
- [ ] User input is captured in chat and sent to the backend
- [ ] Wizard steps are delivered as chat messages with choices/inputs
- [ ] Success/failure feedback appears in the chat
- [ ] Remove or repurpose the standalone DashboardCreatorWizard modal as needed

### Notes
Requires coordination with **George** so the AI returns structured options and accepts user input for each step.

---

## @George — Dashboard Creation: AI Flow & User Input

**Priority:** High  
**Area:** AI / Gemini Integration

### Task
Support the interactive Create Dashboard flow: return basic dashboard options to the chat and accept user input for each step.

### Requirements

1. **Return basic information**
   - When the user requests to create a dashboard, the AI should return structured options (e.g., dashboard title prompt, metric choices, variable defaults).
   - Output should be parseable by the frontend (e.g., JSON or a known format) so the chat can render choices/inputs.

2. **Take in user input**
   - Accept user responses for each step (title, selected metrics, custom selections, variables).
   - Validate and confirm before moving to the next step.
   - Pass the collected data to the backend dashboard creation API when the flow is complete.

### Acceptance Criteria
- [ ] AI recognizes "create dashboard" intent (and variants)
- [ ] AI returns structured options for: title, metrics, variables
- [ ] AI accepts and processes user input for each step
- [ ] Flow supports validation and error handling (e.g., invalid name)
- [ ] Final payload is sent to `createDashboardFromWizard` (or equivalent) on the backend

### Notes
Works with **Niklaus** on the chat UI so responses are rendered correctly and user input is sent back to the AI/backend.

---
