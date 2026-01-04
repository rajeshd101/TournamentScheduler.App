# TourneyPro v1.5

**Author:** Raj D  
**Email:** drajesh@hotmail.com

---

## No Admin Required!

This application runs entirely in user space:
- No administrator privileges needed
- No installation required
- No external dependencies
- Data saved to your Documents folder

---

## Quick Start

### Step 1: Extract the ZIP
Extract to any folder (Desktop, Downloads, USB drive, etc.)

### Step 2: Run the Application
Double-click `StartServer.bat`

### Step 3: Open in Browser

| URL | Purpose |
|-----|---------|
| http://localhost:5000 | Main Application |
| http://localhost:5000/scoreboard | **Display Screen** (for projectors/TVs) |

### Step 4: Stop the Server
Press `Ctrl+C` or close the command window.

---

## Scoreboard Display

Open `/scoreboard` on a separate screen for live tournament display:
- Shows Men's and Women's Top 10
- Current round matches with live scores
- Auto-refreshes every 15 seconds
- Perfect for projectors, TVs, or extra monitors

**LAN URL:** `http://YOUR_IP:5000/scoreboard`

---

## Data Location

All tournament data is saved to:
```
C:\Users\[YourName]\Documents\TournamentScheduler\
```

---

## Using the Application

### 1. Setup Tab
- Configure players, courts, timing
- Select pairing rule (Doubles, Singles, Mixed Doubles)

### 2. Players Tab
- Add players manually or import CSV
- Set gender (M/F) and skill level (1-5)

### 3. Schedule Tab
- Click **Generate** to create schedule
- 🏓 shows serving team
- **Export PDF** to print

### 4. Scores Tab
- Enter scores for each match
- **Auto-refresh** dropdown (Off, 15s, 30s, 1min, 5min)
- Click **🏆 End Tournament** to see winners

---

## LAN Access

For other devices to access:

**Open PowerShell as Administrator and run:**
```
New-NetFirewallRule -DisplayName "TourneyPro" -Direction Inbound -Protocol TCP -LocalPort 5000 -Action Allow
```

---

## System Requirements

- Windows 10/11 (64-bit)
- No .NET installation needed
- No admin privileges needed
- ~100 MB disk space

---

## Support

Contact: **drajesh@hotmail.com**

---

*Thank you for using TourneyPro!* 🏓
