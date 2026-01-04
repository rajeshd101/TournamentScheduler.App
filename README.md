# 🏓 TourneyPro

A modern, feature-rich tournament scheduling application for pickleball and other doubles/singles sports.

## Author

**Raj D**  
📧 Email: drajesh@hotmail.com

---

## Features

### 🎯 Scheduling
- **Multiple Pairing Rules**
  - Doubles (M+M vs M+M or F+F vs F+F)
  - Singles (M vs M or F vs F)
  - Mixed Doubles (M+F vs M+F)
- **100% Court Utilization** - All courts are always filled
- **Fair Game Distribution** - Equal games for all players
- **Skill-Based Matching** - Players matched by similar skill levels
- **Gender-Alternating Rounds** - Women's and men's rounds alternate
- **Random Service Assignment** - 🏓 indicator shows which team serves first

### 👥 Player Management
- Add/Edit/Remove players
- CSV Import/Export for bulk player management
- Player skill levels (1-5)
- Gender assignment for pairing rules
- Player status toggle (In/Out)
- Global search by player name

### 📊 Score Tracking
- Real-time score entry
- Score validation with amber warning for scores > 11
- Progressive round locking (previous rounds lock when later rounds have scores)
- Manual lock/unlock for rounds
- Top 10 leaderboard for men and women

### 🏆 Tournament Management
- End Tournament button with congratulations modal
- Top 3 Champions display (Men's and Women's)
- Gold/Silver/Bronze medal styling
- PDF Export for schedule printing

### 🎨 User Interface
- Modern glass-morphism design
- Dark/Light mode support
- Responsive layout
- Live round highlighting
- Court rotation fairness tracking

---

## Getting Started

### Prerequisites
- .NET 9.0 SDK or later
- Windows OS (for native folder picker)

### Installation

1. **Clone or download the project**
   ```bash
   cd tournament_scheduler/TournamentScheduler.App
   ```

2. **Build the application**
   ```bash
   dotnet build
   ```

3. **Run the application**
   ```bash
   dotnet run
   ```

4. **Open in browser**
   - Navigate to `https://localhost:5001` or the URL shown in the terminal

---

## Usage Guide

### 1. Setup (Home Tab)
- Set the number of players and courts
- Configure match duration and break time
- Set start and end times
- Choose pairing rule (Doubles/Singles/Mixed Doubles)
- Set data path for saving tournament data

### 2. Players Tab
- Add players manually or use **CSV Import**
- Download CSV template for bulk import
- Set player skill levels (1-5)
- Toggle players In/Out as needed

### 3. Schedule Tab
- Click **Generate** to create the tournament schedule
- Schedule shows all matches in a grid format
- 🏓 symbol indicates which team serves first
- **Export PDF** to print the schedule
- Generate is locked after first use (click to unlock)

### 4. Scores Tab
- Enter scores for completed matches
- Scores auto-save
- View Top 10 rankings for men and women
- View participation stats
- Click **🏆 End Tournament** to see winners

---

## CSV Format for Player Import

```csv
Name,Gender,Skill
John Doe,M,3
Jane Smith,F,4
Mike Johnson,M,5
Sarah Williams,F,2
```

- **Name**: Player's full name
- **Gender**: M (Male) or F (Female)
- **Skill**: 1-5 (1=Novice, 5=Expert)

---

## Scheduling Algorithm

The scheduler uses a sophisticated algorithm that:

1. **Generates all valid pairs** based on pairing rule
2. **Creates match combinations** from pairs
3. **Prioritizes by bucket**:
   - Target gender + Fair + Fresh players
   - Target gender + Ahead + Fresh players
   - Non-target gender + Fair + Fresh players
   - Non-target gender + Ahead + Fresh players
   - Non-fresh players (fallback for 100% court fill)
4. **Assigns courts** prioritizing players who haven't used that court
5. **Balances game counts** to ensure equal participation

---

## Technology Stack

- **Framework**: Blazor WebAssembly (.NET 9.0)
- **Styling**: Custom CSS with CSS Variables
- **Data Storage**: JSON files (local filesystem)

---

## License

This project is provided as-is for personal and organizational use.

---

## Support

For questions or support, contact:  
📧 **drajesh@hotmail.com**
