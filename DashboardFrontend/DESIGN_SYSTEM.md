# MonitoringAI Design System

**Flock Ramp aesthetic:** Premium crypto/finance SaaS – deep indigo, purple gradients

---

## Color Palette

### Backgrounds

| Variable | Value | Usage |
|----------|-------|-------|
| `--color-bg-primary` | `#0a0e1a` | Main background |
| `--color-bg-secondary` | `#141824` | Cards, elevated |
| `--color-bg-tertiary` | `#1a1f2e` | Elevated surfaces |
| `--color-bg-card` | `#1e2333` | Card base |

### Gradients

| Variable | Usage |
|----------|-------|
| `--gradient-primary` | Page backgrounds |
| `--gradient-card` | Cards, panels |
| `--gradient-purple` | Primary buttons |

### Text

| Variable | Value |
|----------|-------|
| `--color-text-primary` | `#ffffff` |
| `--color-text-secondary` | `#a0a3bd` |
| `--color-text-tertiary` | `#6b7280` |
| `--color-text-accent` | `#818cf8` |

### Accents

| Variable | Value |
|----------|-------|
| `--color-border-accent` | `#6366f1` |
| `--color-accent-primary` | `#6366f1` |
| `--color-accent-secondary` | `#8b5cf6` |
| `--color-accent-pink` | `#ec4899` |
| `--color-success` | `#10b981` |

### Crypto Card Colors

| Variable | Value |
|----------|-------|
| `--color-ethereum` | `#627eea` |
| `--color-bitcoin` | `#f7931a` |
| `--color-binance` | `#f3ba2f` |
| `--color-tether` | `#26a17b` |
| `--color-solana` | `#dc1fff` |

### Effects

| Variable | Usage |
|----------|-------|
| `--glow-purple` | Button/card hover |
| `--glow-blue` | Secondary hover |
| `--shadow-focus` | Input focus |
| `--glass-bg` | Modal glassmorphism |

---

## Components

- **Primary buttons:** `--gradient-purple`, glow on hover
- **Cards:** `--gradient-card`, purple corner brackets, glow on hover
- **Modals:** Glassmorphism (backdrop-filter, semi-transparent)
- **Sidebar:** Gradient cards with colored icon backgrounds

---

## Widget Cards

Use `.widget-card` with `.widget-card__icon--ethereum`, `--bitcoin`, `--purple`, `--success` for crypto-style widgets with colored icons and large numbers.
