# MonitoringAI Design System

**Eclipsis aesthetic:** Pure black, minimal, modern like ChatGPT

---

## Color Palette

### Dark Theme (Eclipsis)

| Variable | Hex | Usage |
|----------|-----|-------|
| `--color-bg-primary` | `#000000` | Main background |
| `--color-bg-secondary` | `#0a0a0a` | Cards, elevated surfaces |
| `--color-bg-tertiary` | `#1a1a1a` | Elevated surfaces |
| `--color-border` | `#2a2a2a` | Subtle borders |
| `--color-border-accent` | `#ffffff` | White accent borders |
| `--color-text-primary` | `#ffffff` | Primary text |
| `--color-text-secondary` | `#a0a0a0` | Secondary text |
| `--color-text-tertiary` | `#6a6a6a` | Muted gray |
| `--color-hover` | `rgba(255,255,255,0.05)` | Subtle hover |
| `--color-success` | `#10b981` | Success states |
| `--color-error` | `#ef4444` | Error states |
| `--color-warning` | `#f59e0b` | Warning states |

---

## Spacing (Generous, ChatGPT-like)

| Variable | Value |
|----------|-------|
| `--spacing-xs` | 8px |
| `--spacing-sm` | 16px |
| `--spacing-md` | 24px |
| `--spacing-lg` | 32px |
| `--spacing-xl` | 48px |
| `--spacing-2xl` | 64px |

---

## Typography

- **Primary font:** Inter, -apple-system, system-ui, sans-serif
- **Headings:** Thin weight (300)
- **Body:** Regular (400)

---

## Corner Brackets (Eclipsis Signature)

Use `.corner-brackets` on cards for top-left and top-right white bracket accents. Recent Activity card includes built-in corner brackets.

---

## Button Style (Ghost)

- Primary: Transparent bg, white border, white text
- Hover: White bg, black text (inverted)
- Secondary: Transparent, gray text, no border

---

## File Structure

```
src/styles/
├── base/
│   ├── _variables.css
│   ├── _reset.css
│   └── _typography.css
├── components/
│   ├── _corner-brackets.css
│   ├── _buttons.css
│   ├── _cards.css
│   ├── _modals.css
│   └── _forms.css
├── layouts/
│   ├── _header.css
│   └── _sidebar.css
└── pages/
    ├── _dashboard.css
    ├── _chat.css
    ├── _common-qa.css
    ├── _admin.css
    ├── _login.css
    ├── _wizard.css
    └── ...
```
