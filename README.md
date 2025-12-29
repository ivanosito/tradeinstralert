# 📈 TradeInstrAlert – Azure Functions

> **Quiet alerts. Clear signals. Human decisions.**

This project is a **serverless market-alert engine** built with **Azure Functions (.NET 8 – Isolated)**.  
Its purpose is simple and disciplined:

> *Observe the market in silence,  
notify only when something truly matters.*

No bots.  
No auto-trading.  
Just **clarity**.

---

## 🧠 What does it do?

Every **5 minutes**, the system:

1. Reads a **rules file** from Azure Blob Storage  
2. Fetches the latest **5-minute candle (OHLC)** for configured instruments  
3. Evaluates objective conditions (e.g. *did the HIGH reach X?*)  
4. Sends an **SMS alert** when a rule is met  

You decide what to do next.

---

## 🏗️ Architecture (High Level)

```
Azure Timer Trigger (5 min)
        ↓
Azure Blob Storage (rules.json)
        ↓
Market Data Provider (OHLC 5m)
        ↓
Rule Evaluation Engine
        ↓
SMS Notification (VoiceTrading)
        ↓
👤 Human Decision
```

This design is:
- scalable
- observable
- configuration-driven
- cost-efficient

---

## ⚙️ Technology Stack

- **.NET 8 (LTS)** – Isolated Worker Model  
- **Azure Functions** – Timer Trigger  
- **Azure Blob Storage** – Dynamic rule configuration  
- **Market Data API** – OHLC candles (5m)  
- **VoiceTrading SMS Gateway** – Notifications  
- **GitHub Actions** – CI/CD deployment  

---

## 📂 Rule Configuration (`rules.json`)

Rules are stored in **Azure Blob Storage**, allowing live updates **without redeploying**.

```json
{
  "rules": [
    {
      "symbol": "XAU/USD",
      "timeframe": "5min",
      "check": {
        "highGte": 4480.0
      },
      "sms": {
        "enabled": true,
        "alwaysSendCandle": false
      }
    }
  ]
}
```

### Supported concepts
- `highGte` → alert when candle HIGH ≥ value  
- (future) `lowLte`, `closeGte`, multiple conditions  
- Enable/disable alerts per instrument

Secrets never live here.

---

## 🔐 Secrets & Configuration

All sensitive data is stored securely via **Azure App Settings** (or Key Vault):

- Market data API key  
- VoiceTrading credentials  
- SMS sender / recipient  
- Blob container & file names  

No credentials are committed to GitHub.

---

## 🚀 Deployment

Deployment is handled automatically via **GitHub Actions**.

Every push to `main`:
1. Builds the Function
2. Publishes to Azure
3. Activates the new version

Clean. Repeatable. Professional.

---

## 🎯 Design Philosophy

This project follows a few non-negotiable principles:

- **Silence is a feature**  
- **Rules before opinions**  
- **No alerts without structure**  
- **Humans stay in control**  

> *When the market shouts, the system stays calm.*

---

## 🧪 Local Development

- Azure Functions Core Tools  
- Storage Emulator (Azurite)  
- `.NET 8 SDK`  

Timer triggers can be tested locally with adjusted schedules.

---

## 🛣️ Roadmap (Ideas)

- Multiple instruments per execution  
- Additional candle conditions  
- Cooldown / hysteresis per rule  
- Dashboard (SignalR / Web UI)  
- Event Grid fan-out  
- Historical logging  

---

## ✨ Final Note

This is not about speed.  
This is not about prediction.

This is about **method**.

Built for those who prefer:
- fewer alerts
- better timing
- and sleeping well

---

**Built with clarity.  
Deployed with discipline.**  
