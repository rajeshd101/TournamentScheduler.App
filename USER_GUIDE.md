# 📖 Tournament Scheduler - User Guide

**Version 1.0**  
**Author**: Raj D  
**Contact**: drajesh@hotmail.com

---

## Table of Contents

1. [Getting Started](#getting-started)
2. [Setup Tab - Tournament Configuration](#setup-tab)
3. [Players Tab - Managing Participants](#players-tab)
4. [Schedule Tab - Generating Matches](#schedule-tab)
5. [Scores Tab - Recording Results](#scores-tab)
6. [Tips & Best Practices](#tips--best-practices)
7. [Troubleshooting](#troubleshooting)

---

## Getting Started

### Launching the Application

1. Open a terminal in the `TournamentScheduler.App` folder
2. Run the command: `dotnet run`
3. Open your browser and navigate to the URL shown (typically `https://localhost:5001`)

### Navigation

The application has **4 main tabs**:
- **Setup** (🏠) - Configure tournament settings
- **Players** (👥) - Manage participants
- **Schedule** (📅) - Generate and view matches
- **Scores** (📊) - Enter results and view rankings

---

## Setup Tab

The Setup tab is where you configure all tournament parameters.

### Tournament Settings (Left Panel)

| Setting | Description | Recommended |
|---------|-------------|-------------|
| **Number of Players** | Expected total participants | Match your player list |
| **Number of Courts** | Available courts for play | 4-8 courts typical |
| **Pairing Rule** | How teams are formed | See below |

#### Pairing Rules Explained

| Rule | Description | When to Use |
|------|-------------|-------------|
| **Doubles** | Same gender teams (M+M vs M+M, F+F vs F+F) | Standard tournaments |
| **Singles** | 1v1 same gender matches | Singles format |
| **Mixed Doubles** | One man + one woman per team | Social/mixed events |

### Timing & Schedule (Right Panel)

| Setting | Description |
|---------|-------------|
| **Match Duration** | Minutes per game (typically 12-15) |
| **Break Time** | Minutes between rounds (typically 1-3) |
| **Start Time** | When tournament begins |
| **End Time** | When tournament ends |

### Data Path

- Set where tournament data is saved
- Click **Browse...** to select a folder
- All player and schedule data saves automatically

---

## Players Tab

### Adding Players Manually

1. Enter **Name** in the text field
2. Select **Gender** (Male/Female)
3. Select **Skill Level** (1-5)
   - 1 = Novice
   - 3 = Intermediate
   - 5 = Expert
4. Click **Add**

### Importing Players via CSV

1. Click **📥 Download Template** to get the CSV format
2. Open the CSV in Excel or a text editor
3. Add your players (one per line):
   ```
   Name,Gender,Skill
   John Smith,M,3
   Jane Doe,F,4
   ```
4. Save the file
5. Click **📤 Import CSV** and select your file
6. Success message shows how many players were imported

### Editing Players

1. Find the player card in the grid
2. Click **Edit** on their card
3. Modify name, gender, or skill
4. Click **Update** to save

### Player Status

- **Toggle In/Out** - Click the status toggle on a player's card
- "Out" players are excluded from schedule generation
- Useful for late arrivals or early departures

### Searching Players

- Use the **search bar** in the header
- Type any part of a player's name
- Results filter in real-time across all tabs

---

## Schedule Tab

### Generating the Schedule

1. Ensure all players are added and active
2. Set your tournament configuration in Setup
3. Click **⚡ Generate**
4. Schedule appears in a grid format

### Understanding the Schedule Grid

| Element | Meaning |
|---------|---------|
| **Rows** | Rounds (with start times) |
| **Columns** | Courts |
| **Cards** | Match details |
| **🏓 Symbol** | Team that serves first |

### After First Generation

- The Generate button **locks** (🔒) to prevent accidents
- Click **🔒 Unlock to Regenerate** to create a new schedule
- Warning: Regenerating clears all scores!

### Exporting to PDF

1. Click **🖨️ Export PDF**
2. Browser print dialog opens
3. Select "Save as PDF" as destination
4. Full schedule exports across multiple pages

---

## Scores Tab

### Layout Overview

The Scores tab has a **3-column layout**:
- **Left**: Men's Top 10 + Participation Stats
- **Center**: Match Cards for Score Entry
- **Right**: Women's Top 10 + Participation Stats

### Entering Scores

1. Find the match in the center panel
2. Enter both team scores in the number fields
3. Scores **save automatically**

### Score Validation

| Color | Meaning |
|-------|---------|
| Normal | Valid score (0-11) |
| **Amber** | Score > 11 (verify if correct) |

### Round Locking

Rounds can be **locked** to prevent changes:

| Lock State | Meaning |
|------------|---------|
| 🔒 Locked | Scores cannot be changed |
| 🔓 Unlocked | Scores can be edited |

**Automatic Locking**:
- Previous rounds lock when later rounds have scores
- Rounds lock after their scheduled end time

**Manual Override**:
- Click the lock icon to toggle lock state

### Viewing Rankings

**Top 10 Panels** show:
- Player rank
- Player name
- Wins count
- Total points

**Scoring System**:
- Win = Points based on score difference
- Rankings update in real-time

### Ending the Tournament

1. After all matches are scored
2. Click **🏆 End Tournament**
3. Congratulations modal appears with:
   - 🥇 1st Place (Men & Women)
   - 🥈 2nd Place (Men & Women)
   - 🥉 3rd Place (Men & Women)
4. Click **Close** to dismiss

---

## Tips & Best Practices

### Before the Tournament

✅ Add all players before generating schedule  
✅ Set correct number of courts  
✅ Choose appropriate pairing rule  
✅ Set realistic start/end times  
✅ Test with a few players first

### During the Tournament

✅ Enter scores promptly after each match  
✅ Use the search bar to find specific players  
✅ Check participation stats to ensure fairness  
✅ Lock completed rounds to prevent changes

### For Best Results

✅ **Equal Gender Ratio** - For Mixed Doubles, equal men/women works best  
✅ **Skill Variety** - Import skill levels for better matchmaking  
✅ **Court Count** - More courts = more matches per round  
✅ **Buffer Time** - Add 1-2 minutes break between rounds

---

## Troubleshooting

### "No matches scheduled"
- Ensure you have at least 4 players for doubles
- Check that players are set to "In" status
- Verify pairing rule matches your player genders

### Schedule shows empty courts
- This shouldn't happen with current version
- 100% court utilization is enforced
- Try regenerating if issue persists

### Can't enter scores
- Check if round is locked (click 🔒 to unlock)
- Verify round has started (time-based)
- Previous rounds may be locked

### PDF only shows first page
- Use Chrome or Edge browser
- Check print settings for "All pages"
- Print CSS has been optimized for multi-page

### App won't build
- Close running instance first
- `.exe` file may be locked by previous run
- Use Task Manager to end `TournamentScheduler.App` process

---

## Keyboard Shortcuts

| Action | Shortcut |
|--------|----------|
| Search | Click search bar + type |
| Navigate tabs | Click tab buttons |
| Clear search | Click ✕ in search bar |

---

## Data Storage

Tournament data is saved as JSON files in your configured data path:
- `players.json` - Player information
- `schedule.json` - Match schedule
- `config.json` - Tournament settings

**Backup**: Copy these files to preserve your tournament data.

---

## Support

For questions, issues, or feature requests:

📧 **Email**: drajesh@hotmail.com

---

*Thank you for using Tournament Scheduler!* 🏓
