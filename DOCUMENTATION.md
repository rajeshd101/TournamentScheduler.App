# 🏆 TourneyPro v1.5 - Technical Documentation

TourneyPro is a high-performance tournament scheduling and management application built with .NET 9.0 and Blazor. It is designed to handle complex scheduling requirements for sports like Pickleball, Tennis, and Badminton.

---

## 🚀 Quick Start

### Development Environment
1. **Prerequisites**: Install [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0).
2. **Clone Repository**:
   ```bash
   git clone https://github.com/rajeshd101/TournamentScheduler.App.git
   cd TournamentScheduler.App
   ```
3. **Run Application**:
   ```bash
   dotnet run
   ```
   Access the app at `http://localhost:5000`.

### Standalone Desktop Version
The `standalone-app` branch contains the **Photino.Blazor** implementation, allowing the app to run as a native Windows `.exe` without a browser.

---

## 🛠 Core Features & Logic

### 1. Advanced Scheduling Algorithm
The scheduler (`Services/SchedulerService.cs`) is optimized for:
- **100% Court Occupancy**: Mandatory condition to fill all available courts if at least 4 players are available.
- **Fairness Engine**: Tracks game counts per player to ensure equal participation.
- **Resting Logic**: Prioritizes players who did not play in the previous round while maintaining full occupancy.
- **No Consecutive Repeats**: Ensures no two players play in the same match together in two consecutive rounds.
- **High Randomness**: Uses randomized selection and scoring to ensure varied schedules even with identical player lists.
- **Gender Rules**: 
  - **Doubles**: Same-gender teams.
  - **Singles**: 1v1 matches.
  - **Mixed Doubles**: Preferred M+F pairings, with fallback to same-gender if counts are unequal.

### 2. Smart Regeneration
Located in `Services/TournamentState.cs`, the regeneration logic:
- Preserves history of completed (scored) matches.
- Identifies the active round based on time and preserves it.
- Only recalculates future rounds to accommodate player changes (e.g., a player going "Out").

### 3. Player Management
- **Bulk Add**: Paste a list of names to add players instantly.
- **CSV Import/Export**: Full support for external player list management.
- **Status Tracking**: Toggle players "In" or "Out" to dynamically update the schedule.
- **Remove All**: One-click cleanup of the player roster.

### 4. UI/UX Enhancements
- **Live Highlighting**: The current round is highlighted based on the system clock.
- **Completed Rounds**: Finished rounds are grayed out for visual clarity.
- **Sticky Headers**: Table headers (Round, Court#) remain visible while scrolling.
- **Scoreboard Mode**: Dedicated view at `/scoreboard` for public displays/projectors.

---

## 📂 Project Structure

- `Components/Pages/`: Blazor components for Setup, Players, Schedule, and Scores.
- `Services/`: 
  - `TournamentState.cs`: Centralized state management and persistence logic.
  - `SchedulerService.cs`: The core scheduling algorithm.
- `Models/`: Data structures for Players, Matches, and Config.
- `wwwroot/`: Static assets (CSS, JS, Icons).
- `dist/`: Distribution scripts and README for end-users.

---

## 💾 Data Persistence
Data is automatically saved as JSON in the user's `Documents/TournamentScheduler` folder. This ensures no data loss between sessions and allows for easy backups.

---

## 📧 Support
Developed by **Raj D**.  
Contact: [drajesh@hotmail.com](mailto:drajesh@hotmail.com)
